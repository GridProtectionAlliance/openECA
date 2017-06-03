﻿namespace AlgorithmTemplateService
{
    partial class ServiceClient
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
            if (disposing)
            {
                m_clientHelper.Disconnect();
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.m_clientHelper = new GSF.ServiceProcess.ClientHelper(this.components);
            this.m_remotingClient = new GSF.Communication.TcpClient(this.components);
            this.m_errorLogger = new GSF.ErrorManagement.ErrorLogger(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.m_clientHelper)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_remotingClient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_errorLogger)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_errorLogger.ErrorLog)).BeginInit();
            // 
            // m_clientHelper
            // 
            this.m_clientHelper.PersistSettings = true;
            this.m_clientHelper.RemotingClient = this.m_remotingClient;
            // 
            // m_remotingClient
            // 
            this.m_remotingClient.ConnectionString = "server=localhost:[REMOTE_CONSOLE_PORT]; interface=0.0.0.0";
            this.m_remotingClient.IntegratedSecurity = true;
            this.m_remotingClient.NetworkCredential = null;
            this.m_remotingClient.PayloadAware = true;
            this.m_remotingClient.PersistSettings = true;
            this.m_remotingClient.SettingsCategory = "RemotingClient";
            // 
            // m_errorLogger
            // 
            // 
            // 
            // 
            this.m_errorLogger.ErrorLog.FileName = "openFLEConsole.ErrorLog.txt";
            this.m_errorLogger.LogToUI = true;
            ((System.ComponentModel.ISupportInitialize)(this.m_clientHelper)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_remotingClient)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_errorLogger.ErrorLog)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.m_errorLogger)).EndInit();

        }

        #endregion

        private GSF.ServiceProcess.ClientHelper m_clientHelper;
        private GSF.Communication.TcpClient m_remotingClient;
        private GSF.ErrorManagement.ErrorLogger m_errorLogger;
    }
}
