#region [ Environmental Setup ]
from __future__ import with_statement
from __future__ import division
from contextlib import contextmanager
import os,sys,csv,pdb

PSSE_LOCATION_34 = r"""C:\Program Files (x86)\PTI\PSSE34\PSSPY27"""
PSSE_LOCATION_33 = r"""C:\Program Files (x86)\PTI\PSSE33\PSSBIN"""
if os.path.isdir(PSSE_LOCATION_34):
    sys.path.append(PSSE_LOCATION_34)
    import psse34, psspy
    
else:
    os.environ['PATH'] = PSSE_LOCATION_33 + ';' + os.environ['PATH']
    sys.path.append(PSSE_LOCATION_33)
    import psspy
    
from psspy import _i,_f # importing the default integer and float values used by PSS\E(every API uses them)
from pprint import pprint
import numpy as np
import scipy as sp
psspy.psseinit(80000)
import StringIO

#endregion

#region[ Defined Functions ]

@contextmanager
def silence(file_object=None):
    """
    Discard stdout (i.e. write to null device) or
    optionally write to given file-like object.
    """
    if file_object is None:
        file_object = open(os.devnull, 'w')

    old_stdout = sys.stdout
    try:
        sys.stdout = file_object
        yield
    finally:
        sys.stdout = old_stdout

def change_load(load_bus,percentage):
    psspy.bsys(0,0,[0.0,0.0],0,[],len(load_bus),load_bus,0,[],0,[])
    psspy.scal(sid = 0,all = 0, apiopt = 0,status1 = 2, status3 = 1, status4 = 1, scalval1 = percentage)

def changeTxTap(TransformerNumber, ratio):
    if TransformerNumber == 4:
        psspy.two_winding_chng_4(314691,314692,r"""1""",[_i,_i,_i,_i,_i,_i,_i,_i,314691,_i,_i,1,_i,_i,_i],[_f,_f,_f,_f,_f,_f,ratio,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f],["",""])
    elif TransformerNumber == 5:
        psspy.two_winding_chng_4(314691,314692,r"""2""",[_i,_i,_i,_i,_i,_i,_i,_i,314691,_i,_i,1,_i,_i,_i],[_f,_f,_f,_f,_f,_f,ratio,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f,_f],["",""])
    else:
        print("-------------------------------------------------------------")
        print("Cannot change Tx Ratio")
        print("Function: dscn")
        print("-------------------------------------------------------------")
        pdb.set_trace()

def aloadreal(sid,flag,string):
    ierr, iarray = psspy.aloadreal(sid,flag,string)
    if ierr:
      print("-------------------------------------------------------------")
      print("Cannot get load real values")
      print("Function: aloadreal")
      print("Error Code: " + str(ierr))
      print("-------------------------------------------------------------")
      pdb.set_trace()
    return iarray

def brnflo(ibus,jbus,ck):
    ierr, cmpval = psspy.brnflo(ibus,jbus,ck)
    if ierr == 3:
      return cmpval
    if ierr:
      print("-------------------------------------------------------------")
      print("Cannot get branch flow data.")
      print("Function: brnflo")
      print("Error Code: " + str(ierr))
      print("-------------------------------------------------------------")  
      pdb.set_trace()
    return cmpval

def amachreal(sid,flag,string):
    ierr, iarray = psspy.amachreal(sid,flag,string)
    if ierr:
      print("-------------------------------------------------------------")
      print("Cannot get machine real values")
      print("Function: amachreal")
      print("Error Code: " + str(ierr))
      print("-------------------------------------------------------------")  
      pdb.set_trace()
    return iarray

def switchOffCap(shunt_bus_num):
    psspy.shunt_data(shunt_bus_num,r"""1""",1,[_f, 0])

def switchOnCap(shunt_bus_num):
    psspy.shunt_data(shunt_bus_num,r"""1""",1,[_f, 24.38]) # This value is given by PSSE which is different from the design document

#def ChangeTap():

#endregion

#region [ Main ]

######input######

percentage = 2
ScaleLoadAtBuses = [314691,314692,314693,314694,314695]
TransformerRatioChangeStep = 0.007
bus_num = [314691,314692]
gen_bus = [315153,315154]
shunt_bus = [314521,314519]


######input######

# silent the output
output = StringIO.StringIO()
with silence(output):

    # read the control variables from C#
    SubstationName = []
    TransformerToControl = []
    Control = []
    CapSubstationName = []
    CapbankToControl = []
    CapControl = []
    CapControlSign = []
    NcTrip = [0]*2
    NcClose = [0]*2

    if __name__ == '__main__':
       for i in range(0,len(sys.argv[:])):
           if sys.argv[i] == "Decision":
               SubstationName.append(sys.argv[i+1])  
               TransformerToControl.append(sys.argv[i+2]) ## example: TransformerToControl.append("TX4LTC_CTL")
               Control.append(sys.argv[i+3]) ##Control.append("RAISE")

           if sys.argv[i] == "|CapControl":
               CapControlSign = sys.argv[i]
               CapSubstationName = sys.argv[i+1]
               CapbankToControl = sys.argv[i+2]
               CapControl = sys.argv[i+3]

       inputDataFolder = sys.argv[-10]
       ConsecTap = sys.argv[-9]
       ConsecCap = sys.argv[-8]
       Ncdel = sys.argv[-7]
       Ntdel = sys.argv[-6]
       testCaseName = sys.argv[-5]
       NcTrip[0] = sys.argv[-4]
       NcTrip[1] = sys.argv[-3]
       NcClose[0] = sys.argv[-2]
       NcClose[1] = sys.argv[-1]

    # Used for testing
    
    ##i in range(len(sys.argv)):
    ##    print sys.argv[i]
    ##    print '\n'
    ##print CapControlSign
    ##print '\n'
    ##print CapSubstationName
    ##print '\n'
    ##print CapControl


    # Load the PSSE save case
    psspy.case(testCaseName)
    savecase = (testCaseName)
    psspy.save(savecase)

    # determine the ratio and power flow of transformers
    psspy.bsys(sid = 1,numbus = 2, buses = bus_num)
    sid = 1
    flag = 2 # 2 = all non-tx branches 4 = all two-winding and non-transformer branches
    entry = 1 #1 = every branch once 2 = every branch from both sides
    ties = 1 # 1 = inside subsystem lines, # 2 = ties only, # 3 = everything
    ierr,ratio = psspy.atrnreal(sid,2,ties,flag,entry,['RATIO2'])
    ratio =ratio[0]
    fromflow = []
    for i in range(0,len(bus_num)):
        k = i + 1
        fromflow.append(brnflo(bus_num[1],bus_num[0],str(k)))

    # Load the PSSE save case
    psspy.case(testCaseName)
    savecase = (testCaseName)
    psspy.save(savecase)

    # determine the ratio and power flow of transformers
    psspy.bsys(sid = 1,numbus = 2, buses = bus_num)
    sid = 1
    flag = 2 # 2 = all non-tx branches 4 = all two-winding and non-transformer branches
    entry = 1 #1 = every branch once 2 = every branch from both sides
    ties = 1 # 1 = inside subsystem lines, # 2 = ties only, # 3 = everything
    ierr,ratio = psspy.atrnreal(sid,2,ties,flag,entry,['RATIO2'])
    ratio =ratio[0]
    fromflow = []
    for i in range(0,len(bus_num)):
        k = i + 1
        fromflow.append(brnflo(bus_num[1],bus_num[0],str(k)))

    ReactivePowerDifference = abs(fromflow[0].imag - fromflow[1].imag)
    RealPowerDifference = abs(fromflow[0].real - fromflow[1].real)

    # Determine the Load Buses to scale up the load
    psspy.bsys(0,0,[ 0.2, 999.],0,[],5,ScaleLoadAtBuses,0,[],0,[])
    ierr,load_bus = psspy.alodbusint(0,1,['NUMBER'])
    load_bus = load_bus[0]
    
    # determine which transformer should be controlled and how to control.
    tapChangeDirection = []
    for i in range(0,2):
        tapChangeDirection.append(0)
    if TransformerToControl:
        for i in range(0,len(TransformerToControl)):
           if TransformerToControl[i] == "TX4LTC_CTL":
               TransformerToControlIndex = 4
               TransformerRatioIndex = 0
           elif TransformerToControl[i] == "TX5LTC_CTL":
               TransformerToControlIndex = 5
               TransformerRatioIndex = 1
           if Control[i] == "RAISE":
               ratio[TransformerRatioIndex] = ratio[TransformerRatioIndex] - TransformerRatioChangeStep
               tapChangeDirection[TransformerRatioIndex] = 1
           else:
               ratio[TransformerRatioIndex] = ratio[TransformerRatioIndex] + TransformerRatioChangeStep
               tapChangeDirection[TransformerRatioIndex] = -1
           changeTxTap(TransformerToControlIndex,ratio[TransformerRatioIndex])


    # determine which Capbank should be controlled and how to control.
    SubstationIndex = []
    if CapSubstationName is not None:
        if CapSubstationName == "PAMP":
            SubstationIndex = 0
            if CapControl == "CLOSE":
                switchOnCap(shunt_bus[SubstationIndex])
            else:
                switchOffCap(shunt_bus[SubstationIndex])
        elif CapSubstationName == "CREW":
            SubstationIndex = 1
            if CapControl == "CLOSE":
                switchOnCap(shunt_bus[SubstationIndex])
            else:
                switchOffCap(shunt_bus[SubstationIndex])
 
    # write down the new tap ratio into csv files
    TransformersRatioFiles = inputDataFolder +"\\transformerRatio" + ".csv"
    WriteFile = open(TransformersRatioFiles,'a')
    writeRatio = csv.writer(WriteFile,delimiter = ',',lineterminator = '\n')
    writeRatio.writerow(ratio)
    WriteFile.close()

    # run load flow
    psspy.fdns()
    N = psspy.solved()

    if N == 0:

        # measure the voltage at Farm bus
        psspy.bsys(sid = 1,numbus = len(bus_num), buses = bus_num)
        ierr,bus_voltage = psspy.abusreal(1,1,['PU'])
        bus_voltage = bus_voltage[0]

        # measure the voltage at Pamp and Crew buses
        psspy.bsys(sid = 1,numbus = 1, buses = shunt_bus[0])
        ierr,shunt_Pamp_voltage = psspy.abusreal(1,1,['PU'])
        shunt_Pamp_voltage = shunt_Pamp_voltage[0][0]

        psspy.bsys(sid = 1,numbus = 1, buses = shunt_bus[1])
        ierr,shunt_Crew_voltage = psspy.abusreal(1,1,['PU'])
        shunt_Crew_voltage = shunt_Crew_voltage[0][0]

        shunt_bus_voltage = [shunt_Pamp_voltage,shunt_Crew_voltage]
        
        # measure the Gen output
        psspy.bsys(sid = 2,numbus = len(gen_bus), buses = gen_bus)
        Pgen = amachreal(2,1,'O_PGEN')
        Pgen = Pgen[0]
        Qgen = amachreal(2,1,'O_QGEN')
        Qgen = Qgen[0]

        # save measurements to Transformer files
        for i in range(0,len(bus_num)):

            # copy and paste the previous lie
            k = i + 1
            InputTransformerFile = (inputDataFolder +"\\transformer" + str(k) + ".csv")
            ReadFile = open(InputTransformerFile)
            r = csv.reader(ReadFile)
            lines = [line for line in r]
            ReadFile.close()

            # modify the parameters need to be changed
            WriteFile = open(InputTransformerFile,'a')
            w = csv.writer(WriteFile,delimiter = ',',lineterminator = '\n')
            newLine = lines[-1]  # copy a new line of data
            newLine[10] = str(int(newLine[10]) + tapChangeDirection[i])  # add or minus the number of tap
            newLine[13] = fromflow[i].real  # updates real power flow
            newLine[15] = fromflow[i].imag  # updates reactive power flow
            newLine[18] = bus_voltage[0]*115 # updates voltage
            w.writerow(newLine)
            WriteFile.close()
            
        # save measurements to Capbank file
        for i in range(0,len(bus_num)):
            k = i + 1
            InputCapbankFile = (inputDataFolder +"\\CapBank" + str(k) + ".csv")
            ReadFile = open(InputCapbankFile)
            r = csv.reader(ReadFile)
            lines = [line for line in r]
            ReadFile.close()

            WriteFile = open(InputCapbankFile,'a')
            w = csv.writer(WriteFile,delimiter = ',',lineterminator = '\n')
            newLine = lines[-1]  # copy a new line of data
            if i == SubstationIndex:
                newLine[14] = CapControl
            newLine[21] = shunt_bus_voltage[i]*115
            newLine[28] = NcTrip[i]
            newLine[29] = NcClose[i]
            w.writerow(newLine)
            WriteFile.close()

        # save the Delay information into Substation Information File
        InputInformationFile = (inputDataFolder +"\\SubInformation.csv")
        ReadFile = open(InputInformationFile)
        r = csv.reader(ReadFile)
        lines = [line for line in r]
        ReadFile.close()

        WriteFile = open(InputInformationFile,'a')
        w = csv.writer(WriteFile,delimiter = ',',lineterminator = '\n')
        newLine = lines[-1]  # copy a new line of data
        newLine[8] = Pgen[0]
        newLine[9] = Qgen[0]
        newLine[10] = Pgen[1]
        newLine[11] = Qgen[1]
        newLine[12] = ConsecTap
        newLine[13] = ConsecCap
        newLine[14] = Ncdel
        newLine[15] = Ntdel
        w.writerow(newLine)
        WriteFile.close()

        # save the power flow difference into a new file
        pfDifferenceFile = (inputDataFolder + "\\pfDifference.csv")
        WriteFile = open(pfDifferenceFile,'a')
        w_pfDifference = csv.writer(WriteFile,delimiter = ',',lineterminator = '\n')
        newLine = []
        for i in range(0,len(bus_num)):
            newLine.append(fromflow[i].real)
            newLine.append(fromflow[i].imag)
        newLine.append(newLine[0] - newLine[2])
        newLine.append(newLine[1] - newLine[3])
        w_pfDifference.writerow(newLine)
        WriteFile.close()

        # scale up the load
        change_load(load_bus,percentage)
        psspy.save(savecase)
        load = aloadreal(0,1,'TOTALACT')
        load = load[0]

    else:

        print '### system collapses ###'


 #endregion

