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

def switchOffCap(shunt_bus_num):
    psspy.shunt_data(shunt_bus_num,r"""1""",1,[_f, 0])

#endregion

#region [ Main ]

######input######

percentage = 0
ScaleLoadAtBuses = [314691,314692,314693,314694,314695]
TransformerRatioChangeStep = 0.007
shunt_bus = [314521,314519]

######input######

# scale up the load


# silent the output
output = StringIO.StringIO()
with silence(output):
    psspy.case(r"""C:\Users\niezj\Desktop\16a_Fall\openECA_proj\LocalVoltageControl20161110\Test3\2019SUM_2013Series_Updated_forLocalVoltageControl.sav""")
    savecase = (r"""C:\Users\niezj\Desktop\16a_Fall\openECA_proj\LocalVoltageControl20161110\Test3\2019SUM_2013Series_Updated_forLocalVoltageControl_BenchMark.sav""")
    psspy.save(savecase)
    psspy.case(savecase)

    # determine the ratio and power flow of transformers
    bus_num = [314691,314692]
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

    # Scale up the load at certain percentage
    change_load(ScaleLoadAtBuses,percentage)
    load = aloadreal(0,1,'TOTALACT')
    load = load[0]

    #switch off the capbank
    for shunt_bus_num in shunt_bus:
        switchOffCap(shunt_bus_num)

    # run load flow
    psspy.fdns()
    N = psspy.solved()
    
    if N == 0:

        # measure the voltage at each bus
        psspy.bsys(sid = 1,numbus = len(bus_num+shunt_bus), buses = bus_num+shunt_bus)
        ierr,bus_voltage = psspy.abusreal(1,1,['PU'])
        bus_voltage = bus_voltage[0]
        psspy.save(savecase)

    else:

        print '### system collapses ###'


 #endregion
print 'Farmville voltage : '

print bus_voltage[0]*115
print bus_voltage[1]*230

print 'shunt voltage : '
print bus_voltage[2]*115
print bus_voltage[3]*115
