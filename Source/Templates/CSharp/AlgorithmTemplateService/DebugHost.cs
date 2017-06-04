//******************************************************************************************************
//  DebugHost.cs - Gbtc
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
using System.Diagnostics;
using System.Windows.Forms;
using GSF.IO;
using GSF.Reflection;

namespace AlgorithmTemplateService
{
    public partial class DebugHost : Form
    {
        #region [ Members ]

        // Fields
        private readonly ServiceHost m_serviceHost;
        private Process m_remoteConsole;
        private string m_productName;

        #endregion

        #region [ Constructors ]

        public DebugHost(ServiceHost serviceHost)
        {
            InitializeComponent();
            m_serviceHost = serviceHost;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the service host that this debug host is hosting.
        /// </summary>
        public ServiceHost ServiceHost => m_serviceHost;

        #endregion

        #region [ Methods ]

        private void DebugHost_Load(object sender, EventArgs e)
        {
            if (DesignMode)
                return;

            string serviceClientName = "AlgorithmTemplateServiceConsole.exe";

            if (!string.IsNullOrWhiteSpace(serviceClientName))
                m_remoteConsole = Process.Start(FilePath.GetAbsolutePath(serviceClientName));

            // Initialize text
            m_productName = AssemblyInfo.EntryAssembly.Title;
            Text = string.Format(Text, m_productName);
            m_notifyIcon.Text = string.Format(m_notifyIcon.Text, m_productName);
            LabelNotice.Text = string.Format(LabelNotice.Text, m_productName);
            m_exitToolStripMenuItem.Text = string.Format(m_exitToolStripMenuItem.Text, m_productName);

            // Minimize the window
            WindowState = FormWindowState.Minimized;

            // Start the windows service
            m_serviceHost.StartDebugging(Environment.CommandLine.Split(' '));
        }

        private void DebugHost_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DesignMode)
                return;

            if (MessageBox.Show($"Are you sure you want to stop {m_productName} debug service? ", "Stop Debug Service", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                m_serviceHost.StopDebugging();

                // Close remote console session
                if (m_remoteConsole != null && !m_remoteConsole.HasExited)
                    m_remoteConsole.Kill();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void DebugHost_Resize(object sender, EventArgs e)
        {
            // Don't show the window in taskbar when minimized
            if (WindowState == FormWindowState.Minimized)
                ShowInTaskbar = false;
        }

        private void ShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show the window in taskbar the in normal mode (visible)
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion
    }
}