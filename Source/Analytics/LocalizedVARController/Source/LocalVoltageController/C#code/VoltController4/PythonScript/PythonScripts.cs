using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using VoltController.VcControlDevice;


namespace VoltController.PythonScript
{
    public class PythonScripts
    {
        public void RunCmd(string cmd, string args, string folder, VcSubstationInfomation subInformation, string testCaseName, VcCapacitorBank capacitorbank1, VcCapacitorBank capacitorbank2)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"C:\Python27\python.exe";
            start.Arguments = string.Format("{0} {1} \"{2}\" {3} {4} {5} {6} \"{7}\" {8} {9} {10} {11}", cmd, args, folder, subInformation.ConsecTap, subInformation.ConsecCap, subInformation.Ncdel, subInformation.Ntdel, testCaseName, capacitorbank1.NcTrip, capacitorbank2.NcTrip, capacitorbank1.NcClose, capacitorbank2.NcClose);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                }
            }
        }

        public void CleanCmd(string cmd, string folder1, string folder2,string folder3, string caseName, string testCaseName)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"C:\Python27\python.exe";
            start.Arguments = string.Format("{0} \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\"", cmd, folder1, folder2, folder3, caseName, testCaseName);

            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                }
            }
        }
    }
}
