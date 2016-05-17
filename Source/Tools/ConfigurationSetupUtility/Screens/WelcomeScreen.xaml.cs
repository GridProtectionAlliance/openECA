﻿//******************************************************************************************************
//  WelcomePage.xaml.cs - Gbtc
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
//  09/07/2010 - Stephen C. Wills
//       Generated original version of source code.
//  09/19/2010 - J. Ritchie Carroll
//       Added code to cache 64-bit installation state when passed as a command line argument
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace ConfigurationSetupUtility.Screens
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomeScreen : UserControl, IScreen
    {

        #region [ Members ]

        // Fields

        private IScreen m_nextPage;
        private Dictionary<string, object> m_state;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="WelcomeScreen"/> class.
        /// </summary>
        public WelcomeScreen()
        {
            InitializeComponent();
            m_nextPage = new ExistingConfigurationScreen();
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
                return m_nextPage;
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
                return false;
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
                return true;
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
                InitializeWelcomeMessage();
            }
        }

        /// <summary>
        /// Allows the screen to update the navigation buttons after a change is made
        /// that would affect the user's ability to navigate to other screens.
        /// </summary>
        public Action UpdateNavigation { get; set; }

        #endregion

        #region [ Methods ]

        // Initializes the welcome message based on the existence of the -install flag.
        private void InitializeWelcomeMessage()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool installFlag = args.Contains("-install", StringComparer.CurrentCultureIgnoreCase);

            if (m_state != null)
                m_state["64bit"] = args.Contains("-64bit", StringComparer.CurrentCultureIgnoreCase);
    
            if (installFlag)
                m_welcomeMessageTextBlock.Text = "You now need to set up the openECA configuration.\r\n";
            else
                m_welcomeMessageTextBlock.Text = "";

            
            m_welcomeMessageTextBlock.Text += "\r\nThis wizard will walk you through the needed steps so you can easily set up your system configuration.";

            // The historian setup screen takes time to load because of DLL scanning, so we cache it at startup
            if (m_state != null)
                m_state["historianSetupScreen"] = new HistorianSetupScreen();
        }

        #endregion

    }
}
