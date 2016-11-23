# File:"C:\Users\Duotong\Documents\DuotongYang\PSSE_simulation\ICSEG Power Case 1 - IEEE 14 Bus Systems\20150917_simulation.py", generated on THU, SEP 17 2015  10:10, release 32.00.03
from __future__ import with_statement
from contextlib import contextmanager
import os,sys
import csv
import math



PSSE_LOCATION = r"C:\Program Files (x86)\PTI\PSSE34\PSSPY27"
sys.path.append(PSSE_LOCATION)
os.environ['PATH'] = os.environ['PATH'] + ';' + PSSE_LOCATION

import psse34
import psspy       # importing python
from psspy import _i,_f # importing the default integer and float values used by PSS\E(every API uses them) 
import redirect
import PowerSystemPsseLibrary as pssepylib
import random, pdb
redirect.psse2py() # redirecting PSS\E output to python)

import numpy
import pdb
import scipy
from scipy import special,optimize
from scipy.sparse import bsr_matrix
from numpy import genfromtxt
from numpy import max

psspy.psseinit(80000)
savecase = 'IEEE_118.sav'
psspy.case(savecase)

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


####################################### main #############################################################

######input######

percentage_set = [100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20]
stepNumber = len(percentage_set)

######input######

PSSE_CASE = r"IEEE_118.sav"
psspy.case(PSSE_CASE)#r"""2019SUM_2013Series_Updated.sav""")
case_name="IEEE_118_100.sav"
psspy.save(case_name)
import pdb


### if you had already created voltageMeasurementAllBuses.csv, then please remove it.
#os.remove('voltageMeasurementAllBuses.csv')

#os.remove(savefile)

for index in range(0,stepNumber):

    # choose current percentage
    current_percentage = percentage_set[index]

    # choose the case
    case_name_constant = "IEEE_118_%s.sav"
    PSSE_CASE = case_name_constant % current_percentage
    psspy.case(PSSE_CASE)

    # silent the output
    output = StringIO.StringIO()
    with silence(output):

        #pick the bus in Dominion Area 345 is the index of Dominion
        psspy.case(savecase)
        # psspy.bsys(0,0,[ 0.2, 999.],1,[],0,[],0,[],0,[])
        ierr,all_bus = psspy.abusint(-1,1,['NUMBER'])
        bus_num = all_bus[0]

        #Load Bus
        psspy.bsys(sid = 1,numbus = len(bus_num), buses = bus_num)
        ierr,load_bus = psspy.alodbusint(1,1,['NUMBER'])
        load_bus = load_bus[0]

        # Gen Bus
        psspy.bsys(sid=1, numbus=len(bus_num), buses=bus_num)
        ierr, gen_bus = psspy.agenbusint(1, 1, ['NUMBER'])
        gen_bus = gen_bus[0]

        #change the load and the generation
        percentage = 1-(current_percentage-5)/current_percentage
        pssepylib.change_load(load_bus, percentage)
        increment = pssepylib.LoadIncreaseMW(load_bus, percentage)
        pssepylib.change_gen(gen_bus, increment)

        # try:
        #   load_real_sum = sum(load_real)
        # except:
        #   pdb.set_trace()

        # run load flow
        #psspy.case(savecase)
        psspy.fdns()

        # check convergency
        N = psspy.solved()


    if N == 0:

        output = StringIO.StringIO()
        with silence(output):
            case_name = case_name_constant % (current_percentage-5)
            psspy.save(case_name)

    else:

        print '### system collapses ###'
        #break

    # record step
    msg = 'Completion of Percentage: %s'
    new_percentage = current_percentage-5
    print msg % new_percentage

print 'The process has ended.'

