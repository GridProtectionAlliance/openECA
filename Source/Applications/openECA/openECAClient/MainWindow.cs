﻿//******************************************************************************************************
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ECACommonUtilities;
using ECACommonUtilities.Model;
using GSF;
using GSF.Configuration;
using GSF.IO;
using GSF.Reflection;
using GSF.Web.Hosting;
using GSF.Web.Model;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Hosting;
using openECAClient.Model;

namespace openECAClient
{
    public partial class MainWindow : Form
    {
        #region [ Members ]

        // Fields
        private int m_maxLines = 1000;
        private IDisposable m_webAppHost;

        #endregion

        #region [ Constructors ]

        public MainWindow()
        {
            InitializeComponent();

            string errorLogPath = ErrorLogger.ErrorLog.FileName;

            if (!Path.IsPathRooted(errorLogPath))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string ecaClientDataPath = Path.Combine(appData, "Grid Protection Alliance", "openECAClient");
                ErrorLogger.ErrorLog.FileName = Path.Combine(ecaClientDataPath, errorLogPath);
            }
        }

        #endregion

        #region [ Methods ]

        private void MainWindow_Load(object sender, EventArgs e)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                Model.Global.DefaultProjectPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(Model.Global.DefaultProjectPath));
                Directory.CreateDirectory(Model.Global.DefaultProjectPath);
            }
            catch (Exception ex)
            {
                LogException(new InvalidOperationException($"Failed to initialize default project path: {ex.Message}", ex));
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }

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

        private void MessagesTextBox_SizeChanged(object sender, EventArgs e)
        {
            // Sometimes the scrollbar will fail to update or scroll
            // beyond the bottom of the text box when the text box
            // is resized. Scrolling to the top and then back to the
            // bottom fixes this problem
            MessagesTextBox.Select(0, 0);
            MessagesTextBox.ScrollToCaret();
            MessagesTextBox.Select(MessagesTextBox.TextLength, 0);
            MessagesTextBox.ScrollToCaret();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show(this, $"Stopping application will terminate openECA Data Modeling Manager web functionality. Are you sure you want to stop the {Text}?", $"Shutdown {Text}...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                e.Cancel = true;
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if ((object)m_webAppHost != null)
                m_webAppHost.Dispose();

            ErrorLogger.Dispose();
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OpenWebButton_Click(object sender, EventArgs e)
        {
            // Open the main page in the user's default browser
            using (Process.Start(Model.Global.WebHostURL)) { }
        }

        private void WebServer_StatusMessage(object sender, EventArgs<string> e)
        {
            LogStatus(e.Argument);
        }

        private void LoggedExceptionHandler(object sender, EventArgs<Exception> e)
        {
            LogException(e.Argument);
        }

        private void LogStatus(string message)
        {
            LogStatus(message, false);
        }

        internal void LogStatus(string message, bool pushToHubClients)
        {
            if (pushToHubClients)
                ThreadPool.QueueUserWorkItem(state =>
                {
                    string connectionID = state as string;

                    if (!string.IsNullOrEmpty(connectionID))
                    {
                        Program.HubClients.Client(connectionID).sendInfoMessage(message, 3000);
                    }
                    else
                    {
                        Thread.Sleep(1500);
                        Program.HubClients.All.sendInfoMessage(message, 3000);
                    }
                }, 
                DataHub.CurrentConnectionID);

            DisplayText(message);
        }

        private void LogException(Exception ex)
        {
            LogException(ex, false);
        }

        internal void LogException(Exception ex, bool pushToHubClients)
        {
            if (pushToHubClients)
                ThreadPool.QueueUserWorkItem(state =>
                {
                    string connectionID = state as string;

                    if (!string.IsNullOrEmpty(connectionID))
                    {
                        Program.HubClients.Client(connectionID).sendErrorMessage(ex.Message, -1);
                    }
                    else
                    {
                        Thread.Sleep(1500);
                        Program.HubClients.All.sendErrorMessage(ex.Message, -1);
                    }
                }, 
                DataHub.CurrentConnectionID);

            ErrorLogger.Log(ex);
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

            MessagesTextBox.ReadOnly = false;
            MessagesTextBox.SelectedText = "";
            MessagesTextBox.ReadOnly = true;

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

            MessagesTextBox.ReadOnly = false;
            MessagesTextBox.SelectedText = "";
            MessagesTextBox.ReadOnly = true;

            // Scroll to bottom
            MessagesTextBox.SelectionStart = MessagesTextBox.TextLength;
            MessagesTextBox.ScrollToCaret();
        }

        #endregion

        #region [ Static ]

        // Static Fields
        public static readonly AppModel Model;

        // Static Constructor

        static MainWindow()
        {
            CategorizedSettingsElementCollection systemSettings = ConfigurationFile.Current.Settings["systemSettings"];

            systemSettings.Add("WebHostURL", "http://localhost:8080", "The web hosting URL for user interface operation. For increased security, only bind to localhost.");
            systemSettings.Add("DefaultWebPage", "Index.cshtml", "Determines if cache control is enabled for browser clients.");
            systemSettings.Add("CompanyName", "Grid Protection Alliance", "The name of the company who owns this instance of the openMIC.");
            systemSettings.Add("CompanyAcronym", "GPA", "The acronym representing the company who owns this instance of the openMIC.");
            systemSettings.Add("DateFormat", "MM/dd/yyyy", "The default date format to use when rendering timestamps.");
            systemSettings.Add("TimeFormat", "HH:mm.ss.fff", "The default time format to use when rendering timestamps.");
            systemSettings.Add("BootstrapTheme", "Content/bootstrap.min.css", "Path to Bootstrap CSS to use for rendering styles.", false, SettingScope.User);
            systemSettings.Add("SubscriptionConnectionString", "server=localhost:6190; interface=0.0.0.0", "Connection string for data subscriptions to openECA server.", false, SettingScope.User);
            systemSettings.Add("DefaultProjectPath", "openECA Projects", "Default path on which to store the user's projects.", false, SettingScope.User);

            Model = new AppModel();
            Model.Global.WebHostURL = systemSettings["WebHostURL"].Value;
            Model.Global.DefaultWebPage = systemSettings["DefaultWebPage"].Value;
            Model.Global.CompanyName = systemSettings["CompanyName"].Value;
            Model.Global.CompanyAcronym = systemSettings["CompanyAcronym"].Value;
            Model.Global.ApplicationName = "openECA Data Modeling Manager";
            Model.Global.ApplicationDescription = "open Extensible Control & Analytics Client";
            Model.Global.ApplicationKeywords = "open source, utility, software, analytics";
            Model.Global.DateFormat = systemSettings["DateFormat"].Value;
            Model.Global.TimeFormat = systemSettings["TimeFormat"].Value;
            Model.Global.DateTimeFormat = $"{Model.Global.DateFormat} {Model.Global.TimeFormat}";
            Model.Global.BootstrapTheme = systemSettings["BootstrapTheme"].Value;
            Model.Global.SubscriptionConnectionString = systemSettings["SubscriptionConnectionString"].Value;
            Model.Global.DefaultProjectPath = FilePath.AddPathSuffix(systemSettings["DefaultProjectPath"].Value);

        }

        public static void CheckPhasorTypesAndMappings()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string ecaClientDataPath = Path.Combine(appData, "Grid Protection Alliance", "openECAClient");
            string udtDirectory = Path.Combine(ecaClientDataPath, "UserDefinedTypes");
            string udmDirectory = Path.Combine(ecaClientDataPath, "UserDefinedMappings");

            UDTCompiler udtCompiler = new UDTCompiler();

            if (Directory.Exists(udtDirectory))
                udtCompiler.Scan(udtDirectory);

            if (!udtCompiler.DefinedTypes.Where(x => x.IsUserDefined).ToList().Any(x => x.Category == "ECA" && x.Identifier == "Phasor"))
            {
                UserDefinedType udt = new UserDefinedType();
                udt.Identifier = "Phasor";
                udt.Category = "ECA";
                udt.Fields = new List<UDTField>();
                UDTField magnitude = new UDTField();
                magnitude.Type = new DataType() { Category = "FloatingPoint", Identifier = "Double" };
                magnitude.Identifier = "Magnitude";
                udt.Fields.Add(magnitude);
                UDTField angle = new UDTField();
                angle.Type = new DataType() { Category = "FloatingPoint", Identifier = "Double" };
                angle.Identifier = "Angle";
                udt.Fields.Add(angle);
                UDTWriter udtWriter = new UDTWriter();

                udtWriter.Types.Add(udt);

                udtWriter.WriteFiles(udtDirectory);
            }

            udtCompiler = new UDTCompiler();

            if (Directory.Exists(udtDirectory))
                udtCompiler.Scan(udtDirectory);

            MappingCompiler mappingCompiler = new MappingCompiler(udtCompiler);

            if(Directory.Exists(udmDirectory))
                mappingCompiler.Scan(udmDirectory);

            DataHub dataHub = new DataHub();

            dataHub.Context = new HubCallerContext(null, Guid.NewGuid().ToString());

            dataHub.RegisterMetadataRecieved(() =>
            {
                IEnumerable<PhasorDetail> phasorDetails = dataHub.GetPhasorDetails();
                List<MeasurementDetail> measurementDetails = dataHub.GetMeasurementDetails().ToList();
                MappingWriter mappingWriter = new MappingWriter();

                foreach (PhasorDetail pd in phasorDetails)
                {
                    string identifier = (pd.DeviceAcronym + '_' +
                                         pd.Label + '_' +
                                         pd.Phase.Replace(" ", "_").Replace("+", "pos").Replace("-", "neg") + '_' +
                                         pd.Type)
                                         .Replace(" ", "_").Replace("\\", "_").Replace("/", "_").Replace("!", "_").Replace("-", "_").Replace("#", "").Replace("'", "").Replace("(","").Replace(")","");

                    if (!mappingCompiler.DefinedMappings.Any(x => x.Identifier == identifier))
                    {
                        TypeMapping tm = new TypeMapping();
                        tm.Identifier = identifier;
                        tm.Type = (UserDefinedType)udtCompiler.DefinedTypes.Find(x => x.Category == "ECA" && x.Identifier == "Phasor");
                        tm.FieldMappings.Add(new FieldMapping() { Field = tm.Type.Fields[0], Expression = measurementDetails.Find(x => x.DeviceAcronym == pd.DeviceAcronym && x.PhasorSourceIndex == pd.SourceIndex && x.SignalAcronym.Contains("PHM")).SignalID.ToString() });
                        tm.FieldMappings.Add(new FieldMapping() { Field = tm.Type.Fields[1], Expression = measurementDetails.Find(x => x.DeviceAcronym == pd.DeviceAcronym && x.PhasorSourceIndex == pd.SourceIndex && x.SignalAcronym.Contains("PHA")).SignalID.ToString() });
                        mappingWriter.Mappings.Add(tm);
                    }
                }

                mappingWriter.WriteFiles(udmDirectory);
            });

            dataHub.InitializeSubscriptions();
        }


        #endregion
    }
}
