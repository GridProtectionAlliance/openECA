//******************************************************************************************************
//  MainWindow.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  05/17/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GSF;
using GSF.Configuration;
using GSF.Reflection;
using GSF.Web.Hosting;
using GSF.Web.Model;
using Microsoft.Owin.Hosting;
using openECAClient.Model;

namespace openECAClient
{
    public partial class MainWindow : Form
    {
        #region [ Members ]

        // Nested Types

        // Constants

        // Delegates

        // Events

        // Fields
        private int m_maxLines = 1000;
        private IDisposable m_webAppHost;

        #endregion

        #region [ Constructors ]

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region [ Properties ]

        #endregion

        #region [ Methods ]

        private void MainWindow_Load(object sender, EventArgs e)
        {
            try
            {
                // Attach to default web server events
                WebServer webServer = WebServer.Default;
                webServer.StatusMessage += WebServer_StatusMessage;
                webServer.ExecutionException += LoggedExceptionHandler;

                // Initiate pre-compile of base templates
                if (AssemblyInfo.EntryAssembly.Debuggable)
                {
                    RazorEngine<CSharpDebug>.Default.PreCompile(LogException);
                    RazorEngine<VisualBasicDebug>.Default.PreCompile(LogException);
                }
                else
                {
                    RazorEngine<CSharp>.Default.PreCompile(LogException);
                    RazorEngine<VisualBasic>.Default.PreCompile(LogException);
                }

                // Create new web application hosting environment
                m_webAppHost = WebApp.Start<Startup>(Model.Global.WebHostURL);

                // Open the main page in the user's default browser
                using (Process.Start(Model.Global.WebHostURL)) { }
            }
            catch (Exception ex)
            {
                LogException(new InvalidOperationException($"Failed to initialize web hosting: {ex.Message}", ex));
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure you want to stop the openECA client?", "Stopping ECA Client...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            if ((object)m_webAppHost != null)
                m_webAppHost.Dispose();
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void WebServer_StatusMessage(object sender, EventArgs<string> e)
        {
            LogText(e.Argument);
        }

        private void LoggedExceptionHandler(object sender, EventArgs<Exception> e)
        {
            LogException(e.Argument);
        }

        private void LogText(string text)
        {
            DisplayText(text);
        }

        private void LogException(Exception ex)
        {
            DisplayError(ex.Message);
        }

        private void DisplayText(string text)
        {
            if (InvokeRequired)
            {
                // Invoke UI updates on the UI thread
                BeginInvoke(new Action<string>(DisplayText), text);
                return;
            }

            // Append text to the text box
            MessagesTextBox.AppendText(text + "\n");

            // Truncate old messages when the text
            // exceeds the maximum number of lines
            MessagesTextBox.SelectionStart = 0;

            MessagesTextBox.SelectionLength = MessagesTextBox.Lines
                .Take(MessagesTextBox.Lines.Length - m_maxLines)
                .Aggregate(0, (length, line) => length + line.Length + "\n".Length);

            MessagesTextBox.SelectedText = "";

            // Scroll to bottom
            MessagesTextBox.SelectionStart = MessagesTextBox.TextLength;
            MessagesTextBox.ScrollToCaret();
        }

        private void DisplayError(string text)
        {
            if (InvokeRequired)
            {
                // Invoke UI updates on the UI thread
                BeginInvoke(new Action<string>(DisplayError), text);
                return;
            }

            // Start selection at the end of the text box
            // in order to set the color of the appended text
            MessagesTextBox.SelectionStart = MessagesTextBox.TextLength;
            MessagesTextBox.SelectionLength = 0;

            // Append text to the text box
            MessagesTextBox.SelectionColor = Color.Red;
            MessagesTextBox.AppendText(text + "\n");
            MessagesTextBox.SelectionColor = ForeColor;

            // Truncate old messages when the text
            // exceeds the maximum number of lines
            MessagesTextBox.SelectionStart = 0;

            MessagesTextBox.SelectionLength = MessagesTextBox.Lines
                .Take(MessagesTextBox.Lines.Length - m_maxLines)
                .Aggregate(0, (length, line) => length + line.Length + "\n".Length);

            MessagesTextBox.SelectedText = "";

            // Scroll to bottom
            MessagesTextBox.SelectionStart = MessagesTextBox.TextLength;
            MessagesTextBox.ScrollToCaret();
        }

        #endregion

        #region [ Operators ]

        #endregion

        #region [ Static ]

        // Static Fields
        public static readonly AppModel Model;

        // Static Constructor

        static MainWindow()
        {
            CategorizedSettingsElementCollection systemSettings = ConfigurationFile.Current.Settings["systemSettings"];

            Model = new AppModel();
            Model.Global.WebHostURL = systemSettings["WebHostURL"].Value;
            Model.Global.DefaultWebPage = systemSettings["DefaultWebPage"].Value;
            Model.Global.CompanyName = systemSettings["CompanyName"].Value;
            Model.Global.CompanyAcronym = systemSettings["CompanyAcronym"].Value;
            Model.Global.ApplicationName = "openECAClient";
            Model.Global.ApplicationDescription = "open Extensible Control & Analytics Client";
            Model.Global.ApplicationKeywords = "open source, utility, software, analytics";
            Model.Global.DateFormat = systemSettings["DateFormat"].Value;
            Model.Global.TimeFormat = systemSettings["TimeFormat"].Value;
            Model.Global.DateTimeFormat = $"{Model.Global.DateFormat} {Model.Global.TimeFormat}";
            Model.Global.BootstrapTheme = systemSettings["BootstrapTheme"].Value;
        }

        // Static Properties

        // Static Methods

        #endregion
    }
}
