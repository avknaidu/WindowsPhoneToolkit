﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Collections.Specialized;


namespace Phone.Controls.Samples
{
    [ContentPropertyAttribute("Items")]
    public class PanoramaControl : ItemsControl
    {
        // child controls
        private const string LayoutRootName = "LayoutRoot";
        private Panel LayoutRoot { get; set; }

        // scroll view
        private PanoramaView ScrollView;

        public PanoramaControl()
        {
            this.DefaultStyleKey = typeof(PanoramaControl);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // get the parts
            LayoutRoot = GetTemplateChild(LayoutRootName) as Panel;

            // scroll view
            ScrollView = new PanoramaView(this, LayoutRoot);
            ScrollView.AnimationCompleted += new AnimationCompleteEventHandler(ScrollView_AnimationCompleted);

            // control events
            SizeChanged += new SizeChangedEventHandler(OnSizeChanged);
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // clip to control layout
            LayoutRoot.SetValue(Panel.ClipProperty, new RectangleGeometry() { Rect = new Rect(0, 0, this.Width, this.Height) });

            // reset scroll viewer
            ScrollView.Reset(false);
        }

        #region Navigation
        public void ScrollPrev()
        {
            // skip current animation
            ScrollView.ScrollSkip();

            // move to previous item
            int index = this.SelectedIndex - 1;
            if (index < 0) index = Items.Count - 1;
            double pos = this.Items.GetItemPosition(this.SelectedIndex) - this.Items.GetItemWidth(index);
            ScrollView.ScrollTo(pos);
        }

        public void ScrollNext()
        {
            // skip current animation
            ScrollView.ScrollSkip();

            // move to next item
            int index = this.SelectedIndex;
            double pos = this.Items.GetItemPosition(index) + this.Items.GetItemWidth(index);
            ScrollView.ScrollTo(pos);
        }

        private void MoveTo(int index)
        {
            double pos = this.Items.GetItemPosition(index);
            ScrollView.MoveTo(pos);
        }

        void ScrollView_AnimationCompleted(object sender, AnimationCompleteEventArgs e)
        {
           // find out where we landed
            SelectedIndex = this.Items.GetIndexOfPosition(e.Position);

            // special case for when we only have 1 item :
            // the above code will not trigger the SelectedIndex change
            // since we'll be staying on item(0).
            if (this.Items.Count == 1)
            {
                // reset visuals
                MoveTo(SelectedIndex);
            }
        }
        #endregion

        #region Keyboard events
        protected override void OnKeyUp(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.Left:
                    ScrollPrev();
                    break;
                case Key.Down:
                case Key.Right:
                    ScrollNext();
                    break;
            }
        }
        #endregion

        #region Multitouch events
#if WINDOWS_PHONE
        // multitouch position
        double startPosition;

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            e.Handled = true;

            // skip current animation (if any)
            ScrollView.ScrollSkip();

            // capture position
            startPosition = ScrollView.Position;
        }

        ManipulationHook _hack = new ManipulationHook();
        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            e.Handled = true;

            // cancel capture from current object to disable behaviors
            // for example, a button will not trigger the Click event
            // after we've done scrolling
            UIElement ui = e.ManipulationContainer as UIElement;
            ui.ReleaseMouseCapture();

            // the above code doesn't seem to work for unknown reasons
            // let's just hook/override it for now...
            _hack.Set(e.ManipulationContainer, OnManipulationCompleted);

            // new position
            double position = startPosition - e.CumulativeManipulation.Translation.X;

            // move to position
            ScrollView.MoveTo(position);
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            e.Handled = true;

            // new position
            double position = startPosition - e.TotalManipulation.Translation.X;

            // get direction
            int direction = (int)Math.Sign(-e.FinalVelocities.LinearVelocity.X);

            // move previous/next
            switch (direction)
            {
                case -1:
                    ScrollPrev();
                    return;
                case 1:
                    ScrollNext();
                    return;
            }

            // find out which item is at screen center
            double center = position + LayoutRoot.ActualWidth / 2;
            int index = this.Items.GetIndexOfPosition(center);
            PanoramaItem item = this.Items.GetItem(index);

            // cycle back to last item ?
            if (center < 0)
            {
                ScrollPrev();
                return;
            }

            // cycle back to first item ?
            if (center > this.Items.GetTotalWidth())
            {
                ScrollNext();
                return;
            }

            // item's start position
            double start = this.Items.GetItemPosition(index);

            // close to left edge : snap left
            if (center - start < LayoutRoot.ActualWidth / 2)
            {
                ScrollView.ScrollTo(start);
            }

            // close to right edge : snap right
            else if (center - start > item.Width - LayoutRoot.ActualWidth / 2)
            {
                double end = start + item.Width;
                ScrollView.ScrollTo(end - DefaultItemWidth);
            }

            // nowhere close to edges but
            // we're asked to snap anyways
            else if (item.AutoSnap)
            {
                ScrollView.ScrollTo(start);
            }
        }
#endif
        #endregion

        #region BackgroundImage
        public static readonly DependencyProperty BackgroundImageProperty =
            DependencyProperty.Register(
            "BackgroundImage",
            typeof(ImageSource),
            typeof(PanoramaControl),
            new PropertyMetadata(OnBackgroundImageChanged));

        public static readonly DependencyProperty BackgroundImageTemplateProperty =
            DependencyProperty.Register(
            "BackgroundImageTemplate",
            typeof(DataTemplate),
            typeof(PanoramaControl),
            null);

        public ImageSource BackgroundImage
        {
            get { return (ImageSource)GetValue(BackgroundImageProperty); }
            set { SetValue(BackgroundImageProperty, value); }
        }

        private static void OnBackgroundImageChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PanoramaControl ctrl = (PanoramaControl)sender;
            ctrl.OnBackgroundImageChanged(e.OldValue, e.NewValue);
        }

        public virtual void OnBackgroundImageChanged(object oldValue, object newValue)
        {
        }

        public DataTemplate BackgroundImageTemplate
        {
            get { return (DataTemplate)GetValue(BackgroundImageTemplateProperty); }
            set { SetValue(BackgroundImageTemplateProperty, value); }
        }
        #endregion

        #region Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
            "Title",
            typeof(string),
            typeof(PanoramaControl),
            new PropertyMetadata(OnTitleChanged));

        public static readonly DependencyProperty TitleTemplateProperty =
            DependencyProperty.Register(
            "TitleTemplate",
            typeof(DataTemplate),
            typeof(PanoramaControl),
            null);

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        private static void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PanoramaControl ctrl = (PanoramaControl)sender;
            ctrl.OnTitleChanged(e.OldValue, e.NewValue);
        }

        public virtual void OnTitleChanged(object oldValue, object newValue)
        {
        }

        public DataTemplate TitleTemplate
        {
            get { return (DataTemplate)GetValue(TitleTemplateProperty); }
            set { SetValue(TitleTemplateProperty, value); }
        }
        #endregion

        #region DefaultItemWidth Property
        public static readonly DependencyProperty DefaultItemWidthProperty = DependencyProperty.Register(
            "DefaultItemWidth",
            typeof(double),
            typeof(PanoramaControl),
            new PropertyMetadata(OnDefaultItemWidthChanged));

        public double DefaultItemWidth
        {
            get { return (double)GetValue(DefaultItemWidthProperty); }
            set { SetValue(DefaultItemWidthProperty, value); }
        }

        private static void OnDefaultItemWidthChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PanoramaControl ctrl = (PanoramaControl)sender;
            ctrl.UpdateDefaultItemWidth();
        }

        private void UpdateDefaultItemWidth()
        {
            foreach (object o in this.Items)
            {
                PanoramaItem item = o as PanoramaItem;
                if (null != item)
                {
                    // reset width for each item
                    double width = (double)item.GetValue(PanoramaItem.WidthProperty);
                    item.SetValue(PanoramaItem.WidthProperty, Math.Max(width, DefaultItemWidth));
                }
            }
        }
        #endregion

        #region Items
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // BUGBUG : pls optimize this, we don't need to iterate
                    // thru the entire collection each time a child is added

                    // fixup all items default width
                    UpdateDefaultItemWidth();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;
            }

            // delay reset the control
            if (null != ScrollView)
                ScrollView.Reset();
        }

        #endregion

        #region SelectedItem
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem",
            typeof(PanoramaItem),
            typeof(PanoramaControl),
            new PropertyMetadata(OnSelectedItemChanged));

        public PanoramaItem SelectedItem
        {
            get { return (PanoramaItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // this is a wrapper method for selection changes
            // the real work occurs in OnSelectedIndexChanges

            PanoramaControl ctrl = (PanoramaControl)d;
            PanoramaItem oldItem = (PanoramaItem)e.OldValue;
            PanoramaItem newItem = (PanoramaItem)e.NewValue;

            // find out the index for new items
            int index = ctrl.Items.IndexOf(newItem);

            // none found or null : return to old
            if ((null == newItem) || (index == -1))
                index = ctrl.Items.IndexOf(oldItem);

            // change selection
            ctrl.SelectedIndex = index;
        }
        #endregion

        #region SelectedIndex
        public event SelectionChangedEventHandler SelectionChanged;

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
            "SelectedIndex",
            typeof(int),
            typeof(PanoramaControl),
            new PropertyMetadata(OnSelectedIndexChanged));

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PanoramaControl ctrl = (PanoramaControl)d;
            int oldIndex = (int)e.OldValue;
            int newIndex = (int)e.NewValue;

            // nothing to do
            if (newIndex == oldIndex)
            {
                // exit here, to avoid infinite recursion
                return;
            }

            // change selection
            ctrl.SelectedIndex = newIndex;

            // change visuals
            ctrl.MoveTo(ctrl.SelectedIndex);

            // raise event
            PanoramaItem oldItem = ctrl.Items.GetItem(oldIndex);
            PanoramaItem newItem = ctrl.Items.GetItem(newIndex);
            SelectionChangedEventArgs args = new SelectionChangedEventArgs(
                (null == oldItem) ? new List<PanoramaItem> { } : new List<PanoramaItem> { oldItem },
                (null == newItem) ? new List<PanoramaItem> { } : new List<PanoramaItem> { newItem });

            ctrl.OnSelectionChanged(args);
        }

        protected virtual void OnSelectionChanged(SelectionChangedEventArgs args)
        {
            if (null != SelectionChanged)
                SelectionChanged(this, args);
        }
        #endregion
    }
}