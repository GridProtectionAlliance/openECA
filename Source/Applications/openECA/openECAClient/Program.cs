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

namespace openECAClient
{
    static class Program
    {
        /// <summary>
        /// Defines common status message logging function for the application.
        /// </summary>
        public static readonly Action<string> LogStatus;

        /// <summary>
        /// Defines common exception logging function for the application.
        /// </summary>
        public static readonly Action<Exception> LogException;

        /// <summary>
        /// Defines common global settings for the application.
        /// </summary>
        public static readonly GlobalSettings Global;

        private static readonly MainWindow s_mainWindow;

        static Program()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            s_mainWindow = new MainWindow();
            LogStatus = s_mainWindow.LogStatus;
            LogException = s_mainWindow.LogException;
            Global = MainWindow.Model.Global;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(s_mainWindow);
        }
    }
}
