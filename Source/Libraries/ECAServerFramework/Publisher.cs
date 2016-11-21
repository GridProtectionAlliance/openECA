//******************************************************************************************************
//  Publisher.cs - Gbtc
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
//  11/17/2016 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using ECACommonUtilities.Model;
using GSF;
using GSF.Configuration;
using GSF.Data;
using GSF.Threading;
using GSF.TimeSeries;
using GSF.TimeSeries.Transport;

namespace ECAServerFramework
{
    public enum ECAServerCommand : byte
    {
        MetaSignal = ServerCommand.UserCommand00,
        DataPacket = ServerCommand.UserCommand01,
        StatusMessage = ServerCommand.UserCommand02
    }

    public class Publisher : DataPublisher
    {
        #region [ Members ]

        // Constants
        private const int ConfigurationChangedTimeout = 1000;

        // Fields
        private ICancellationToken m_configurationChangedToken;

        #endregion

        #region [ Methods ]

        protected override void HandleUserCommand(ClientConnection connection, ServerCommand command, byte[] buffer, int startIndex, int length)
        {
            base.HandleUserCommand(connection, command, buffer, startIndex, length);

            ECAServerCommand ecaCommand = (ECAServerCommand)command;

            switch (ecaCommand)
            {
                case ECAServerCommand.MetaSignal:
                    HandleMetaSignalCommand(connection, buffer, startIndex, length);
                    break;

                case ECAServerCommand.DataPacket:
                    HandleDataPacketCommand(connection, buffer, startIndex, length);
                    break;

                case ECAServerCommand.StatusMessage:
                    HandleStatusMessageCommand(connection, buffer, startIndex, length);
                    break;

                default:
                    OnProcessException(new InvalidOperationException($"Received unrecognized user command: {command}"));
                    break;
            }
        }

        private void HandleMetaSignalCommand(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            MakeConfigurationChanges(() =>
            {
                try
                {
                    if (startIndex + sizeof(int) > Math.Min(length, buffer.Length))
                        throw new InvalidOperationException("No message payload.");

                    int index = startIndex;
                    int payloadByteLength = BigEndian.ToInt32(buffer, index);
                    index += sizeof(int);

                    if (payloadByteLength < 0 || index + payloadByteLength > Math.Min(length, buffer.Length))
                        throw new InvalidOperationException($"Payload byte length out of bounds ({payloadByteLength}).");

                    string parameterString = connection.Encoding.GetString(buffer, index, payloadByteLength);
                    MetaSignal metaSignal = new MetaSignal();
                    new ConnectionStringParser<SettingAttribute>().ParseConnectionString(parameterString, metaSignal);

                    string deviceAcronym = Regex.Replace($"{metaSignal.AnalyticProjectName}!{metaSignal.AnalyticInstanceName}".ToUpper(), @"[^A-Z0-9\-!_\.@#\$]", "");
                    string deviceName = $"{metaSignal.AnalyticProjectName} {metaSignal.AnalyticInstanceName}";
                    using (AdoDataConnection dbConnection = new AdoDataConnection("systemSettings"))
                    {
                        if (dbConnection.ExecuteScalar<int>("SELECT COUNT(*) FROM Device WHERE UniqueID = {0}", metaSignal.DeviceID) > 0)
                            dbConnection.ExecuteNonQuery("UPDATE Device SET Acronym = {0} WHERE UniqueID = {1}", deviceAcronym, metaSignal.DeviceID);
                        else
                            dbConnection.ExecuteNonQuery("INSERT INTO Device(Acronym, Name, UniqueID, ProtocolID) VALUES({0}, {1}, {2}, (SELECT ID FROM Protocol WHERE Acronym = 'VirtualInput'))", deviceAcronym, deviceName, metaSignal.DeviceID);

                        int deviceID = dbConnection.ExecuteScalar<int>("SELECT ID FROM Device WHERE UniqueID = {0}", metaSignal.DeviceID);
                        int signalTypeID = dbConnection.ExecuteScalar<int>("SELECT ID FROM SignalType WHERE Acronym = {0}", metaSignal.SignalType);

                        if (dbConnection.ExecuteScalar<int>("SELECT COUNT(*) FROM Measurement WHERE SignalID = {0}", metaSignal.SignalID) > 0)
                            dbConnection.ExecuteNonQuery("UPDATE Measurement SET DeviceID = {0}, PointTag = {1}, SignalTypeID = {2}, Description = {3}", metaSignal.DeviceID, metaSignal.PointTag, signalTypeID, metaSignal.Description);
                        else
                            dbConnection.ExecuteNonQuery("INSERT INTO Measurement(DeviceID, SignalID, PointTag, SignalTypeID, Description) VALUES({0}, {1}, {2}, {3}, {4})", deviceID, metaSignal.SignalID, metaSignal.PointTag, signalTypeID, metaSignal.Description);
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Meta-signal command failed due to exception: {ex.Message}";
                    OnProcessException(new Exception(errorMessage, ex));
                    SendClientResponse(connection.ClientID, ServerResponse.Failed, (ServerCommand)ECAServerCommand.StatusMessage, errorMessage);
                }
            });
        }

        private void HandleDataPacketCommand(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            try
            {
                List<IMeasurement> measurements = new List<IMeasurement>();

                int index = startIndex;
                int payloadByteLength = BigEndian.ToInt32(buffer, index);
                index += sizeof(int);

                string dataString = connection.Encoding.GetString(buffer, index, payloadByteLength);
                ConnectionStringParser<SettingAttribute> connectionStringParser = new ConnectionStringParser<SettingAttribute>();

                foreach (string measurementString in dataString.ParseKeyValuePairs().Values)
                {
                    ECAMeasurement measurement = new ECAMeasurement();
                    connectionStringParser.ParseConnectionString(measurementString, measurement);

                    measurements.Add(new Measurement()
                    {
                        Metadata = MeasurementKey.LookUpBySignalID(measurement.SignalID).Metadata,
                        Timestamp = measurement.Timestamp,
                        Value = measurement.Value,
                        StateFlags = measurement.StateFlags
                    });
                }

                OnNewMeasurements(measurements);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Data packet command failed due to exception: {ex.Message}";
                OnProcessException(new Exception(errorMessage, ex));
                SendClientResponse(connection.ClientID, ServerResponse.Failed, (ServerCommand)ECAServerCommand.StatusMessage, errorMessage);
            }
        }

        private void HandleStatusMessageCommand(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            try
            {
                if (startIndex + 1 + sizeof(int) > Math.Min(length, buffer.Length))
                {
                    SendClientResponse(connection.ClientID, ServerResponse.Failed, (ServerCommand)ECAServerCommand.StatusMessage, "Received status message command with no message payload.");
                    return;
                }

                int index = startIndex;
                UpdateType updateType = (UpdateType)buffer[index];
                index++;

                int messageLength = BigEndian.ToInt32(buffer, index);
                index += sizeof(int);

                if (messageLength < 0 || index + messageLength > Math.Min(length, buffer.Length))
                {
                    SendClientResponse(connection.ClientID, ServerResponse.Failed, (ServerCommand)ECAServerCommand.StatusMessage, "Received malformed status message command.");
                    return;
                }

                string message = connection.Encoding.GetString(buffer, index, messageLength);

                switch (updateType)
                {
                    case UpdateType.Information:
                        OnStatusMessage(message);
                        break;

                    case UpdateType.Warning:
                        OnStatusMessage($"WARNING: {message}");
                        break;

                    case UpdateType.Alarm:
                        OnProcessException(new Exception(message));
                        break;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Status message command failed due to exception: {ex.Message}";
                OnProcessException(new Exception(errorMessage, ex));
                SendClientResponse(connection.ClientID, ServerResponse.Failed, (ServerCommand)ECAServerCommand.StatusMessage, errorMessage);
            }
        }

        private void MakeConfigurationChanges(Action configurationChangeAction)
        {
            m_configurationChangedToken?.Cancel();
            configurationChangeAction.TryExecute(OnProcessException);
            m_configurationChangedToken = new Action(OnConfigurationChanged).DelayAndExecute(ConfigurationChangedTimeout);
        }

        #endregion
    }
}
