﻿//******************************************************************************************************
//  AccessDatabaseSetupScreen.xaml.cs - Gbtc
//
//  Copyright © 2011, Grid Protection Alliance.  All Rights Reserved.
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
//  09/09/2010 - Stephen C. Wills
//      Generated original version of source code.
//  09/19/2010 - J. Ritchie Carroll
//      Made default path for Access database point to a non-restrictive location.
//      Added user verification for override of existing Access configuration.
//  01/21/2011 - J. Ritchie Carroll
//       Modified next page to be admin user account credentials setup.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TVA;
using TVA.Data;
using TVA.IO;

namespace ConfigurationSetupUtility.Screens
{
    /// <summary>
    /// Interaction logic for AccessDatabaseSetupScreen.xaml
    /// </summary>
    public partial class AccessDatabaseSetupScreen : UserControl, IScreen
    {
        #region [ Members ]

        // Fields
        private Dictionary<string, object> m_state;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="AccessDatabaseSetupScreen"/> class.
        /// </summary>
        public AccessDatabaseSetupScreen()
        {
            InitializeComponent();
            m_accessDatabaseFilePathTextBox.Text = App.BaseAccessConfig;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the screen to be displayed when the user clicks the "Next" button.
        /// </summary>
        public IScreen NextScreen
        {
            get
            {
                IScreen nextScreen;
                bool securityUpgrade = false;

                if (m_state.ContainsKey("securityUpgrade"))
                    securityUpgrade = Convert.ToBoolean(m_state["securityUpgrade"]);

                if (Convert.ToBoolean(m_state["existing"]) && !securityUpgrade)
                {
                    if (!m_state.ContainsKey("applyChangesScreen"))
                        m_state.Add("applyChangesScreen", new ApplyConfigurationChangesScreen());

                    nextScreen = m_state["applyChangesScreen"] as IScreen;
                }
                else
                {
                    if (!m_state.ContainsKey("userAccountSetupScreen"))
                        m_state.Add("userAccountSetupScreen", new UserAccountCredentialsSetupScreen());

                    nextScreen = m_state["userAccountSetupScreen"] as IScreen;
                }

                return nextScreen;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the user can advance to
        /// the next screen from the current screen.
        /// </summary>
        public bool CanGoForward
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the user can return to
        /// the previous screen from the current screen.
        /// </summary>
        public bool CanGoBack
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the user can cancel the
        /// setup process from the current screen.
        /// </summary>
        public bool CanCancel
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the user input is valid on the current page.
        /// </summary>
        public bool UserInputIsValid
        {
            get
            {
                if (!string.IsNullOrEmpty(m_accessDatabaseFilePathTextBox.Text))
                {
                    if (!Convert.ToBoolean(m_state["existing"]) && File.Exists(m_accessDatabaseFilePathTextBox.Text))
                        return (MessageBox.Show("An Access database already exists at the selected location. Are you sure you want to override the existing configuration?", "Configuration Already Exists", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes);

                    bool existing = Convert.ToBoolean(m_state["existing"]);
                    bool migrate = existing && Convert.ToBoolean(m_state["updateConfiguration"]);

                    if (existing && !migrate)
                    {
                        //check if UserAccount table has any rows.
                        IDbConnection connection = null;
                        try
                        {
                            string destination = m_state["accessDatabaseFilePath"].ToString();
                            string connectionString = "Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" + destination;
                            string dataProviderString = "AssemblyName={System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089}; ConnectionType=System.Data.OleDb.OleDbConnection; AdapterType=System.Data.OleDb.OleDbDataAdapter";

                            Dictionary<string, string> settings = connectionString.ParseKeyValuePairs();
                            Dictionary<string, string> dataProviderSettings = dataProviderString.ParseKeyValuePairs();
                            string assemblyName = dataProviderSettings["AssemblyName"];
                            string connectionTypeName = dataProviderSettings["ConnectionType"];
                            string connectionSetting;

                            Assembly assembly = Assembly.Load(new AssemblyName(assemblyName));
                            Type connectionType = assembly.GetType(connectionTypeName);

                            if (settings.TryGetValue("Data Source", out connectionSetting))
                            {
                                settings["Data Source"] = FilePath.GetAbsolutePath(connectionSetting);
                                connectionString = settings.JoinKeyValuePairs();
                            }

                            connection = (IDbConnection)Activator.CreateInstance(connectionType);
                            connection.ConnectionString = connectionString;
                            connection.Open();

                            if ((int)connection.ExecuteScalar("SELECT COUNT(*) FROM UserAccount") > 0)
                                m_state["securityUpgrade"] = false;
                            else
                                m_state["securityUpgrade"] = true;

                        }
                        catch (Exception ex)
                        {
                            string failMessage = "Database connection issue. " + ex.Message;
                            MessageBox.Show(failMessage);
                            m_accessDatabaseFilePathTextBox.Focus();
                            return false;
                        }
                        finally
                        {
                            if (connection != null)
                                connection.Dispose();
                        }
                    }

                    return true;
                }
                else
                {
                    MessageBox.Show("Please enter a location for the Access database file.");
                    m_accessDatabaseFilePathTextBox.Focus();
                    return false;
                }
            }
        }

        /// <summary>
        /// Collection shared among screens that represents the state of the setup.
        /// </summary>
        public Dictionary<string, object> State
        {
            get
            {
                return m_state;
            }
            set
            {
                m_state = value;
                InitializeState();
            }
        }

        /// <summary>
        /// Allows the screen to update the navigation buttons after a change is made
        /// that would affect the user's ability to navigate to other screens.
        /// </summary>
        public Action UpdateNavigation { get; set; }

        #endregion

        #region [ Methods ]

        // Initializes the state keys to their default values.
        private void InitializeState()
        {
            bool existing = Convert.ToBoolean(m_state["existing"]);
            bool migrate = existing && Convert.ToBoolean(m_state["updateConfiguration"]);
            string newDatabaseMessage = "Please select the location in which to save the new database file.";
            string oldDatabaseMessage = "Please select the location of your existing database file.";

            m_accessDatabaseInstructionTextBlock.Text = (!existing || migrate) ? newDatabaseMessage : oldDatabaseMessage;

            try
            {
                // Set a default path for Access database that will allow non-restrictive read/write access
                string accessDatabaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "openPG\\");

                // Make sure path exists

                if (!Directory.Exists(accessDatabaseFilePath))
                    Directory.CreateDirectory(accessDatabaseFilePath);

                m_accessDatabaseFilePathTextBox.Text = Path.Combine(accessDatabaseFilePath, migrate ? App.AccessConfigv2 : App.BaseAccessConfig);
            }
            catch
            {
                m_accessDatabaseFilePathTextBox.Text = migrate ? App.AccessConfigv2 : App.BaseAccessConfig;
            }

            if (!m_state.ContainsKey("accessDatabaseFilePath"))
                m_state.Add("accessDatabaseFilePath", m_accessDatabaseFilePathTextBox.Text);
        }

        // Occurs when the user changes the path name of the Access database file.
        private void AccessDatabaseFilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_state != null)
                m_state["accessDatabaseFilePath"] = m_accessDatabaseFilePathTextBox.Text;
        }

        // Occurs when the user clicks the "Browse..." button.
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            FileDialog browseDialog;
            bool existing = Convert.ToBoolean(m_state["existing"]);
            bool migrate = existing && Convert.ToBoolean(m_state["updateConfiguration"]);

            if (existing && !migrate)
            {
                browseDialog = new OpenFileDialog();
                browseDialog.CheckFileExists = true;
            }
            else
            {
                browseDialog = new SaveFileDialog();
                browseDialog.AddExtension = true;
                browseDialog.CheckPathExists = true;
                browseDialog.DefaultExt = "mdb";
            }

            browseDialog.Filter = "MDB Files (*.mdb)|*.mdb|All Files|*.*";

            if (browseDialog.ShowDialog() == true)
                m_accessDatabaseFilePathTextBox.Text = browseDialog.FileName;
        }

        #endregion
    }
}
