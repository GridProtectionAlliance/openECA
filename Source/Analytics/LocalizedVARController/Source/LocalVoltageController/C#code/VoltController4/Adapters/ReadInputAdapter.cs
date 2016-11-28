//******************************************************************************************************
//  ReadInputAdapter.cs
//
//  Copyright © 2016, Duotong Yang  All Rights Reserved.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  11/09/2016 - Duotong Yang
//       Generated original version of source code.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using VoltController.VcControlDevice;
using VoltController.Adapters;
using VoltController.VcSubRoutines;


namespace VoltController.Adapters
{
    public class ReadInputAdapter
    {
        #region [ Private Members ]

        private Dictionary<string, VcTransformer> m_controlTransformers;
        //VoltVarController vc;


        #endregion

        #region [ Private Methods ]

        #region [ Read the InputData ]

        private void ReadSubAlarm(VoltVarController vc, string SubAlarmPathName)
        {
            CsvAdapter csvRead = new CsvAdapter();
            csvRead.ReadCSV(SubAlarmPathName);
            
            vc.SubstationAlarmDevice.Host =         csvRead.Frame[1, 0];
            vc.SubstationAlarmDevice.SwSchedId =    csvRead.Frame[1, 1];
            vc.SubstationAlarmDevice.SwSchedField = csvRead.Frame[1, 2];
            vc.SubstationAlarmDevice.SwSchedClear = csvRead.Frame[1, 3];
            vc.SubstationAlarmDevice.LtcDevice =    csvRead.Frame[1, 4];
            vc.SubstationAlarmDevice.ContFail =     csvRead.Frame[1, 5];
            vc.SubstationAlarmDevice.LtcLimit =     csvRead.Frame[1, 6];
            vc.SubstationAlarmDevice.OutService =   csvRead.Frame[1, 7];
            vc.SubstationAlarmDevice.VoltsOut =     csvRead.Frame[1, 8];
            vc.SubstationAlarmDevice.LtcProgram =   csvRead.Frame[1, 9];
            vc.SubstationAlarmDevice.ZNTX =         Convert.ToInt32(csvRead.Frame[1, 10]);
            vc.SubstationAlarmDevice.ZNCP =         Convert.ToInt32(csvRead.Frame[1, 11]);
            vc.SubstationAlarmDevice.ZVLO =         Convert.ToDouble(csvRead.Frame[1, 12]);
            vc.SubstationAlarmDevice.VLLIM =        Convert.ToDouble(csvRead.Frame[1, 13]);
            vc.SubstationAlarmDevice.VHLIM =        Convert.ToDouble(csvRead.Frame[1, 14]);
            vc.SubstationAlarmDevice.ZVHI =         Convert.ToDouble(csvRead.Frame[1, 15]);
            vc.SubstationAlarmDevice.ZHITAP =       Convert.ToInt32(csvRead.Frame[1, 16]);
            vc.SubstationAlarmDevice.ZLOTAP =       Convert.ToInt32(csvRead.Frame[1, 17]);
            vc.SubstationAlarmDevice.ZTCONS =       Convert.ToInt32(csvRead.Frame[1, 18]);
            vc.SubstationAlarmDevice.ZBAL =         Convert.ToDouble(csvRead.Frame[1, 19]);
            vc.SubstationAlarmDevice.ZLOWV =        Convert.ToDouble(csvRead.Frame[1, 20]);
            vc.SubstationAlarmDevice.ZDIFTAP =      Convert.ToDouble(csvRead.Frame[1, 21]);

        }

        private void ReadSubInformation(VoltVarController vc,string SubInformationPath, int rowNumber)
        {
            CsvAdapter csvRead = new CsvAdapter();
            csvRead.ReadCSV(SubInformationPath);

            vc.SubstationInformation.SubDevId =  csvRead.Frame[rowNumber, 0];
            vc.SubstationInformation.TieId =     csvRead.Frame[rowNumber, 1];
            vc.SubstationInformation.TieV =      Convert.ToInt32(csvRead.Frame[rowNumber, 2]);
            vc.SubstationInformation.CloverDev = csvRead.Frame[rowNumber, 3];
            vc.SubstationInformation.G1MwId =   csvRead.Frame[rowNumber, 4];
            vc.SubstationInformation.G1MvrId =    csvRead.Frame[rowNumber, 5];
            vc.SubstationInformation.G2MwId =   csvRead.Frame[rowNumber, 6];
            vc.SubstationInformation.G2MvrId =    csvRead.Frame[rowNumber, 7];
            vc.SubstationInformation.G1Mw  = Convert.ToDouble(csvRead.Frame[rowNumber, 8]);
            vc.SubstationInformation.G1Mvr   = Convert.ToDouble(csvRead.Frame[rowNumber, 9]);
            vc.SubstationInformation.G2Mw  = Convert.ToDouble(csvRead.Frame[rowNumber, 10]);
            vc.SubstationInformation.G2Mvr   = Convert.ToDouble(csvRead.Frame[rowNumber, 11]);

            vc.SubstationInformation.ConsecTap = Convert.ToInt32(csvRead.Frame[rowNumber, 12]);
            vc.SubstationInformation.ConsecCap = Convert.ToInt32(csvRead.Frame[rowNumber, 13]);
            vc.SubstationInformation.Ncdel =     Convert.ToInt32(csvRead.Frame[rowNumber, 14]);
            vc.SubstationInformation.Ntdel =     Convert.ToInt32(csvRead.Frame[rowNumber, 15]);
            vc.SubstationInformation.OldDay =    Convert.ToInt32(csvRead.Frame[rowNumber, 16]);
            vc.SubstationInformation.Zcdel =     Convert.ToInt32(csvRead.Frame[rowNumber, 17]);
            vc.SubstationInformation.Zdel =      Convert.ToInt32(csvRead.Frame[rowNumber, 18]);
            vc.SubstationInformation.Zmaxtrip =  Convert.ToInt32(csvRead.Frame[rowNumber, 19]);
            vc.SubstationInformation.Zmaxclose = Convert.ToInt32(csvRead.Frame[rowNumber, 20]);
            vc.SubstationInformation.Zccons =    Convert.ToInt32(csvRead.Frame[rowNumber, 21]);
            vc.SubstationInformation.Zclmvr =    Convert.ToDouble (csvRead.Frame[rowNumber, 22]);
            vc.SubstationInformation.Ztrmvr =    Convert.ToDouble(csvRead.Frame[rowNumber, 23]);
            vc.SubstationInformation.Alarm =     csvRead.Frame[rowNumber, 24];
            vc.SubstationInformation.Normal =    csvRead.Frame[rowNumber, 25];
            vc.SubstationInformation.Raise =     csvRead.Frame[rowNumber, 26];
            vc.SubstationInformation.Lower =     csvRead.Frame[rowNumber, 27];
            vc.SubstationInformation.ON =        csvRead.Frame[rowNumber, 28];
            vc.SubstationInformation.OFF =       csvRead.Frame[rowNumber, 29];
            vc.SubstationInformation.Close =     csvRead.Frame[rowNumber, 30];
            vc.SubstationInformation.Trip =      csvRead.Frame[rowNumber, 31];
            vc.SubstationInformation.Remote =    csvRead.Frame[rowNumber, 32];
            vc.SubstationInformation.Local =     csvRead.Frame[rowNumber, 33];
            vc.SubstationInformation.Auto =      csvRead.Frame[rowNumber, 34];
            vc.SubstationInformation.Manual =    csvRead.Frame[rowNumber, 35];
            vc.SubstationInformation.Dashes =    csvRead.Frame[rowNumber, 36];
        }

        private void ReadTransformer(VoltVarController vc, string datafolder, int rowNumber)
        {
            for (int i = 0; i < vc.SubstationAlarmDevice.ZNTX; i++)
            {
                int k = i + 1;
                CsvAdapter csvRead = new CsvAdapter();
                csvRead.ReadCSV(string.Format(datafolder + "Transformer{0}.csv", k));
                vc.ControlTransformers[i].DeviceId = csvRead.Frame[rowNumber, 0];
                vc.ControlTransformers[i].LocRemId = csvRead.Frame[rowNumber, 1];
                vc.ControlTransformers[i].LocRemV = csvRead.Frame[rowNumber, 2];
                vc.ControlTransformers[i].ScadaSw = csvRead.Frame[rowNumber, 3];
                vc.ControlTransformers[i].ScadaSwV = csvRead.Frame[rowNumber, 4];
                vc.ControlTransformers[i].HighSideId = csvRead.Frame[rowNumber, 5];
                vc.ControlTransformers[i].HighSideV = Convert.ToInt32(csvRead.Frame[rowNumber, 6]);
                vc.ControlTransformers[i].LowSideId = csvRead.Frame[rowNumber, 7];
                vc.ControlTransformers[i].LowSideV = Convert.ToInt32(csvRead.Frame[rowNumber, 8]);
                vc.ControlTransformers[i].TapId = csvRead.Frame[rowNumber, 9];
                vc.ControlTransformers[i].TapV = Convert.ToInt32(csvRead.Frame[rowNumber, 10]);
                vc.ControlTransformers[i].StTapV = Convert.ToInt32(csvRead.Frame[rowNumber, 11]);
                vc.ControlTransformers[i].MwId = csvRead.Frame[rowNumber, 12];
                vc.ControlTransformers[i].MwV = Convert.ToDouble(csvRead.Frame[rowNumber, 13]);
                vc.ControlTransformers[i].MvrId = csvRead.Frame[rowNumber, 14];
                vc.ControlTransformers[i].MvrV = Convert.ToDouble(csvRead.Frame[rowNumber, 15]);
                vc.ControlTransformers[i].StMvrV = Convert.ToInt32(csvRead.Frame[rowNumber, 16]);
                vc.ControlTransformers[i].VoltsId = csvRead.Frame[rowNumber, 17];
                vc.ControlTransformers[i].VoltsV = Convert.ToDouble(csvRead.Frame[rowNumber, 18]);
                vc.ControlTransformers[i].VoltsTime = Convert.ToInt32(csvRead.Frame[rowNumber, 19]);
                vc.ControlTransformers[i].LtcCtlId = csvRead.Frame[rowNumber, 20];
                vc.ControlTransformers[i].TapTime = Convert.ToInt32(csvRead.Frame[rowNumber, 21]);
                vc.ControlTransformers[i].CtlDone = Convert.ToInt32(csvRead.Frame[rowNumber, 22]);
                vc.ControlTransformers[i].InSvc = Convert.ToInt32(csvRead.Frame[rowNumber, 23]);
                vc.ControlTransformers[i].PrevCtl = csvRead.Frame[rowNumber, 24];
                vc.ControlTransformers[i].MvrBal = Convert.ToInt32(csvRead.Frame[rowNumber, 25]);
                vc.ControlTransformers[i].IRange = csvRead.Frame[rowNumber, 26];
            }        
        }

        private void ReadCapBanks(VoltVarController vc, string datafolder, int rowNumber)
        {
            for (int i = 0; i < vc.SubstationAlarmDevice.ZNCP; i++)
            {
                int k = i + 1;
     
                CsvAdapter csvRead = new CsvAdapter();
                csvRead.ReadCSV(string.Format(datafolder + "CapBank{0}.csv", k));
                vc.ControlCapacitorBanks[i].OpCapDev = csvRead.Frame[rowNumber, 0];
                vc.ControlCapacitorBanks[i].OpCapId = csvRead.Frame[rowNumber, 1];
                vc.ControlCapacitorBanks[i].OpCapV =  csvRead.Frame[rowNumber, 2];
                vc.ControlCapacitorBanks[i].ScadaSwId = csvRead.Frame[rowNumber, 3];
                vc.ControlCapacitorBanks[i].ScadaSwV = csvRead.Frame[rowNumber, 4];
                vc.ControlCapacitorBanks[i].LocRemId = csvRead.Frame[rowNumber, 5];
                vc.ControlCapacitorBanks[i].LocRemV =  csvRead.Frame[rowNumber, 6];
                vc.ControlCapacitorBanks[i].AutoManId = csvRead.Frame[rowNumber, 7];
                vc.ControlCapacitorBanks[i].AutoManV = vc.SubstationInformation.Manual;
                vc.ControlCapacitorBanks[i].MiscId =   csvRead.Frame[rowNumber, 9];
                vc.ControlCapacitorBanks[i].MiscV =    csvRead.Frame[rowNumber, 10];
                vc.ControlCapacitorBanks[i].BusBkrId = csvRead.Frame[rowNumber, 11];
                vc.ControlCapacitorBanks[i].BusBkrV =  csvRead.Frame[rowNumber, 12];
                vc.ControlCapacitorBanks[i].CapBkrId = csvRead.Frame[rowNumber, 13];
                vc.ControlCapacitorBanks[i].CapBkrV = csvRead.Frame[rowNumber, 14];
                vc.ControlCapacitorBanks[i].Clov = Convert.ToDouble(csvRead.Frame[rowNumber, 15]);
                vc.ControlCapacitorBanks[i].Chiv = Convert.ToDouble(csvRead.Frame[rowNumber, 16]);
                vc.ControlCapacitorBanks[i].Alovc = Convert.ToDouble(csvRead.Frame[rowNumber, 17]);
                vc.ControlCapacitorBanks[i].Ahivt = Convert.ToDouble(csvRead.Frame[rowNumber, 18]);
                vc.ControlCapacitorBanks[i].LockvDev = csvRead.Frame[rowNumber, 19];
                vc.ControlCapacitorBanks[i].LockvId = csvRead.Frame[rowNumber, 20];
                vc.ControlCapacitorBanks[i].LockvV = Convert.ToDouble(csvRead.Frame[rowNumber, 21]);
                vc.ControlCapacitorBanks[i].CapCtlDev = csvRead.Frame[rowNumber, 22];
                vc.ControlCapacitorBanks[i].CapCtlId = csvRead.Frame[rowNumber, 23];
                vc.ControlCapacitorBanks[i].Rtu = Convert.ToInt32(csvRead.Frame[rowNumber, 24]);
                vc.ControlCapacitorBanks[i].CtlDone = Convert.ToInt32(csvRead.Frame[rowNumber, 25]);
                vc.ControlCapacitorBanks[i].InSvc = Convert.ToInt32(csvRead.Frame[rowNumber, 26]);
                vc.ControlCapacitorBanks[i].PrevCtl = csvRead.Frame[rowNumber, 27];
                vc.ControlCapacitorBanks[i].NcTrip = Convert.ToInt32(csvRead.Frame[rowNumber, 28]);
                vc.ControlCapacitorBanks[i].NcClose = Convert.ToInt32(csvRead.Frame[rowNumber, 29]);
                vc.ControlCapacitorBanks[i].TripEx = Convert.ToInt32(csvRead.Frame[rowNumber, 30]);
                vc.ControlCapacitorBanks[i].CloseEx = Convert.ToInt32(csvRead.Frame[rowNumber, 31]);
       
            }
        }

        private void ReadLtcStatus(VoltVarController vc, string LtcStatusPath)
        {

            CsvAdapter csvRead = new CsvAdapter();
            csvRead.ReadCSV(LtcStatusPath);
            vc.LtcStatus.MinVar = Convert.ToDouble(csvRead.Frame[1, 0]);
            vc.LtcStatus.MaxVar = Convert.ToDouble(csvRead.Frame[1, 1]);
            vc.LtcStatus.MinTap =  Convert.ToInt32(csvRead.Frame[1, 2]);
            vc.LtcStatus.MaxTap =  Convert.ToInt32(csvRead.Frame[1, 3]);
            vc.LtcStatus.Nins =    Convert.ToInt32(csvRead.Frame[1, 4]);
            vc.LtcStatus.Avv =     Convert.ToInt32(csvRead.Frame[1, 5]);
            vc.LtcStatus.DifTap =  Convert.ToInt32(csvRead.Frame[1, 6]);
            vc.LtcStatus.Cfail =   Convert.ToInt32(csvRead.Frame[1, 7]);
            vc.LtcStatus.RTX =     Convert.ToInt32(csvRead.Frame[1, 8]);
            vc.LtcStatus.LTX =     Convert.ToInt32(csvRead.Frame[1, 9]);
        }

        private void CreateDictionary(VoltVarController vc)
        {
            vc.RawkeyValuePairs = new Dictionary<string, dynamic>();


            // Add Transformers
            foreach (VcTransformer vcTransformer in vc.ControlTransformers)
            {
                vc.RawkeyValuePairs.Add(vcTransformer.HighSideId, vcTransformer.HighSideV);
                vc.RawkeyValuePairs.Add(vcTransformer.LowSideId, vcTransformer.LowSideV);
                vc.RawkeyValuePairs.Add(vcTransformer.LocRemId, vcTransformer.LocRemV);
                vc.RawkeyValuePairs.Add(vcTransformer.MvrId, vcTransformer.MvrV);
                vc.RawkeyValuePairs.Add(vcTransformer.MwId, vcTransformer.MwV);     
                vc.RawkeyValuePairs.Add(vcTransformer.ScadaSw, vcTransformer.ScadaSwV);
                vc.RawkeyValuePairs.Add(vcTransformer.TapId, vcTransformer.TapV);
                vc.RawkeyValuePairs.Add(vcTransformer.VoltsId, vcTransformer.VoltsV);

            }

            foreach (VcCapacitorBank vcCapacitorbank in vc.ControlCapacitorBanks)
            {
                vc.RawkeyValuePairs.Add(vcCapacitorbank.OpCapId, vcCapacitorbank.OpCapV);
                vc.RawkeyValuePairs.Add(vcCapacitorbank.ScadaSwId, vcCapacitorbank.ScadaSwV);
                vc.RawkeyValuePairs.Add(vcCapacitorbank.MiscId, vcCapacitorbank.MiscV);
                vc.RawkeyValuePairs.Add(vcCapacitorbank.AutoManId, vcCapacitorbank.AutoManV);
                vc.RawkeyValuePairs.Add(vcCapacitorbank.BusBkrId, vcCapacitorbank.BusBkrV);
                vc.RawkeyValuePairs.Add(vcCapacitorbank.LockvId, vcCapacitorbank.LockvV);
                vc.RawkeyValuePairs.Add(vcCapacitorbank.CapBkrId, vcCapacitorbank.CapBkrV);

            }

            // Add Generators
            vc.RawkeyValuePairs.Add(vc.SubstationInformation.G1MwId, vc.SubstationInformation.G1Mw);
            vc.RawkeyValuePairs.Add(vc.SubstationInformation.G1MvrId, vc.SubstationInformation.G1Mvr);
            vc.RawkeyValuePairs.Add(vc.SubstationInformation.G2MwId, vc.SubstationInformation.G2Mw);
            vc.RawkeyValuePairs.Add(vc.SubstationInformation.G2MvrId, vc.SubstationInformation.G2Mvr);

        }

        #endregion

        #endregion

        #region [ Public Methods ]

        public VoltVarController ReadFrame(string inputdatafolder, string fileName, int rowNumber)
        {

            ReadInputAdapter ReadIn = new ReadInputAdapter();
            VoltVarController vc = new VoltVarController();

            string SubAlarmPath = "SubAlarm.csv";
            string SubInformationPath = "SubInformation.csv";
            string LtcStatusPath = "LtcStatus.csv";

            ReadIn.ReadSubAlarm(vc, inputdatafolder + SubAlarmPath);
            ReadIn.ReadSubInformation(vc, inputdatafolder + SubInformationPath, rowNumber);
            ReadIn.ReadLtcStatus(vc, inputdatafolder + LtcStatusPath);

            vc.LinkNetworkComponents(vc.SubstationAlarmDevice.ZNTX, vc.SubstationAlarmDevice.ZNCP);

            ReadIn.ReadTransformer(vc, inputdatafolder, rowNumber);
            ReadIn.ReadCapBanks(vc, inputdatafolder, rowNumber);

            ReadIn.CreateDictionary(vc);

            //vc.SerializeToXml(fileName);

            return vc;
        }
        
        public OpOverRides ReadOpOverRides(string inputdatafolder, string fileName)
        {
            OpOverRides overRides = new OpOverRides();
            CsvAdapter csvRead = new CsvAdapter();
            csvRead.ReadCSV(inputdatafolder + fileName);
            overRides.StatV = csvRead.Frame[1, 0];
            overRides.AnalV = csvRead.Frame[1, 1];
            return overRides;
        }
        
        #endregion
    }
}
