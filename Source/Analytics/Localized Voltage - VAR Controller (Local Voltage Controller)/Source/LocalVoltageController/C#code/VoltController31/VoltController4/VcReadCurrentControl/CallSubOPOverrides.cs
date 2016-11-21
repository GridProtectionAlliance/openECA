using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoltController.VcSubRoutines;
using VoltController.VcControlDevice;

namespace VoltController.VcReadCurrentControl
{
    public class CallSubOPOverrides
    {  
        //public void CallSubOPOverride()
        //{
        //    OpOverRides OpOR = new OpOverRides();
        //    OpOR.OpOverRide();

        //    VcSubstationInfomation VcSubInfo = new VcSubstationInfomation();
        //    VcCapacitorBank VcCap = new VcCapacitorBank();
            
        //    //----------------------------------------------------------------
        //    // Adjust the capbank control delaty counter, which is used to ensure:
        //    // a. We don't do two cap bank control within 30 minites of each other
        //    // b. We don't do a tap control within a minute of a cap bank control
        //    //----------------------------------------------------------------

        //    if (VcSubInfo.Ncdel < VcSubInfo.Zcdel)
        //    {
        //        VcSubInfo.Ncdel = VcSubInfo.Ncdel + 1;
        //    }

        //    if (VcSubInfo.Ntdel < VcSubInfo.Zdel)
        //    {
        //        VcSubInfo.Ntdel = VcSubInfo.Ntdel + 1;
        //    }
        //} 

    }
}
