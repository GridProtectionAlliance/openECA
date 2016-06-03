//******************************************************************************************************
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
//  06/01/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AlgorithmTemplate.Model;
using GSF.Threading;

namespace AlgorithmTemplate.Framework
{
    public partial class MainWindow : Form
    {
        #region [ Members ]

        // Constants for extern calls to various scrollbar functions
        private const int SB_VERT = 0x1;
        private const int WM_VSCROLL = 0x115;
        private const int SB_THUMBPOSITION = 4;

        // Fields
        private Concentrator m_concentrator;
        private Subscriber m_subscriber;
        private int m_selectionStart;
        private bool m_isClosed;

        #endregion

        #region [ Constructors ]

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region [ Methods ]

        private void UpdateStatus()
        {
            if (m_isClosed)
                return;

            Invoke(new Action(() =>
            {
                SetText(SubscriberStatusBox, m_subscriber.Status);
                SetText(ConcentratorStatusBox, m_concentrator.Status);
            }));

            new Action(UpdateStatus).DelayAndExecute(1000);
        }

        private void SetText(RichTextBox textBox, string text)
        {
            int VSmin;
            int VSmax;
            int savedVpos;

            // Get the position and range of the scroll bar
            savedVpos = GetScrollPos(textBox.Handle, SB_VERT);
            GetScrollRange(textBox.Handle, SB_VERT, out VSmin, out VSmax);

            // Get the current position of the user's selection in the text box
            int selectionStart = m_selectionStart;
            int selectionLength = textBox.SelectionLength;

            if (selectionStart != textBox.SelectionStart)
                selectionLength = -selectionLength;

            // Set the text of the text box
            textBox.Text = text;

            // Put the selection back where it was
            textBox.Select(selectionStart, selectionLength);
            m_selectionStart = selectionStart;

            // Put the scroll bar back where it was
            SetScrollPos(textBox.Handle, SB_VERT, savedVpos, true);
            PostMessageA(textBox.Handle, WM_VSCROLL, SB_THUMBPOSITION + 0x10000 * savedVpos, 0);
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            SignalLookup lookup = new SignalLookup();
            Mapper mapper = new Mapper(lookup);
            m_concentrator = new Concentrator(mapper);
            m_subscriber = new Subscriber(m_concentrator);
            m_concentrator.Start();
            m_subscriber.Start();
            new Action(UpdateStatus).DelayAndExecute(1000);
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_isClosed = true;
            m_subscriber.Stop();
            m_concentrator.Stop();
            m_concentrator.Dispose();
        }

        private void StatusBox_SelectionChanged(object sender, EventArgs e)
        {
            RichTextBox textBox = sender as RichTextBox;

            if ((object)textBox == null)
                return;

            if (textBox.SelectionLength > 0)
                return;

            m_selectionStart = textBox.SelectionStart;
        }

        #endregion

        #region [ Static ]

        // Static Methods

        [DllImport("user32.dll")]
        static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("user32.dll")]
        private static extern bool PostMessageA(IntPtr hWnd, int nBar, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);

        #endregion
    }
}
