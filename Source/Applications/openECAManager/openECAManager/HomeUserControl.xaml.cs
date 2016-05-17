﻿//******************************************************************************************************
//  HomeUserControl.xaml.cs - Gbtc
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
//  11/09/2011 - Mehulbhai P Thakkar
//       Generated original version of source code.
// 30/07/2012 - Aniket Salver
//       Remembers the last graph selection. 
//******************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using GSF;
using GSF.Communication;
using GSF.Data;
using GSF.Identity;
using GSF.IO;
using GSF.PhasorProtocols.UI.DataModels;
using GSF.Reflection;
using GSF.ServiceProcess;
using GSF.TimeSeries;
using GSF.TimeSeries.Transport;
using GSF.TimeSeries.UI;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Measurement = GSF.TimeSeries.UI.DataModels.Measurement;

#pragma warning disable 612,618

namespace openECAManager
{
    /// <summary>
    /// Interaction logic for HomeUserControl.xaml
    /// </summary>
    public partial class HomeUserControl
    {
        #region [ Members ]

        // Constants
        private const double RefreshInterval = 0.25;
        private const int NumberOfPointsToPlot = 60;

        // Fields
        private readonly ObservableCollection<MenuDataItem> m_menuDataItems;
        private WindowsServiceClient m_windowsServiceClient;
        private DispatcherTimer m_refreshTimer;

        // Subscription fields
        private DataSubscriber m_unsynchronizedSubscriber;
        private bool m_subscribedUnsynchronized;
        private string m_signalID;
        private int m_processingUnsynchronizedMeasurements;
        private int[] m_xAxisDataCollection;                                // Source data for the binding collection.
        private EnumerableDataSource<int> m_xAxisBindingCollection;         // Values plotted on X-Axis.        
        private ConcurrentQueue<double> m_yAxisDataCollection;              // Source data for the binding collection. Format is <signalID, collection of values from subscription API>.
        private EnumerableDataSource<double> m_yAxisBindingCollection;      // Values plotted on Y-Axis.
        private LineGraph m_lineGraph;
        private bool m_eventHandlerRegistered;
        private Measurement m_selectedMeasurement;
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates an instance of <see cref="HomeUserControl"/>.
        /// </summary>
        public HomeUserControl()
        {
            InitializeComponent();
            this.Loaded += HomeUserControl_Loaded;
            this.Unloaded += HomeUserControl_Unloaded;

            // Load Menu
            XmlRootAttribute xmlRootAttribute = new XmlRootAttribute("MenuDataItems");
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<MenuDataItem>), xmlRootAttribute);

            using (XmlReader reader = XmlReader.Create(FilePath.GetAbsolutePath("Menu.xml")))
            {
                m_menuDataItems = (ObservableCollection<MenuDataItem>)serializer.Deserialize(reader);
            }
        }

        #endregion

        #region [ Methods ]

        private void HomeUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_windowsServiceClient != null && m_windowsServiceClient.Helper != null)
                {
                    m_windowsServiceClient.Helper.ReceivedServiceResponse -= Helper_ReceivedServiceResponse;
                }

                if (m_refreshTimer != null)
                    m_refreshTimer.Stop();

                UnsubscribeUnsynchronizedData();
            }
            finally
            {
                m_refreshTimer = null;
                m_unsynchronizedSubscriber = null;
            }
        }

        private void HomeUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!CommonFunctions.CurrentPrincipal.IsInRole("Administrator,Editor"))
                ButtonInputWizard.IsEnabled = false;

            m_windowsServiceClient = CommonFunctions.GetWindowsServiceClient();

            if (m_windowsServiceClient == null || m_windowsServiceClient.Helper.RemotingClient.CurrentState != ClientState.Connected)
            {
                // TODO: Disable any buttons here that will not function without service connectivity
            }
            else
            {
                m_windowsServiceClient.Helper.ReceivedServiceResponse += Helper_ReceivedServiceResponse;
                CommonFunctions.SendCommandToService("Health -actionable");
                CommonFunctions.SendCommandToService("Version -actionable");
                CommonFunctions.SendCommandToService("Status -actionable");
                CommonFunctions.SendCommandToService("Time -actionable");
                m_eventHandlerRegistered = true;
            }

            m_refreshTimer = new DispatcherTimer();
            m_refreshTimer.Interval = TimeSpan.FromSeconds(5);
            m_refreshTimer.Tick += RefreshTimer_Tick;
            m_refreshTimer.Start();

            if (IntPtr.Size == 8)
                TextBlockInstance.Text = "64-bit";
            else
                TextBlockInstance.Text = "32-bit";

            TextBlockLocalTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Version appVersion = AssemblyInfo.EntryAssembly.Version;
            TextBlockManagerVersion.Text = appVersion.Major + "." + appVersion.Minor + "." + appVersion.Build + ".0";

            try
            {
                using (AdoDataConnection database = new AdoDataConnection(CommonFunctions.DefaultSettingsCategory))
                {
                    TextBlockDatabaseType.Text = database.DatabaseType.ToString();

                    try
                    {
                        if (database.IsSqlite || database.IsJetEngine)
                        {
                            // Extract database file name from connection string for file centric databases
                            TextBlockDatabaseName.Text = FilePath.GetFileName(database.Connection.ConnectionString.ParseKeyValuePairs()["Data Source"]);
                        }
                        else if (database.IsOracle)
                        {
                            // Extract user name from connection string for Oracle databases
                            TextBlockDatabaseName.Text = database.Connection.ConnectionString.ParseKeyValuePairs()["User Id"];
                        }
                        else
                        {
                            TextBlockDatabaseName.Text = database.Connection.Database;
                        }
                    }
                    catch
                    {
                        // Fall back on database name if file anything fails
                        TextBlockDatabaseName.Text = database.Connection.Database;
                    }
                }
            }
            catch
            {
                TextBlockDatabaseName.Text = "Not Avaliable";
            }

            try
            {
                using (UserInfo info = new UserInfo(CommonFunctions.CurrentUser))
                {
                    if (info.Exists)
                        TextBlockUser.Text = info.LoginID;
                    else
                        TextBlockUser.Text = CommonFunctions.CurrentUser;
                }
            }
            catch
            {
                TextBlockUser.Text = CommonFunctions.CurrentUser;
            }

            ((HorizontalAxis)ChartPlotterDynamic.MainHorizontalAxis).LabelProvider.LabelStringFormat = "";

            //Remove legend on the right.
            Panel legendParent = (Panel)ChartPlotterDynamic.Legend.ContentGrid.Parent;
            if (legendParent != null)
                legendParent.Children.Remove(ChartPlotterDynamic.Legend.ContentGrid);

            ChartPlotterDynamic.NewLegendVisible = false;

            m_xAxisDataCollection = new int[NumberOfPointsToPlot];
            for (int i = 0; i < NumberOfPointsToPlot; i++)
                m_xAxisDataCollection[i] = i;
            m_xAxisBindingCollection = new EnumerableDataSource<int>(m_xAxisDataCollection);
            m_xAxisBindingCollection.SetXMapping(x => x);

            LoadComboBoxDeviceAsync();
        }

        private void LoadComboBoxDeviceAsync()
        {
            Thread t = new Thread(() =>
            {
                Dictionary<int, string> deviceLookupList = Device.GetLookupList(null);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ComboBoxDevice.ItemsSource = deviceLookupList;

                    if (Application.Current.Resources.Contains("SelectedDevice_Home"))
                        ComboBoxDevice.SelectedIndex = (int)Application.Current.Resources["SelectedDevice_Home"];
                    else
                        ComboBoxDevice.SelectedIndex = 0;
                }));
            });

            t.IsBackground = true;
            t.Start();
        }

        private void LoadComboBoxMeasurementAsync()
        {
            int selectedDeviceID = ((KeyValuePair<int, string>)ComboBoxDevice.SelectedItem).Key;

            Thread t = new Thread(() =>
            {
                ObservableCollection<Measurement> measurements = Measurement.Load(null, selectedDeviceID);
                ObservableCollection<Measurement> itemsSource = new ObservableCollection<Measurement>(measurements.Where(m => m.SignalSuffix == "PA" || m.SignalSuffix == "FQ"));

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ComboBoxMeasurement.ItemsSource = itemsSource;

                    if (Application.Current.Resources.Contains("SelectedMeasurement_Home") && (int)Application.Current.Resources["SelectedMeasurement_Home"] != -1)
                        ComboBoxMeasurement.SelectedIndex = (int)Application.Current.Resources["SelectedMeasurement_Home"];
                    else
                        ComboBoxMeasurement.SelectedIndex = 0;
                }));
            });

            t.IsBackground = true;
            t.Start();
        }

        void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (m_windowsServiceClient != null && m_windowsServiceClient.Helper != null &&
                m_windowsServiceClient.Helper.RemotingClient != null && m_windowsServiceClient.Helper.RemotingClient.CurrentState == ClientState.Connected)
            {
                try
                {
                    ButtonInputWizard.IsEnabled = CommonFunctions.CurrentPrincipal.IsInRole("Administrator,Editor");

                    if (!m_eventHandlerRegistered)
                    {
                        m_windowsServiceClient.Helper.ReceivedServiceResponse += Helper_ReceivedServiceResponse;
                        CommonFunctions.SendCommandToService("Version -actionable");
                        m_eventHandlerRegistered = true;
                    }

                    CommonFunctions.SendCommandToService("Health -actionable");
                    CommonFunctions.SendCommandToService("Time -actionable");

                    if (PopupStatus.IsOpen)
                        CommonFunctions.SendCommandToService("Status -actionable");
                }
                catch
                {
                }
            }
            //else
            //{
            //    // TODO: Disable any buttons here that will not function without service connectivity
            //}

            TextBlockLocalTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private void ButtonQuickLink_Click(object sender, RoutedEventArgs e)
        {
            MenuDataItem item = new MenuDataItem();
            string stringToMatch = ((Button)sender).Tag.ToString();

            if (!string.IsNullOrEmpty(stringToMatch))
            {
                if (stringToMatch == "Restart")
                {
                    RestartService();
                }
                else
                {
                    GetMenuDataItem(m_menuDataItems, stringToMatch, ref item);

                    if ((object)item.MenuText != null)
                        item.Command.Execute(null);
                }
            }
        }

        /// <summary>
        /// Recursively finds menu item to navigate to when a button is clicked on the UI.
        /// </summary>
        /// <param name="items">Collection of menu items.</param>
        /// <param name="stringToMatch">Item to search for in menu items collection.</param>
        /// <param name="item">Returns a menu item.</param>
        private void GetMenuDataItem(ObservableCollection<MenuDataItem> items, string stringToMatch, ref MenuDataItem item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].UserControlPath.ToLower() == stringToMatch.ToLower())
                {
                    item = items[i];
                    break;
                }

                if (items[i].SubMenuItems.Count > 0)
                    GetMenuDataItem(items[i].SubMenuItems, stringToMatch, ref item);
            }
        }

        private void RestartService()
        {
            try
            {
                if (MessageBox.Show("Do you want to restart service?", "Restart Service", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    CommonFunctions.SendCommandToService("Restart");
                    MessageBox.Show("Successfully sent RESTART command to the service.", "Restart Service", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("Failed sent RESTART command to the service." + Environment.NewLine + "Service is either offline or disconnected.", "Restart Service", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles ReceivedServiceResponse event.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void Helper_ReceivedServiceResponse(object sender, EventArgs<ServiceResponse> e)
        {
            string sourceCommand;
            bool responseSuccess;

            if (ClientHelper.TryParseActionableResponse(e.Argument, out sourceCommand, out responseSuccess))
            {
                if (sourceCommand.ToLower() == "health")
                {
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        TextBlockSystemHealth.Text = e.Argument.Message.TrimEnd();
                        GroupBoxSystemHealth.Header = "System Health (Last Refreshed: " + DateTime.Now.ToString("HH:mm:ss.fff") + ")";
                    });
                }
                else if (sourceCommand.ToLower() == "status")
                {
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        GroupBoxStatus.Header = "System Status (Last Refreshed: " + DateTime.Now.ToString("HH:mm:ss.fff") + ")";
                        TextBlockStatus.Text = e.Argument.Message.TrimEnd();
                    });
                }
                else if (sourceCommand.ToLower() == "version")
                {
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        TextBlockVersion.Text = e.Argument.Message.Substring(e.Argument.Message.ToLower().LastIndexOf("version:", StringComparison.Ordinal) + 8).Trim();
                    });
                }
                else if (sourceCommand.ToLower() == "time")
                {
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        string[] times = Regex.Split(e.Argument.Message, "\r\n");

                        if (times.Any())
                        {
                            string[] currentTimes = Regex.Split(times[0], ",");

                            if (currentTimes.Any())
                                TextBlockServerTime.Text = currentTimes[0].Substring(currentTimes[0].ToLower().LastIndexOf("system time:", StringComparison.Ordinal) + 12).Trim();
                        }
                    });
                }
            }
        }

        private void ButtonStatus_Click(object sender, RoutedEventArgs e)
        {
            PopupStatus.IsOpen = true;
            if (m_windowsServiceClient != null && m_windowsServiceClient.Helper != null &&
                   m_windowsServiceClient.Helper.RemotingClient != null && m_windowsServiceClient.Helper.RemotingClient.CurrentState == ClientState.Connected)
                CommonFunctions.SendCommandToService("Status -actionable");
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            PopupStatus.IsOpen = false;
        }

        private void ComboBoxMeasurement_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxMeasurement.Items.Count > 0)
            {
                m_selectedMeasurement = (Measurement)ComboBoxMeasurement.SelectedItem;

                // Capture's the Selected index of measurement Combo Box
                if (Application.Current.Resources.Contains("SelectedMeasurement_Home"))
                {
                    Application.Current.Resources.Remove("SelectedMeasurement_Home");
                    Application.Current.Resources.Add("SelectedMeasurement_Home", ComboBoxMeasurement.SelectedIndex);
                }
                else
                    Application.Current.Resources.Add("SelectedMeasurement_Home", ComboBoxMeasurement.SelectedIndex);

                if (m_selectedMeasurement != null)
                {
                    m_signalID = m_selectedMeasurement.SignalID.ToString();

                    if (m_selectedMeasurement.SignalSuffix == "PA")
                        ChartPlotterDynamic.Visible = DataRect.Create(0, -180, NumberOfPointsToPlot, 180);
                    else if (m_selectedMeasurement.SignalSuffix == "FQ")
                        ChartPlotterDynamic.Visible = DataRect.Create(0, Convert.ToDouble(IsolatedStorageManager.ReadFromIsolatedStorage("FrequencyRangeMin")), NumberOfPointsToPlot, Convert.ToDouble(IsolatedStorageManager.ReadFromIsolatedStorage("FrequencyRangeMax")));
                }
            }
            else
                m_signalID = string.Empty;

            SubscribeUnsynchronizedData();
        }

        private void ComboBoxDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Capture's the Selected index of Device Combo Box
            if (Application.Current.Resources.Contains("SelectedDevice_Home"))
            {
                Application.Current.Resources.Remove("SelectedDevice_Home");
                Application.Current.Resources.Add("SelectedDevice_Home", ComboBoxDevice.SelectedIndex);
            }
            else
            {
                Application.Current.Resources.Add("SelectedDevice_Home", ComboBoxDevice.SelectedIndex);
            }

            LoadComboBoxMeasurementAsync();
        }

        #region [ Unsynchronized Subscription ]

        private void m_unsynchronizedSubscriber_ConnectionTerminated(object sender, EventArgs e)
        {
            m_subscribedUnsynchronized = false;
            UnsubscribeUnsynchronizedData();

            try
            {
                ChartPlotterDynamic.Dispatcher.BeginInvoke(new Action(() => ChartPlotterDynamic.Children.Remove(m_lineGraph)));
            }
            catch
            {
            }

            InitializeUnsynchronizedSubscription();
        }

        private void m_unsynchronizedSubscriber_NewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            if (0 == Interlocked.Exchange(ref m_processingUnsynchronizedMeasurements, 1))
            {
                try
                {
                    foreach (IMeasurement measurement in e.Argument)
                    {
                        double tempValue = measurement.AdjustedValue;

                        if (!double.IsNaN(tempValue) && !double.IsInfinity(tempValue)) // Process data only if it is not NaN or infinity.
                        {
                            ChartPlotterDynamic.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate
                            {
                                if (m_yAxisDataCollection.Count == 0)
                                {
                                    for (int i = 0; i < NumberOfPointsToPlot; i++)
                                        m_yAxisDataCollection.Enqueue(tempValue);

                                    m_yAxisBindingCollection = new EnumerableDataSource<double>(m_yAxisDataCollection);
                                    m_yAxisBindingCollection.SetYMapping(y => y);

                                    m_lineGraph = ChartPlotterDynamic.AddLineGraph(new CompositeDataSource(m_xAxisBindingCollection, m_yAxisBindingCollection), Color.FromArgb(255, 25, 25, 200), 1, "");

                                }
                                else
                                {
                                    double oldValue;

                                    if (m_yAxisDataCollection.TryDequeue(out oldValue))
                                        m_yAxisDataCollection.Enqueue(tempValue);
                                }
                                m_yAxisBindingCollection.RaiseDataChanged();
                            });
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref m_processingUnsynchronizedMeasurements, 0);
                }
            }
        }

        private void m_unsynchronizedSubscriber_ConnectionEstablished(object sender, EventArgs e)
        {
            m_subscribedUnsynchronized = true;
            SubscribeUnsynchronizedData();
        }

        private void m_unsynchronizedSubscriber_ProcessException(object sender, EventArgs<Exception> e)
        {

        }

        private void m_unsynchronizedSubscriber_StatusMessage(object sender, EventArgs<string> e)
        {

        }

        private void InitializeUnsynchronizedSubscription()
        {
            try
            {
                using (AdoDataConnection database = new AdoDataConnection(CommonFunctions.DefaultSettingsCategory))
                {
                    m_unsynchronizedSubscriber = new DataSubscriber();
                    m_unsynchronizedSubscriber.StatusMessage += m_unsynchronizedSubscriber_StatusMessage;
                    m_unsynchronizedSubscriber.ProcessException += m_unsynchronizedSubscriber_ProcessException;
                    m_unsynchronizedSubscriber.ConnectionEstablished += m_unsynchronizedSubscriber_ConnectionEstablished;
                    m_unsynchronizedSubscriber.NewMeasurements += m_unsynchronizedSubscriber_NewMeasurements;
                    m_unsynchronizedSubscriber.ConnectionTerminated += m_unsynchronizedSubscriber_ConnectionTerminated;
                    m_unsynchronizedSubscriber.ConnectionString = database.DataPublisherConnectionString();
                    m_unsynchronizedSubscriber.Initialize();
                    m_unsynchronizedSubscriber.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize subscription." + Environment.NewLine + ex.Message, "Failed to Subscribe", MessageBoxButton.OK);
            }
        }

        private void StopUnsynchronizedSubscription()
        {
            if (m_unsynchronizedSubscriber != null)
            {
                m_unsynchronizedSubscriber.StatusMessage -= m_unsynchronizedSubscriber_StatusMessage;
                m_unsynchronizedSubscriber.ProcessException -= m_unsynchronizedSubscriber_ProcessException;
                m_unsynchronizedSubscriber.ConnectionEstablished -= m_unsynchronizedSubscriber_ConnectionEstablished;
                m_unsynchronizedSubscriber.NewMeasurements -= m_unsynchronizedSubscriber_NewMeasurements;
                m_unsynchronizedSubscriber.ConnectionTerminated -= m_unsynchronizedSubscriber_ConnectionTerminated;
                m_unsynchronizedSubscriber.Stop();
                m_unsynchronizedSubscriber.Dispose();
                m_unsynchronizedSubscriber = null;
            }
        }

        private void SubscribeUnsynchronizedData()
        {
            if (m_unsynchronizedSubscriber == null)
                InitializeUnsynchronizedSubscription();

            try
            {
                ChartPlotterDynamic.Dispatcher.BeginInvoke(new Action(() => ChartPlotterDynamic.Children.Remove(m_lineGraph)));
            }
            catch
            {
            }

            m_yAxisDataCollection = new ConcurrentQueue<double>();

            if (m_subscribedUnsynchronized && (object)m_unsynchronizedSubscriber != null && !string.IsNullOrEmpty(m_signalID))
                m_unsynchronizedSubscriber.UnsynchronizedSubscribe(true, true, m_signalID, null, true, RefreshInterval);
        }

        /// <summary>
        /// Unsubscribes data from the service.
        /// </summary>
        public void UnsubscribeUnsynchronizedData()
        {
            try
            {
                if (m_unsynchronizedSubscriber != null)
                {
                    m_unsynchronizedSubscriber.Unsubscribe();
                    StopUnsynchronizedSubscription();
                }
            }
            catch
            {
                m_unsynchronizedSubscriber = null;
            }
        }

        #endregion

        #endregion
    }
}
