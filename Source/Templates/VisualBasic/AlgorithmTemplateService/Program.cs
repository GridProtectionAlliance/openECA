//******************************************************************************************************
//  Program.cs - Gbtc
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

#if !DEBUG
#define RunAsService
#endif

#if RunAsService
using System.ServiceProcess;
#else
using System.Windows.Forms;
#endif

using System;
using System.IO;
using System.Text;

namespace AlgorithmTemplateService
{
    static class Program
    {
        /// <summary>
        /// Main entry point for service.
        /// </summary>
        static void Main()
        {
#if RunAsService
            ServiceHost host = new ServiceHost();
            RedirectConsoleOutput(host);
            ServiceBase.Run(host);
#else
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DebugHost host = new DebugHost(new ServiceHost());
            RedirectConsoleOutput(host.ServiceHost);
            Application.Run(host);
#endif
        }

        // Intercept any console output and send to service host
        static void RedirectConsoleOutput(ServiceHost host)
        {
            Console.SetOut(new RemoteConsoleWriter(host));
            Console.SetError(new RemoteConsoleWriter(host, true));
        }

        private class RemoteConsoleWriter : TextWriter
        {
            private readonly ServiceHost m_host;
            private readonly StringBuilder m_message;
            private readonly bool m_targetError;

            public RemoteConsoleWriter(ServiceHost host, bool targetError = false)
            {
                m_host = host;
                m_message = new StringBuilder();
                m_targetError = targetError;
            }

            public override void Write(char value)
            {
                const string ErrorPrefix = "ERROR: ";

                m_message.Append(value);

                string message = m_message.ToString();

                if (message.EndsWith(Environment.NewLine))
                {
                    if (m_targetError)
                        m_host.HandleException(new InvalidOperationException(message));
                    else if (message.StartsWith(ErrorPrefix))
                        m_host.HandleException(new InvalidOperationException(message.Substring(ErrorPrefix.Length)));
                    else
                        m_host.BroadcastMessage(message);

                    m_message.Clear();
                }
            }

            public override Encoding Encoding => Console.OutputEncoding;
        }
    }
}