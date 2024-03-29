﻿//******************************************************************************************************
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
//  05/18/2017 - Stephen A. Jenks
//      Added m_bufferTextBox to RichTextBoxWrapper to fix freezing issue after pausing
//
//******************************************************************************************************

using GSF;
using GSF.Threading;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ECAClientFramework
{
    public partial class MainWindow : Form
    {
        #region [ Members ]

        // Nested Types
        private class RichTextBoxWrapper
        {
            #region [ Members ]

            // Fields
            private readonly Form m_form;
            private readonly Label m_pauseLabel;
            private readonly RichTextBox m_visibleTextBox;
            private readonly RichTextBox m_bufferTextBox;
            private readonly Timer m_resumeTimer;
            private bool m_paused;
            private int m_updateCount;

            #endregion

            #region [ Constructors ]

            public RichTextBoxWrapper(Form form, Label pauseLabel, RichTextBox textBox)
            {
                m_form = form;
                m_pauseLabel = pauseLabel;
                m_visibleTextBox = textBox;
                m_bufferTextBox = new RichTextBox();
                m_bufferTextBox.Visible = false;

                m_resumeTimer = new Timer();
                m_resumeTimer.Interval = 200;
                m_resumeTimer.Tick += ResumeTimer_Tick;
            }

            #endregion

            #region [ Methods ]

            public void Update(Action<RichTextBox> action)
            {
                if (m_visibleTextBox.InvokeRequired)
                {
                    m_visibleTextBox.BeginInvoke(new Action<Action<RichTextBox>>(Update), action);
                    return;
                }

                CheckBounds(Cursor.Position);

                if (!m_paused)
                {
                    action(m_visibleTextBox);
                }
                else
                {
                    action(m_bufferTextBox);
                    m_updateCount++;
                    m_pauseLabel.Text = $"Updates paused while interacting! Queued updates: {m_updateCount}";
                }
            }

            private void CheckBounds(Point mousePosition)
            {
                if (!m_form.ContainsFocus || !m_visibleTextBox.Visible)
                {
                    Resume();
                    return;
                }

                Control parent = m_visibleTextBox.Parent;
                Point pos = parent.PointToClient(mousePosition);

                bool pause =
                    pos.X > 0 &&
                    pos.Y > 0 &&
                    pos.X < parent.Width &&
                    pos.Y < parent.Height;

                if (pause)
                    Pause();
                else
                    Resume();
            }

            private void Pause()
            {
                if (m_paused)
                    return;

                m_pauseLabel.Visible = true;
                m_visibleTextBox.BackColor = SystemColors.Control;
                m_bufferTextBox.Rtf = m_visibleTextBox.Rtf;
                m_updateCount = 0;
                m_paused = true;
                m_resumeTimer.Start();
            }

            private void Resume()
            {
                if (!m_paused)
                    return;

                m_visibleTextBox.Rtf = m_bufferTextBox.Rtf;

                m_resumeTimer.Stop();
                m_paused = false;
                m_visibleTextBox.BackColor = SystemColors.Window;
                m_pauseLabel.Text = "Updates paused while interacting!";
                m_pauseLabel.Visible = false;
            }

            private void ResumeTimer_Tick(object sender, EventArgs eventArgs)
            {
                CheckBounds(Cursor.Position);
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

        // ReSharper disable once NotAccessedField.Local
        private IMapper m_mapper; // Maintaining possible external reference
        private readonly Concentrator m_concentrator;
        private readonly Subscriber m_subscriber;

        private RichTextBoxWrapper m_algorithmMessageBoxWrapper;
        private RichTextBoxWrapper m_subscriberStatusBoxWrapper;
        private RichTextBoxWrapper m_subscriberMessageBoxWrapper;
        private RichTextBoxWrapper m_concentratorStatusBoxWrapper;
        private RichTextBoxWrapper m_concentratorMessageBoxWrapper;

        private bool m_isClosed;

        #endregion

        #region [ Constructors ]

        public MainWindow(Framework framework)
        {
            m_mapper = framework.Mapper;
            m_concentrator = framework.Concentrator;
            m_subscriber = framework.Subscriber;
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
            if (m_isClosed)
                return;

            // Get the position and range of the scroll bar
            int savedVpos = GetScrollPos(textBox.Handle, SB_VERT);
            GetScrollRange(textBox.Handle, SB_VERT, out int _, out int _);

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
            s_window = this;

            m_algorithmMessageBoxWrapper = new RichTextBoxWrapper(this, AlgorithmMessageLabel, AlgorithmMessageBox);
            m_subscriberStatusBoxWrapper = new RichTextBoxWrapper(this, SubscriberStatusLabel, SubscriberStatusBox);
            m_subscriberMessageBoxWrapper = new RichTextBoxWrapper(this, SubscriberMessageLabel, SubscriberMessageBox);
            m_concentratorStatusBoxWrapper = new RichTextBoxWrapper(this, ConcentratorStatusLabel, ConcentratorStatusBox);
            m_concentratorMessageBoxWrapper = new RichTextBoxWrapper(this, ConcentratorMessageLabel, ConcentratorMessageBox);

            m_concentrator.ProcessException += Concentrator_ProcessException;
            m_concentrator.FramesPerSecond = SystemSettings.FramesPerSecond;
            m_concentrator.LagTime = SystemSettings.LagTime;
            m_concentrator.LeadTime = SystemSettings.LeadTime;
            m_concentrator.RoundToNearestTimestamp = true;
            m_concentrator.Start();

            m_subscriber.StatusMessage += Subscriber_StatusMessage;
            m_subscriber.ProcessException += Subscriber_ProcessException;
            m_subscriber.Start();

            new Action(UpdateStatus).DelayAndExecute(1000);
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_isClosed = true;
            m_subscriber?.Stop();
            m_concentrator?.Stop();
            m_concentrator?.Dispose();
        }

        private void Concentrator_ProcessException(object sender, EventArgs<Exception> args)
        {
            m_concentratorMessageBoxWrapper.Update(textBox => AppendText(textBox, Color.Red, args.Argument.ToString()));
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
            Console.WriteLine(message);

            if ((object)s_window == null)
                return;

            s_window.m_algorithmMessageBoxWrapper.Update(textBox => s_window.AppendText(textBox, textBox.ForeColor, message));
        }

        public static void WriteWarning(string message)
        {
            Console.WriteLine($"WARNING: {message}");

            if ((object)s_window == null)
                return;

            s_window.m_algorithmMessageBoxWrapper.Update(textBox => s_window.AppendText(textBox, Color.Gold, message));
        }

        public static void WriteError(Exception ex)
        {
            Console.Error.WriteLine(ex.Message);

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
