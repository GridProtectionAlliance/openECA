# File:"C:\Users\Duotong\Documents\DuotongYang\PSSE_simulation\ICSEG Power Case 1 - IEEE 14 Bus Systems\20150917_simulation.py", generated on THU, SEP 17 2015  10:10, release 32.00.03
from __future__ import with_statement
from contextlib import contextmanager
import os,sys

PSSE_LOCATION = r"C:\Program Files (x86)\PTI\PSSE32\PSSBIN"
sys.path.append(PSSE_LOCATION)
os.environ['PATH'] = os.environ['PATH'] + ';' + PSSE_LOCATION 
 
import psspy       # importing python
from psspy import _i,_f # importing the default integer and float values used by PSS\E(every API uses them) 
import redirect
redirect.psse2py() # redirecting PSS\E output to python)

import numpy     
import scipy
from scipy import special,optimize
import StringIO


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

output = StringIO.StringIO()
with silence(output):

    psspy.psseinit(80000)             # initialize PSS\E in python
    savecase = 'IEEE 57 bus.sav'
    psspy.case(savecase)


    # find all the buses
    psspy.bsys(0,0,[0.0,0.0],1,[1],0,[],0,[],0,[])
    ierr,all_bus = psspy.abusint(0,1,['number'])
    bus_num = all_bus[0]

    #List of all machines
    psspy.bsys(sid = 1,numbus = len(bus_num), buses = bus_num)
    ierr,machine_bus = psspy.amachint(1,1,['NUMBER'])
    machine_bus = machine_bus[0]
    ierr,machine_id =  psspy.amachchar(1,1,['ID'])
    machine_id = machine_id[0]

    #List of all Gen
    psspy.bsys(sid = 1,numbus = len(bus_num), buses = bus_num)
    ierr,gen_bus = psspy.agenbusint(1,1,['NUMBER'])
    gen_bus = gen_bus[0]

    #List of all load
    psspy.bsys(sid = 1,numbus = len(bus_num), buses = bus_num)
    ierr,load_bus = psspy.alodbusint(1,1,['NUMBER'])
    load_bus = load_bus[0]
    ierr,load_id =  psspy.aloadchar(1,1,['ID'])
    load_id = load_id[0]

    #List of branches
    ierr,internal_linesfbtb = psspy.abrnint(sid=1,ties=1,flag=1,string=['FROMNUMBER','TONUMBER'])
    ierr,internal_linesid = psspy.abrnchar(sid=1,ties=1,flag=1,string=['ID'])

    #Building the list of contingencies
    line_trip = internal_linesfbtb + internal_linesid  # [[fb1,tb1,id1]]
    response_buses = list(bus_num)

    # export the pq bus
    ierr, bus_type = psspy.abusint(1,1,'type')
    bus_type = bus_type[0]
    pq = []
    for index,bus in enumerate(bus_num):
        if bus_type[index] == 1:
            pq.append(bus)

    # export the slack bus
    slackBus = []
    for index,bus in enumerate(bus_num):
        if bus_type[index] == 3:
            slackBus.append(bus)

