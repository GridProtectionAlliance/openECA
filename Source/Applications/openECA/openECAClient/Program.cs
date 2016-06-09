//******************************************************************************************************
//  Program.cs - Gbtc
//
//  Copyright © 2016, Grid Protection Alliance.  All Rights Reserved.
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
//  06/07/2016 - Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Windows.Forms;
using GSF.Diagnostics;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace openECAClient
{
    static class Program
    {
        /// <summary>
        /// Defines common global settings for the application.
        /// </summary>
        public static readonly GlobalSettings Global;

        /// <summary>
        /// Defines a common performance monitor for the application.
        /// </summary>
        public static readonly PerformanceMonitor PerformanceMonitor; 

        private static readonly MainWindow s_mainWindow;

        /// <summary>
        /// Gets the list of currently connected hub clients.
        /// </summary>
        public static IHubConnectionContext<dynamic> HubClients => s_clients.Value;

        private static readonly Lazy<IHubConnectionContext<dynamic>> s_clients = new Lazy<IHubConnectionContext<dynamic>>(() => GlobalHost.ConnectionManager.GetHubContext<DataHub>().Clients);

        static Program()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            s_mainWindow = new MainWindow();
            Global = MainWindow.Model.Global;

            PerformanceMonitor = new PerformanceMonitor();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(s_mainWindow);
        }

        /// <summary>
        /// Common status message logging function for the application.
        /// </summary>
        public static void LogStatus(string message, bool pushToHubClients = false)
        {
            s_mainWindow.LogStatus(message, pushToHubClients);
        }

        /// <summary>
        /// Common exception logging function for the application.
        /// </summary>
        public static void LogException(Exception ex, bool pushToHubClients = false)
        {
            s_mainWindow.LogException(ex, pushToHubClients);
        }
    }
}
