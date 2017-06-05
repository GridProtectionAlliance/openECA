//******************************************************************************************************
//  ServiceHost.cs - Gbtc
//
//  Copyright © 2017, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  05/31/2017 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ECAClientFramework;
using GSF;
using GSF.Adapters;
using GSF.Configuration;
using GSF.Console;
using GSF.Diagnostics;
using GSF.IO;
using GSF.ServiceProcess;
using GSF.Threading;
using GSF.Units;

namespace AlgorithmTemplateService
{
    public partial class ServiceHost : ServiceBase
    {
        #region [ Members ]

        // Constants
        private const bool DefaultAllowRemoteRestart = true;
        private const bool DefaultAllowServiceMonitors = true;
        private const int DefaultMaxLogFiles = 300;
        private const string DefaultCulture = "en-US";

        // Fields
        private AdapterLoader<IServiceMonitor> m_serviceMonitors;
        private RunTimeLog m_runTimeLog;
        private bool m_allowRemoteRestart;
        private bool m_allowServiceMonitors;
        private Concentrator m_concentrator;
        private Subscriber m_subscriber;

        #endregion

        #region [ Constructors ]

        public ServiceHost()
        {
            InitializeComponent();

            // Register event handlers
            m_serviceHelper.ServiceStarting += ServiceHelper_ServiceStarting;
            m_serviceHelper.ServiceStarted += ServiceHelper_ServiceStarted;
            m_serviceHelper.ServiceStopping += ServiceHelper_ServiceStopping;
        }

        public ServiceHost(IContainer container) : this()
        {
            container?.Add(this);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the related remote console application name.
        /// </summary>
        protected virtual string ConsoleApplicationName => ServiceName + "Console.exe";

        /// <summary>
        /// Gets access to the <see cref="ServiceHelper"/>.
        /// </summary>
        protected ServiceHelper ServiceHelper => m_serviceHelper;

        #endregion

        #region [ Methods ]

        #region [ Service Event Handlers ]

        private void ServiceHelper_ServiceStarting(object sender, EventArgs<string[]> e)
        {
            ShutdownHandler.Initialize();

            // Define a run-time log
            m_runTimeLog = new RunTimeLog();
            m_runTimeLog.FileName = "RunTimeLog.txt";
            m_runTimeLog.ProcessException += ProcessExceptionHandler;
            m_runTimeLog.Initialize();

            // Create a handler for unobserved task exceptions
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Make sure default service settings exist
            ConfigurationFile configFile = ConfigurationFile.Current;

            string servicePath = FilePath.GetAbsolutePath("");
            string defaultLogPath = string.Format("{0}{1}Logs{1}", servicePath, Path.DirectorySeparatorChar);

            // Initialize algorithm processing framework - this will define default system settings
            try
            {
                AlgorithmHostingEnvironment.Initialize();
            }
            catch (Exception ex)
            {
                HandleException(new InvalidOperationException($"Exception while creating framework for algorithm hosting environment: {ex.Message}", ex));
            }

            CategorizedSettingsElementCollection systemSettings = configFile.Settings["systemSettings"];

            // Makes sure exepected system settings are defined in the configuration file
            systemSettings.Add("LogPath", defaultLogPath, "Defines the path used to archive log files");
            systemSettings.Add("MaxLogFiles", DefaultMaxLogFiles, "Defines the maximum number of log files to keep");
            systemSettings.Add("AllowRemoteRestart", DefaultAllowRemoteRestart, "Controls ability to remotely restart the host service.");
            systemSettings.Add("AllowServiceMonitors", DefaultAllowServiceMonitors, "Controls ability to auto-load IServiceMonitor implementations.");
            systemSettings.Add("DefaultCulture", DefaultCulture, "Default culture to use for language, country/region and calendar formats.");
            systemSettings.Add("InputMapping", SystemSettings.InputMapping, "Mnput mapping used by algorithm for incoming data.");
            systemSettings.Add("OutputMapping", SystemSettings.OutputMapping, "Mapping used by algorithm for outgoing data.");
            systemSettings.Add("ConnectionString", SystemSettings.ConnectionString, "Connection string used by algorithm to connect to openECA data source.");
            systemSettings.Add("FramesPerSecond", SystemSettings.FramesPerSecond, "Data rate, in frames per second, expected by algorithm.");
            systemSettings.Add("LagTime", SystemSettings.LagTime, "Maximum past-time deviation tolerance, in seconds (can be sub-second), that the algorithm will tolerate.");
            systemSettings.Add("LeadTime", SystemSettings.LeadTime, "Maximum future-time deviation tolerance, in seconds (can be sub-second), that the algorithm will tolerate.");

            // Attempt to set default culture
            try
            {
                string defaultCulture = systemSettings["DefaultCulture"].ValueAs(DefaultCulture);
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture(defaultCulture);     // Defaults for date formatting, etc.
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CreateSpecificCulture(defaultCulture);   // Culture for resource strings, etc.
            }
            catch (Exception ex)
            {
                HandleException(new InvalidOperationException($"Failed to set default culture due to exception, defaulting to \"{CultureInfo.CurrentCulture.Name.ToNonNullNorEmptyString("Undetermined")}\": {ex.Message}", ex));
            }

            // Retrieve application log path as defined in the config file
            string logPath = FilePath.GetAbsolutePath(systemSettings["LogPath"].Value);

            // Make sure log directory exists
            try
            {
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);
            }
            catch (Exception ex)
            {
                // Attempt to default back to common log file path
                if (!Directory.Exists(defaultLogPath))
                {
                    try
                    {
                        Directory.CreateDirectory(defaultLogPath);
                    }
                    catch
                    {
                        defaultLogPath = servicePath;
                    }
                }

                HandleException(new InvalidOperationException($"Failed to create logging directory \"{logPath}\" due to exception, defaulting to \"{defaultLogPath}\": {ex.Message}", ex));
                logPath = defaultLogPath;
            }

            int maxLogFiles = systemSettings["MaxLogFiles"].ValueAs(DefaultMaxLogFiles);

            try
            {
                Logger.FileWriter.SetPath(logPath);
                Logger.FileWriter.SetLoggingFileCount(maxLogFiles);
            }
            catch (Exception ex)
            {
                HandleException(new InvalidOperationException($"Failed to set logging path \"{logPath}\" or max file count \"{maxLogFiles}\" due to exception: {ex.Message}"));
            }

            try
            {
                Directory.SetCurrentDirectory(servicePath);
            }
            catch (Exception ex)
            {
                HandleException(new InvalidOperationException($"Failed to set current directory to execution path \"{servicePath}\" due to exception: {ex.Message}"));
            }

            // Initialize system settings as defined in configuration file
            m_allowRemoteRestart = systemSettings["AllowRemoteRestart"].ValueAs(DefaultAllowRemoteRestart);
            m_allowServiceMonitors = systemSettings["AllowServiceMonitors"].ValueAs(DefaultAllowServiceMonitors);
            SystemSettings.InputMapping = systemSettings["InputMapping"].ValueAs(SystemSettings.InputMapping);
            SystemSettings.OutputMapping = systemSettings["OutputMapping"].ValueAs(SystemSettings.OutputMapping);
            SystemSettings.ConnectionString = systemSettings["ConnectionString"].ValueAs(SystemSettings.ConnectionString);
            SystemSettings.FramesPerSecond = systemSettings["FramesPerSecond"].ValueAs(SystemSettings.FramesPerSecond);
            SystemSettings.LagTime = systemSettings["LagTime"].ValueAs(SystemSettings.LagTime);
            SystemSettings.LeadTime = systemSettings["LeadTime"].ValueAs(SystemSettings.LeadTime);
        }

        private void ServiceHelper_ServiceStarted(object sender, EventArgs e)
        {
            // Define a line of asterisks for emphasis
            string stars = new string('*', 79);

            // Get current process memory usage
            long processMemory = Common.GetProcessMemory();

            // Log startup information
            m_serviceHelper.UpdateStatus(
                UpdateType.Information,
                "{14}{14}{0}{14}{14}" +
                "{1} Initializing{14}{14}" +
                "     System Time: {2} UTC{14}{14}" +
                "    Current Path: {3}{14}{14}" +
                "    Machine Name: {4}{14}{14}" +
                "      OS Version: {5}{14}{14}" +
                "    Product Name: {6}{14}{14}" +
                "  Working Memory: {7}{14}{14}" +
                "  Execution Mode: {8}-bit{14}{14}" +
                "      Processors: {9}{14}{14}" +
                "  GC Server Mode: {10}{14}{14}" +
                " GC Latency Mode: {11}{14}{14}" +
                " Process Account: {12}\\{13}{14}{14}" +
                "{0}{14}",
                stars,
                ServiceName,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                FilePath.TrimFileName(FilePath.RemovePathSuffix(FilePath.GetAbsolutePath("")), 61),
                Environment.MachineName,
                Environment.OSVersion.VersionString,
                Common.GetOSProductName(),
                processMemory > 0 ? SI2.ToScaledString(processMemory, 4, "B", SI2.IECSymbols) : "Undetermined",
                IntPtr.Size * 8,
                Environment.ProcessorCount,
                GCSettings.IsServerGC,
                GCSettings.LatencyMode,
                Environment.UserDomainName,
                Environment.UserName,
                Environment.NewLine);

            // Add run-time log as a service component
            m_serviceHelper.ServiceComponents.Add(m_runTimeLog);

            // Define scheduled service processes
            m_serviceHelper.AddScheduledProcess(ServiceHeartbeatHandler, "ServiceHeartbeat", "* * * * *");

            // Define remote client service command requests (i.e., console commands)
            m_serviceHelper.ClientRequestHandlers.Add(new ClientRequestHandler("Restart", "Attempts to restart the host service", RestartServiceHandler));
            m_serviceHelper.ClientRequestHandlers.Add(new ClientRequestHandler("Initialize", "Attempts to re-initialize algorithm processing framework.", ReinitializeAlgorithmHandler, new[] { "Reinitialize" }));
            m_serviceHelper.ClientRequestHandlers.Add(new ClientRequestHandler("Status", "Gets current algorithm processing framework status.", FrameworkStatusHandler, new[] { "list", "ls" }));
            m_serviceHelper.ClientRequestHandlers.Add(new ClientRequestHandler("LogEvent", "Logs remote event log entries.", LogEventRequestHandler, false));

            if (m_allowServiceMonitors)
            {
                // Establish plug-in service monitoring architecture - external implementations of IServiceMonitor will be auto-loaded
                m_serviceMonitors = new AdapterLoader<IServiceMonitor>();
                m_serviceMonitors.AdapterCreated += ServiceMonitors_AdapterCreated;
                m_serviceMonitors.AdapterLoaded += ServiceMonitors_AdapterLoaded;
                m_serviceMonitors.AdapterUnloaded += ServiceMonitors_AdapterUnloaded;
                m_serviceMonitors.Initialize();

                // Add service monitoring command
                m_serviceHelper.ClientRequestHandlers.Add(new ClientRequestHandler("NotifyMonitors", "Sends a message to all service monitors", NotifyMonitorsRequestHandler));
            }

            // Start algorithm processing framework
            StartAlgorithmProcessing();

            // If any settings have been added to configuration file, we go ahead and save them now
            m_serviceHelper.SaveSettings(true);
            ConfigurationFile.Current.Save();
        }

        private void ServiceHelper_ServiceStopping(object sender, EventArgs eventArgs)
        {
            // Shutdown algorithm processing framework
            StopAlgorithmProcessing();

            // Dispose of run-time log
            if ((object)m_runTimeLog != null)
            {
                m_serviceHelper.ServiceComponents.Remove(m_runTimeLog);
                m_runTimeLog.ProcessException -= ProcessExceptionHandler;
                m_runTimeLog.Dispose();
                m_runTimeLog = null;
            }

            if (m_allowServiceMonitors && (object)m_serviceMonitors != null)
            {
                m_serviceMonitors.AdapterLoaded -= ServiceMonitors_AdapterLoaded;
                m_serviceMonitors.AdapterUnloaded -= ServiceMonitors_AdapterUnloaded;
                m_serviceMonitors.Dispose();
            }

            // Deregister event handlers
            m_serviceHelper.ServiceStarting -= ServiceHelper_ServiceStarting;
            m_serviceHelper.ServiceStarted -= ServiceHelper_ServiceStarted;
            m_serviceHelper.ServiceStopping -= ServiceHelper_ServiceStopping;

            // Detach from handler for unobserved task exceptions
            TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

            ShutdownHandler.InitiateSafeShutdown();
        }

        private void StartAlgorithmProcessing()
        {
            try
            {
                AlgorithmHostingEnvironment.Start();

                Framework framework = AlgorithmHostingEnvironment.Framework;
                m_concentrator = framework.Concentrator;
                m_subscriber = framework.Subscriber;

                m_concentrator.ProcessException += ProcessExceptionHandler;
                m_concentrator.FramesPerSecond = SystemSettings.FramesPerSecond;
                m_concentrator.LagTime = SystemSettings.LagTime;
                m_concentrator.LeadTime = SystemSettings.LeadTime;
                m_concentrator.RoundToNearestTimestamp = true;
                m_concentrator.Start();

                m_subscriber.StatusMessage += StatusMessageHandler;
                m_subscriber.ProcessException += ProcessExceptionHandler;
                m_subscriber.Start();
            }
            catch (Exception ex)
            {
                HandleException(new InvalidOperationException($"Exception while creating framework for algorithm hosting environment: {ex.Message}", ex));
            }
        }

        private void StopAlgorithmProcessing()
        {
            try
            {
                if ((object)m_subscriber != null)
                {
                    m_subscriber.Stop();
                    m_subscriber.StatusMessage -= StatusMessageHandler;
                    m_subscriber.ProcessException -= ProcessExceptionHandler;
                    m_subscriber = null;
                }

                if ((object)m_concentrator != null)
                {
                    m_concentrator.Stop();
                    m_concentrator.ProcessException -= ProcessExceptionHandler;
                    m_concentrator = null;
                }

                AlgorithmHostingEnvironment.Shutdown();
            }
            catch (Exception ex)
            {
                HandleException(new InvalidOperationException($"Exception while shutting down algorithm hosting environment: {ex.Message}", ex));
            }
        }

        #endregion

        #region [ Service Command Request Handlers ]

        // Attempts to restart the host service
        private void RestartServiceHandler(ClientRequestInfo requestInfo)
        {
            if (requestInfo.Request.Arguments.ContainsHelpRequest)
            {
                StringBuilder helpMessage = new StringBuilder();

                helpMessage.Append("Attempts to restart the host service.");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Usage:");
                helpMessage.AppendLine();
                helpMessage.Append("       Restart [Options]");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Options:");
                helpMessage.AppendLine();
                helpMessage.Append("       -?".PadRight(20));
                helpMessage.Append("Displays this help message");

                DisplayResponseMessage(requestInfo, helpMessage.ToString());
            }
            else
            {
                if (m_allowRemoteRestart)
                {
                    BroadcastMessage("Attempting to restart host service...");

                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(ConsoleApplicationName)
                        {
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = false,
                            Arguments = ServiceName + " -restart"
                        };

                        using (Process shell = new Process())
                        {
                            shell.StartInfo = psi;
                            shell.Start();

                            if (!shell.WaitForExit(30000))
                                shell.Kill();
                        }

                        SendResponse(requestInfo, true);
                    }
                    catch (Exception ex)
                    {
                        SendResponse(requestInfo, false, "Failed to restart host service: {0}", ex.Message);
                        HandleException(ex);
                    }
                }
                else
                {
                    BroadcastMessage("Remote restart request denied, this is currently disallowed in the system configuration.", UpdateType.Warning);
                }
            }
        }

        // Handles request to re-initialize algorithm processing framework
        private void ReinitializeAlgorithmHandler(ClientRequestInfo requestInfo)
        {
            if (requestInfo.Request.Arguments.ContainsHelpRequest)
            {
                StringBuilder helpMessage = new StringBuilder();

                helpMessage.Append("Reinitializes algorithm processing framework.");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Usage:");
                helpMessage.AppendLine();
                helpMessage.Append("       [Re]Initialize [Options]");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Options:");
                helpMessage.AppendLine();
                helpMessage.Append("       -?".PadRight(20));
                helpMessage.Append("Displays this help message");

                DisplayResponseMessage(requestInfo, helpMessage.ToString());
            }
            else
            {
                try
                {
                    BroadcastMessage($"Attempting to re-initialize algorithm processing framework...{Environment.NewLine}");

                    StopAlgorithmProcessing();
                    StartAlgorithmProcessing();

                    BroadcastMessage($"{Environment.NewLine}Successfully re-initialized algorithm processing framework.");
                }
                catch (Exception ex)
                {
                    HandleException(new InvalidOperationException($"Failed to re-initialize algorithm processing framework: {ex.Message}", ex));
                }
            }
        }

        // Handles request to get algorithm framework status
        private void FrameworkStatusHandler(ClientRequestInfo requestInfo)
        {
            if (requestInfo.Request.Arguments.ContainsHelpRequest)
            {
                StringBuilder helpMessage = new StringBuilder();

                helpMessage.Append("Gets algorithm processing framework status.");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Usage:");
                helpMessage.AppendLine();
                helpMessage.Append("       Status [Options]");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Options:");
                helpMessage.AppendLine();
                helpMessage.Append("       -?".PadRight(20));
                helpMessage.Append("Displays this help message");

                DisplayResponseMessage(requestInfo, helpMessage.ToString());
            }
            else
            {
                try
                {
                    StringBuilder status = new StringBuilder();

                    if ((object)m_concentrator != null)
                        status.AppendLine($"Concentrator Status:{Environment.NewLine}{m_concentrator.Status}");

                    if ((object)m_subscriber != null)
                        status.AppendLine($"Subscriber Status:{Environment.NewLine}{m_subscriber.Status}");

                    DisplayResponseMessage(requestInfo, status.ToString());
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Failed to get algorithm processing framework status: {ex.Message}";
                    DisplayResponseMessage(requestInfo, errorMessage);
                    HandleException(new InvalidOperationException(errorMessage, ex), false);
                }
            }
        }

        // Send a message to the service monitors on request
        private void NotifyMonitorsRequestHandler(ClientRequestInfo requestInfo)
        {
            Arguments arguments = requestInfo.Request.Arguments;

            if (arguments.ContainsHelpRequest)
            {
                StringBuilder helpMessage = new StringBuilder();

                helpMessage.Append("Sends a message to all service monitors.");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Usage:");
                helpMessage.AppendLine();
                helpMessage.Append("       NotifyMonitors [Options] [Args...]");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Options:");
                helpMessage.AppendLine();
                helpMessage.Append("       -?".PadRight(20));
                helpMessage.Append("Displays this help message");

                DisplayResponseMessage(requestInfo, helpMessage.ToString());
            }
            else
            {
                string[] args = Enumerable.Range(1, arguments.OrderedArgCount)
                    .Select(arg => arguments[arguments.OrderedArgID + arg])
                    .ToArray();

                // Go through all service monitors and handle the message
                foreach (IServiceMonitor serviceMonitor in m_serviceMonitors.Adapters)
                {
                    try
                    {
                        // If the service monitor is enabled, notify it of the message
                        if (serviceMonitor.Enabled)
                            serviceMonitor.HandleClientMessage(args);
                    }
                    catch (Exception ex)
                    {
                        // Handle each service monitor's exceptions individually
                        HandleException(ex);
                    }
                }

                SendResponse(requestInfo, true);
            }
        }

        // Sends remote entry for logging
        private void LogEventRequestHandler(ClientRequestInfo requestInfo)
        {
            if (requestInfo.Request.Arguments.ContainsHelpRequest)
            {
                StringBuilder helpMessage = new StringBuilder();

                helpMessage.Append("Logs remote entry to event log.");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Usage:");
                helpMessage.AppendLine();
                helpMessage.Append("       LogEvent [Options]");
                helpMessage.AppendLine();
                helpMessage.AppendLine();
                helpMessage.Append("   Options:");
                helpMessage.AppendLine();
                helpMessage.Append("       -?".PadRight(20));
                helpMessage.Append("Displays this help message");
                helpMessage.AppendLine();
                helpMessage.Append("       -Message=\"Event Message\"".PadRight(20));
                helpMessage.Append("Specifies message for event log entry (required)");
                helpMessage.AppendLine();
                helpMessage.Append("       -Type=[Error|Warning|Information|...]".PadRight(20));
                helpMessage.Append("Specifies EventLogEntryType setting (optional)");
                helpMessage.AppendLine();
                helpMessage.Append("       -ID=0".PadRight(20));
                helpMessage.Append("Specifies application event log ID (optional)");
                helpMessage.AppendLine();

                DisplayResponseMessage(requestInfo, helpMessage.ToString());
            }
            else
            {
                if (requestInfo.Request.Arguments.Exists("Message"))
                {
                    try
                    {
                        string message = requestInfo.Request.Arguments["Message"];
                        string type, id;
                        EventLogEntryType entryType;
                        ushort eventID;

                        if (!(requestInfo.Request.Arguments.TryGetValue("Type", out type) && Enum.TryParse(type, out entryType)))
                            entryType = EventLogEntryType.Information;

                        if (!(requestInfo.Request.Arguments.TryGetValue("ID", out id) && ushort.TryParse(id, out eventID)))
                            eventID = 0;

                        EventLog.WriteEntry(ServiceName, message, entryType, eventID);
                        SendResponse(requestInfo, true, "Successfully wrote event log entry.");
                    }
                    catch (Exception ex)
                    {
                        SendResponse(requestInfo, false, "Failed to write event log entry: {0}", ex.Message);
                    }
                }
                else
                {
                    SendResponse(requestInfo, false, "Failed to write event log entry: required \"message\" parameter was not specified.");
                }
            }
        }

        #endregion

        #region [ Service Monitor Handlers ]

        private void ServiceMonitors_AdapterCreated(object sender, EventArgs<IServiceMonitor> e)
        {
            e.Argument.PersistSettings = true;
        }

        private void ServiceMonitors_AdapterLoaded(object sender, EventArgs<IServiceMonitor> e)
        {
            m_serviceHelper.UpdateStatusAppendLine(UpdateType.Information, "{0} has been loaded", e.Argument.GetType().Name);
        }

        private void ServiceMonitors_AdapterUnloaded(object sender, EventArgs<IServiceMonitor> e)
        {
            m_serviceHelper.UpdateStatusAppendLine(UpdateType.Information, "{0} has been unloaded", e.Argument.GetType().Name);
        }

        private void ServiceHeartbeatHandler(string s, object[] args)
        {
            foreach (IServiceMonitor serviceMonitor in m_serviceMonitors.Adapters)
            {
                try
                {
                    if (serviceMonitor.Enabled)
                        serviceMonitor.HandleServiceHeartbeat();
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }

            const string RequestCommand = "Health";
            ClientRequestHandler requestHandler = m_serviceHelper.FindClientRequestHandler(RequestCommand);
            requestHandler?.HandlerMethod(ClientHelper.PretendRequest(RequestCommand));
        }

        #endregion

        #region [ Service Exception Handlers ]

        /// <summary>
        /// Handles an exception encountered by the service.
        /// </summary>
        /// <param name="ex">Exception to handle.</param>
        /// <param name="broadcastError">Flag that determines if error message should be broadcasted to all clients.</param>
        internal void HandleException(Exception ex, bool broadcastError = true)
        {
            string newLines = string.Format("{0}{0}", Environment.NewLine);

            m_serviceHelper.LogException(ex);

            if (broadcastError)
                m_serviceHelper.UpdateStatus(UpdateType.Alarm, ex.Message + newLines);

            foreach (IServiceMonitor serviceMonitor in m_serviceMonitors.Adapters)
            {
                try
                {
                    if (serviceMonitor.Enabled)
                        serviceMonitor.HandleServiceError(ex);
                }
                catch (Exception handlerEx)
                {
                    m_serviceHelper.ErrorLogger.Log(handlerEx);
                    m_serviceHelper.UpdateStatus(UpdateType.Alarm, handlerEx.Message + newLines);
                }
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            foreach (Exception ex in e.Exception.Flatten().InnerExceptions)
                HandleException(ex);

            e.SetObserved();
        }

        private void ProcessExceptionHandler(object sender, EventArgs<Exception> e)
        {
            HandleException(e.Argument);
        }

        #endregion

        #region [ Service Messaging Handlers ]

        /// <summary>
        /// Sends a message to all connected clients.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <param name="type">Update message type.</param>
        /// <param name="publishToLog">Flag that determines if message should go to log.</param>
        internal void BroadcastMessage(string message, UpdateType type = UpdateType.Information, bool publishToLog = true)
        {
            m_serviceHelper.UpdateStatus(type, publishToLog, "{0}{1}", message, Environment.NewLine);
        }

        /// <summary>
        /// Sends an actionable response to client.
        /// </summary>
        /// <param name="requestInfo"><see cref="ClientRequestInfo"/> instance containing the client request.</param>
        /// <param name="success">Flag that determines if this response to client request was a success.</param>
        private void SendResponse(ClientRequestInfo requestInfo, bool success)
        {
            SendResponseWithAttachment(requestInfo, success, null, null);
        }

        /// <summary>
        /// Sends an actionable response to client with a formatted message.
        /// </summary>
        /// <param name="requestInfo"><see cref="ClientRequestInfo"/> instance containing the client request.</param>
        /// <param name="success">Flag that determines if this response to client request was a success.</param>
        /// <param name="status">Formatted status message to send with response.</param>
        /// <param name="args">Arguments of the formatted status message.</param>
        private void SendResponse(ClientRequestInfo requestInfo, bool success, string status, params object[] args)
        {
            SendResponseWithAttachment(requestInfo, success, null, status, args);
        }

        /// <summary>
        /// Sends an actionable response to client with a formatted message and attachment.
        /// </summary>
        /// <param name="requestInfo"><see cref="ClientRequestInfo"/> instance containing the client request.</param>
        /// <param name="success">Flag that determines if this response to client request was a success.</param>
        /// <param name="attachment">Attachment to send with response.</param>
        /// <param name="status">Formatted status message to send with response.</param>
        /// <param name="args">Arguments of the formatted status message.</param>
        private void SendResponseWithAttachment(ClientRequestInfo requestInfo, bool success, object attachment, string status, params object[] args)
        {
            try
            {
                // Send actionable response
                m_serviceHelper.SendActionableResponse(requestInfo, success, attachment, status, args);

                // Log details of client request as well as response
                if (m_serviceHelper.LogStatusUpdates && m_serviceHelper.StatusLog.IsOpen)
                {
                    string responseType = requestInfo.Request.Command + (success ? ":Success" : ":Failure");
                    string arguments = requestInfo.Request.Arguments.ToString();
                    string message = responseType + (string.IsNullOrWhiteSpace(arguments) ? "" : "(" + arguments + ")");

                    if (status != null)
                    {
                        if (args.Length == 0)
                            message += " - " + status;
                        else
                            message += " - " + string.Format(status, args);
                    }

                    m_serviceHelper.StatusLog.WriteTimestampedLine(message);
                }
            }
            catch (Exception ex)
            {
                string message = $"Failed to send client response due to an exception: {ex.Message}";
                HandleException(new InvalidOperationException(message, ex));
            }
        }

        /// <summary>
        /// Displays a response message to client requestor.
        /// </summary>
        /// <param name="requestInfo"><see cref="ClientRequestInfo"/> instance containing the client request.</param>
        /// <param name="status">Formatted status message to send to client.</param>
        /// <param name="args">Arguments of the formatted status message.</param>
        private void DisplayResponseMessage(ClientRequestInfo requestInfo, string status, params object[] args)
        {
            try
            {
                m_serviceHelper.UpdateStatus(requestInfo.Sender.ClientID, UpdateType.Information, $"{status}{Environment.NewLine}{Environment.NewLine}", args);
            }
            catch (Exception ex)
            {
                string message = $"Failed to update client status \"{status.ToNonNullString()}\" due to an exception: {ex.Message}";
                HandleException(new InvalidOperationException(message, ex));
            }
        }

        private void StatusMessageHandler(object sender, EventArgs<string> e)
        {
            BroadcastMessage(e.Argument);
        }

        #endregion

        #endregion
    }
}