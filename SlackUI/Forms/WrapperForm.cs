﻿#region Copyright © 2014 Ricardo Amaral

/*
 * Use of this source code is governed by an MIT-style license that can be found in the LICENSE file.
 */

#endregion

using System;
using System.Windows.Forms;
using CefSharp.WinForms;

namespace SlackUI {

    [System.ComponentModel.DesignerCategory("Form")]
    internal partial class WrapperForm : BaseForm {

        #region Private Fields

        private const uint SYSMENU_DEVTOOLS_ID = 0x1;

        private readonly ChromiumWebBrowser chromium;

        private FormWindowState previousWindowState;

        #endregion

        #region Public Constructors

        /*
         * Create a wrapper form around the chromium web browser.
         */
        public WrapperForm() {
            InitializeComponent();

            // Initializes a new instance of the chromium web browser
            chromium = new ChromiumWebBrowser("http://jsfiddle.net/u7sffzc5/")
            {
                MenuHandler = new BrowserMenuHandler()
            };

            // Subscribe to multiple chromium web browser events
            chromium.FrameLoadEnd += chromium_FrameLoadEnd;

            // Add the chromium web browser to the browser panel
            browserPanel.Controls.Add(chromium);

            // Save the current window state as the previous one
            previousWindowState = WindowState;
        }

        #endregion

        #region Private Methods

        /*
         * Chromium web browser frame load end event handler.
         */
        private void chromium_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            if (e.IsMainFrame)
            {
                // Remove the initial load overlay from the form
                this.InvokeOnUiThreadIfRequired(() => browserPanel.Controls.RemoveByKey("initialLoadOverlay"));

                chromium.FrameLoadEnd -= chromium_FrameLoadEnd;
            }
        }

        /*
         * Save the wrapper form window location, size and state settings.
         */
        private void SaveWindowProperties() {
            // Update the wrapper form window state setting
            Program.Settings.Data.WindowState = WindowState;

            // Update the wrapper form window location and size settings
            if(WindowState == FormWindowState.Normal) {
                Program.Settings.Data.WindowLocation = Location;
                Program.Settings.Data.WindowSize = Size;
            } else {
                Program.Settings.Data.WindowLocation = RestoreBounds.Location;
                Program.Settings.Data.WindowSize = RestoreBounds.Size;
            }

            // Persist new application settings
            Program.Settings.Save();
        }

        /*
         * Wrapper form closing event handler.
         */
        private void WrapperForm_FormClosing(object sender, FormClosingEventArgs e) {
            // Release all chromium web browser resources
            if(e.CloseReason == CloseReason.ApplicationExitCall) {
                if(chromium != null) {
                    chromium.Dispose();
                }
            }

            // Hide instead of close if the user is closing the form
            if(e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                Hide();
            }
        }

        #endregion

        #region Protected Methods

        /*
         * Handler for the overridden on handle created event.
         */
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);

            // Get this form's system menu handler
            IntPtr systemMenu = NativeMethods.GetSystemMenu(Handle, false);

            // Add a separator followed by the DevTools menu item
            NativeMethods.AppendMenu(systemMenu, NativeMethods.MenuFlags.MF_SEPARATOR, UIntPtr.Zero, String.Empty);
            NativeMethods.AppendMenu(systemMenu, NativeMethods.MenuFlags.MF_STRING, new UIntPtr(SYSMENU_DEVTOOLS_ID),
                "&Show DevTools…");
        }

        /*
         *  Handler for the overridden Windows messages processor.
         */
        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);

            // Handles detected Windows messages
            switch(m.Msg) {
                // Show DevTools if the respective item was selected from the system menu
                case (int)NativeMethods.WindowsMessages.WM_SYSCOMMAND:
                    if((int)m.WParam == SYSMENU_DEVTOOLS_ID) {
                        chromium.ShowDevTools();
                    }

                    break;

                // Save the wrapper form window location, size and state
                case (int)NativeMethods.WindowsMessages.WM_EXITSIZEMOVE:
                    SaveWindowProperties();
                    break;

                // Save the wrapper form window location, size and state
                case (int)NativeMethods.WindowsMessages.WM_SIZE:
                    if(previousWindowState != WindowState) {
                        previousWindowState = WindowState;
                        SaveWindowProperties();
                    }

                    break;
            }
        }

        #endregion

    }

}
