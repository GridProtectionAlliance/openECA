﻿//******************************************************************************************************
//  ServiceInstall.Designer.cs - Gbtc
//
//  Copyright © 2015, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  05/04/2009 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

namespace openECA
{
    partial class ServiceInstall
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

#if MONO
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            // 
            // ServiceInstall
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] { this.serviceProcessInstaller });
        }

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
#else
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstallerEx = new GSF.ServiceProcess.ServiceInstallerEx();
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            // 
            // serviceInstallerEx
            // 
            this.serviceInstallerEx.Description = "Provides processing for streaming time-series data in real-time including phasor " +
                "data concentration services for synchrophasor applications.";
            this.serviceInstallerEx.DisplayName = "openECA";
            this.serviceInstallerEx.ServiceName = "openECA";
            this.serviceInstallerEx.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ServiceInstall
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller,
            this.serviceInstallerEx});

        }

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
        private GSF.ServiceProcess.ServiceInstallerEx serviceInstallerEx;
#endif

        #endregion
    }
}