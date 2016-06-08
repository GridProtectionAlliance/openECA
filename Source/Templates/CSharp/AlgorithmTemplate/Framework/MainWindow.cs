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
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AlgorithmTemplate.Model;
using GSF;
using GSF.Threading;

namespace AlgorithmTemplate.Framework
{
    public partial class MainWindow : Form
    {
        #region [ Members ]

        // Nested Types
        private class RichTextBoxWrapper
        {
            #region [ Members ]

            // Fields
            private RichTextBox m_textBox;
            private Queue<Action<RichTextBox>> m_updateQueue;
            bool m_queueUpdates;

            #endregion

            #region [ Constructors ]

            public RichTextBoxWrapper(RichTextBox textBox)
            {
                m_textBox = textBox;
                m_textBox.MouseEnter += TextBox_MouseEnter;
                m_textBox.MouseLeave += TextBox_MouseLeave;

                m_updateQueue = new Queue<Action<RichTextBox>>();
            }

            #endregion

            #region [ Methods ]

            public void Update(Action<RichTextBox> action)
            {
                if (m_textBox.InvokeRequired)
                {
                    m_textBox.BeginInvoke(new Action<Action<RichTextBox>>(Update), action);
                    return;
                }

                if (!m_queueUpdates)
                    action(m_textBox);
                else
                    m_updateQueue.Enqueue(action);
            }

            private void TextBox_MouseEnter(object sender, EventArgs eventArgs)
            {
                m_textBox.BackColor = SystemColors.Control;
                m_queueUpdates = true;
            }

            private void TextBox_MouseLeave(object sender, EventArgs eventArgs)
            {
                while (m_updateQueue.Count > 0)
                    m_updateQueue.Dequeue()(m_textBox);

                m_queueUpdates = false;
                m_textBox.BackColor = SystemColors.Window;
            }

            #endregion
        }

        // Constants
        private const int MaxTextBoxLength = 30000;
        
        // for extern calls to various scrollbar functions
        private const int SB_VERT = 0x1;
        private const int WM_VSCROLL = 0x115;
        private const int SB_THUMBPOSITION = 4;

        // Fields
        private Concentrator m_concentrator;
        private Subscriber m_subscriber;

        private RichTextBoxWrapper m_algorithmMessageBoxWrapper;
        private RichTextBoxWrapper m_subscriberStatusBoxWrapper;
        private RichTextBoxWrapper m_subscriberMessageBoxWrapper;
        private RichTextBoxWrapper m_concentratorStatusBoxWrapper;
        private RichTextBoxWrapper m_concentratorMessageBoxWrapper;

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
            string subscriberStatus;
            string concentratorStatus;

            if (m_isClosed)
                return;

            subscriberStatus = m_subscriber.Status;
            concentratorStatus = m_concentrator.Status;
            m_subscriberStatusBoxWrapper.Update(textBox => SetText(textBox, subscriberStatus));
            m_concentratorStatusBoxWrapper.Update(textBox => SetText(textBox, concentratorStatus));

            new Action(UpdateStatus).DelayAndExecute(1000);
        }

        private void SetText(RichTextBox textBox, string text)
        {
            int VSmin;
            int VSmax;
            int savedVpos;

            if (m_isClosed)
                return;

            // Get the position and range of the scroll bar
            savedVpos = GetScrollPos(textBox.Handle, SB_VERT);
            GetScrollRange(textBox.Handle, SB_VERT, out VSmin, out VSmax);

            // Set the text of the text box
            textBox.Text = text;

            // Put the scroll bar back where it was
            SetScrollPos(textBox.Handle, SB_VERT, savedVpos, true);
            PostMessageA(textBox.Handle, WM_VSCROLL, SB_THUMBPOSITION + 0x10000 * savedVpos, 0);
        }

        private void AppendText(RichTextBox textBox, Color textColor, string text)
        {
            if (m_isClosed)
                return;

            int totalLength = textBox.TextLength + text.Length;
            int overflow = totalLength - MaxTextBoxLength;

            if (overflow > 0)
            {
                int position = textBox.Text.IndexOf("\n", overflow, StringComparison.Ordinal);

                if (position > 0)
                {
                    textBox.Select(0, position + 1);
                    textBox.ReadOnly = false;
                    textBox.SelectedText = "";
                    textBox.ReadOnly = true;
                }
                else
                {
                    textBox.Clear();
                }
            }

            textBox.Select(textBox.TextLength, 0);
            textBox.SelectionColor = textColor;
            textBox.AppendText(text + "\n");
            textBox.ScrollToCaret();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            SignalLookup lookup = new SignalLookup();
            Mapper mapper = new Mapper(lookup);

            s_window = this;

            m_algorithmMessageBoxWrapper = new RichTextBoxWrapper(AlgorithmMessageBox);
            m_subscriberStatusBoxWrapper = new RichTextBoxWrapper(SubscriberStatusBox);
            m_subscriberMessageBoxWrapper = new RichTextBoxWrapper(SubscriberMessageBox);
            m_concentratorStatusBoxWrapper = new RichTextBoxWrapper(ConcentratorStatusBox);
            m_concentratorMessageBoxWrapper = new RichTextBoxWrapper(ConcentratorMessageBox);

            m_concentrator = new Concentrator(mapper);
            m_concentrator.ProcessException += Concentrator_ProcessException;
            m_concentrator.FramesPerSecond = 30;
            m_concentrator.LagTime = 3;
            m_concentrator.LeadTime = 1;
            m_concentrator.Start();

            m_subscriber = new Subscriber(m_concentrator);
            m_subscriber.StatusMessage += Subscriber_StatusMessage;
            m_subscriber.ProcessException += Subscriber_ProcessException;
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

        private void Concentrator_ProcessException(object sender, EventArgs<Exception> args)
        {
            m_concentratorMessageBoxWrapper.Update(textBox => AppendText(textBox, Color.Red, args.Argument.Message));
        }

        private void Subscriber_StatusMessage(object sender, EventArgs<string> args)
        {
            m_subscriberMessageBoxWrapper.Update(textBox => AppendText(textBox, textBox.ForeColor, args.Argument));
        }

        private void Subscriber_ProcessException(object sender, EventArgs<Exception> args)
        {
            m_subscriberMessageBoxWrapper.Update(textBox => AppendText(textBox, Color.Red, args.Argument.Message));
        }

        #endregion

        #region [ Static ]

        // Static Fields
        private static MainWindow s_window;

        // Static Methods
        public static void WriteMessage(string message)
        {
            if ((object)s_window == null)
                return;

            s_window.m_algorithmMessageBoxWrapper.Update(textBox => s_window.AppendText(textBox, textBox.ForeColor, message));
        }

        public static void WriteWarning(string message)
        {
            if ((object)s_window == null)
                return;

            s_window.m_algorithmMessageBoxWrapper.Update(textBox => s_window.AppendText(textBox, Color.Gold, message));
        }

        public static void WriteError(Exception ex)
        {
            if ((object)s_window == null)
                return;

            s_window.m_algorithmMessageBoxWrapper.Update(textBox => s_window.AppendText(textBox, Color.Red, ex.Message));
        }

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
