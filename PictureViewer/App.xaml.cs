﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PictureViewer
{
    public partial class App : Application
    {
        public App()
        {
            UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>(Application_UnhandledException);

            InitializeComponent();

            // debugging
            Application.Current.Host.Settings.EnableFrameRateCounter = true;
            Application.Current.Host.Settings.EnableRedrawRegions = true;
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred, break in the debugger
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                // By default show the error
                e.Handled = true;
                MessageBox.Show(e.ExceptionObject.Message + Environment.NewLine + e.ExceptionObject.StackTrace,
                    "Error", MessageBoxButton.OK);
            }
        }
    }
}