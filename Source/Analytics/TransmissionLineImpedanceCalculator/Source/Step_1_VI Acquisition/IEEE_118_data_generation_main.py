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

import math
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

def change_load(load_bus,percentage):
    psspy.bsys(0,0,[0.0,0.0],0,[],len(load_bus),load_bus,0,[],0,[])
    psspy.scal(sid = 0,all = 0, apiopt = 0, status1 = 2, status2 = 0,status3 = 1, status4 = 0, scalval1 = percentage)

####################################### main #############################################################

######input######

m = 1800#108001#
n = 2
import scipy.io
load_alter_percent = scipy.io.loadmat('load_alter_percent_sample.mat')
peak_load_alter_percent=list()
valley_load_alter_percent=list()
for i in range(m):
    peak_load_alter_percent.append(load_alter_percent.values()[0][i][0])
    valley_load_alter_percent.append(load_alter_percent.values()[0][i][1])

stepNumber = m

######input######

PSSE_CASE = r"IEEE_118.sav"
psspy.case(PSSE_CASE)#r"""2019SUM_2013Series_Updated.sav""")
savecase = (r"""IEEE_118_test_temp.sav""")

ierr, load_real = psspy.aloadreal(-1, 1, ['MVAACT'])
load_real_sum = sum(load_real[0])
msg = 'The original total load is: %s'
print msg % load_real_sum

baseKV = 345*1000/math.sqrt(3)
baseMVA = 100*1000000

psspy.save(savecase)
import pdb


### if you had already created voltageMeasurementAllBuses.csv, then please remove it.
#os.remove('voltageMeasurementAllBuses.csv')

for peak_valley_index in range(1):

    bus_number_recorded_flag =0;
    # choose the percent
    if peak_valley_index == 0:
        percent_set = peak_load_alter_percent
        savefile = 'VI_Measurement_All_345KV_Buses_Peak.csv'
        print 'Processing PEAK LOAD data.'
    else:
        percent_set = valley_load_alter_percent

        savefile = 'VI_Measurement_All_345KV_Buses_Valley.csv'
        print 'Processing Valley LOAD data.'

    #os.remove(savefile)
    with open(savefile,'a') as f1:
        for index in range(0,stepNumber):

            record_inspected_bus_current = []
            record_inspected_bus_line_bus_info = []
            inspected_bus_number = 64

            # silent the output
            output = StringIO.StringIO()
            with silence(output):

                # pick the bus in Dominion Area 345 is the index of Dominion
                psspy.case(savecase)
                ierr, all_bus = psspy.abusint(-1, 1, ['number'])
                bus_num = all_bus[0]

                # Load Bus
                psspy.bsys(sid=-1, numbus=len(bus_num), buses=bus_num)
                ierr, load_bus = psspy.alodbusint(-1, 1, ['NUMBER'])
                load_bus = load_bus[0]

                # Gen Bus
                psspy.bsys(sid=-1, numbus=len(bus_num), buses=bus_num)
                ierr, gen_bus = psspy.agenbusint(-1, 1, ['NUMBER'])
                gen_bus = gen_bus[0]

                # Choose the proper case
                current_percentage = (1 + percent_set[index]) * 100
                level = math.ceil(current_percentage / 5)
                case_percent = level * 5
                case_name_constant = "IEEE_118_%s.sav"
                current_CASE = case_name_constant % case_percent
                psspy.case(current_CASE)

                # change the load and the generation
                percentage = (current_percentage - case_percent) / case_percent * 100
                pssepylib.change_load(load_bus, percentage)
                increment = pssepylib.LoadIncreaseMW(load_bus, percentage)
                pssepylib.change_gen(gen_bus, increment)

                psspy.fdns()

                # check convergency
                N = psspy.solved()

            if N == 0:

                # Select DVP system as the current subsystem object
                psspy.bsys(1, 1, [344, 346], 0, [], 0, [], 0, [], 0, [])
                ierr_bus_number, bus_number = psspy.abusint(1, 2, ['NUMBER'])
                bus_number = bus_number[0]

                # record the bus number in first row of each file
                if bus_number_recorded_flag == 0:
                    bus_number_recorded_flag = 1
                    bus_number_row = []
                    bus_number_row.extend(bus_number)
                    writer = csv.writer(f1, delimiter=',', lineterminator='\n')
                    writer.writerow(bus_number_row)

                # measure the voltage at each bus
                # choose 500KV buses in DVP system

                # record bus voltage magnitudes
                ierr, bus_voltage_m = psspy.abusreal(1, 1, ['PU'])
                bus_voltage_m = bus_voltage_m[0]
                # record bus voltage phase imags
                ierr, bus_voltage_a = psspy.abusreal(1, 1, ['ANGLE'])
                bus_voltage_a = bus_voltage_a[0]  # In radians

                ierr, bus_voltage_complex = psspy.abuscplx(1, 1, ['VOLTAGE'])
                bus_voltage_complex = bus_voltage_complex[0]

                bus_voltage = list()
                for i in range(len(bus_voltage_m)):
                    current_m = bus_voltage_m[i] * baseKV
                    current_a = bus_voltage_a[i]
                    bus_voltage.append(current_m)
                    bus_voltage.append(current_a)

                # save voltage magnitudes and imags in csv
                db_row = []
                db_row.extend(bus_voltage)
                writer = csv.writer(f1, delimiter=',', lineterminator='\n')
                writer.writerow(db_row)

                ## find all the injections of each concerned bus
                total_bus_num = len(bus_number)

                # All transmission lines
                ierr, from_bus_number_set = psspy.abrnint(sid=1, ties=3, entry=2, string='FROMNUMBER')
                from_bus_number_set = from_bus_number_set[0]
                ierr, to_bus_number_set = psspy.abrnint(sid=1, ties=3, entry=2, string='TONUMBER')
                to_bus_number_set = to_bus_number_set[0]
                ierr, from_bus_PQ_set = psspy.abrncplx(sid=1, ties=3, entry=2, string='PQ')
                from_bus_PQ_set = from_bus_PQ_set[0]
                ierr, branch_ID_set = psspy.abrnchar(sid=1, ties=3, entry=2, string='ID')
                branch_ID_set = branch_ID_set[0]
                line_num = len(from_bus_number_set)

                ierr, bus_number_set = psspy.abusint(sid=-1, string='NUMBER')
                bus_number_set = bus_number_set[0]
                ierr, bus_voltage_set = psspy.abuscplx(sid=-1, string='VOLTAGE')
                bus_voltage_set = bus_voltage_set[0]
                ierr, baseKV_set = psspy.abusreal(sid=-1, string='BASE')
                baseKV_set = baseKV_set[0]

                # import pdb
                # pdb.set_trace()
                # find all lines connected to DVP system
                recorded_line_bus_info = []
                recorded_flag = 0
                connected_line_currents_row = []  # save lines currents

                connected_line_num = len(from_bus_number_set) / 2

                for idx2 in range(connected_line_num * 2):
                    temp_from_bus_number = from_bus_number_set[idx2]
                    temp_to_bus_number = to_bus_number_set[idx2]
                    temp_branch_ID = branch_ID_set[idx2]

                    if ([temp_from_bus_number, temp_to_bus_number, temp_branch_ID] in recorded_line_bus_info) or (
                                [temp_to_bus_number, temp_from_bus_number, temp_branch_ID] in recorded_line_bus_info):
                        recorded_flag = 1
                    else:
                        recorded_line_bus_info.append([temp_from_bus_number, temp_to_bus_number, temp_branch_ID])

                    if recorded_flag == 1:
                        recorded_flag = 0
                        continue

                    from_index = bus_number_set.index(temp_from_bus_number)
                    to_index = bus_number_set.index(temp_to_bus_number)

                    PQ_i = from_bus_PQ_set[idx2] * 1000
                    for idx21 in range(connected_line_num * 2):
                        if (from_bus_number_set[idx21] == temp_to_bus_number) & (
                                    to_bus_number_set[idx21] == temp_from_bus_number) & (
                                    branch_ID_set[idx21] == temp_branch_ID):
                            PQ_k = from_bus_PQ_set[idx21] * 1000

                    from_bus_voltage = bus_voltage_set[from_index] * baseKV_set[from_index]
                    to_bus_voltage = bus_voltage_set[to_index] * baseKV_set[to_index]

                    I_ik_i = numpy.conjugate(PQ_i / from_bus_voltage) / numpy.sqrt(3)
                    I_ik_k = numpy.conjugate(PQ_k / to_bus_voltage) / numpy.sqrt(3)

                    I_ik_i_R = numpy.real(I_ik_i)
                    I_ik_i_I = numpy.imag(I_ik_i)
                    I_ik_k_R = numpy.real(I_ik_k)
                    I_ik_k_I = numpy.imag(I_ik_k)

                    # save voltage magnitudes and imags in csv
                    # if (I_ik_i_R > 0.01) & (I_ik_k_R > 0.01):
                    connected_line_currents_row.extend([temp_from_bus_number, temp_to_bus_number, temp_branch_ID])
                    connected_line_currents_row.extend([I_ik_i_R, I_ik_i_I, I_ik_k_R, I_ik_k_I])

                    if temp_from_bus_number == inspected_bus_number:
                        record_inspected_bus_current.append(I_ik_i)
                        record_inspected_bus_line_bus_info.append([temp_from_bus_number, temp_to_bus_number, temp_branch_ID])
                    if temp_to_bus_number == inspected_bus_number:
                        record_inspected_bus_current.append(I_ik_k)
                        record_inspected_bus_line_bus_info.append([temp_from_bus_number, temp_to_bus_number, temp_branch_ID])

                # 2_wingding Transformers
                ierr, from_bus_number_set_trn = psspy.atrnint(sid=1, ties=3, entry=2, string='FROMNUMBER')
                from_bus_number_set_trn = from_bus_number_set_trn[0]
                ierr, to_bus_number_set_trn = psspy.atrnint(sid=1, ties=3, entry=2, string='TONUMBER')
                to_bus_number_set_trn = to_bus_number_set_trn[0]
                ierr, from_bus_PQ_set_trn = psspy.atrncplx(sid=1, ties=3, entry=2, string='PQ')
                from_bus_PQ_set_trn = from_bus_PQ_set_trn[0]
                ierr, branch_ID_set_trn = psspy.atrnchar(sid=1, ties=3, entry=2, string='ID')
                branch_ID_set_trn = branch_ID_set_trn[0]
                two_winding_trn_num = len(from_bus_number_set_trn) / 2

                # find all 2-winding transformers connected to DVP system
                # 1 - from bus, 0 - to bus
                connected_trn_currents_row = []  # save lines currents
                recorded_line_bus_info = []
                recorded_flag = 0
                for idx3 in range(two_winding_trn_num * 2):
                    temp_from_bus_number = from_bus_number_set_trn[idx3]
                    temp_to_bus_number = to_bus_number_set_trn[idx3]
                    temp_branch_ID = branch_ID_set_trn[idx3]

                    from_index = bus_number_set.index(temp_from_bus_number)
                    to_index = bus_number_set.index(temp_to_bus_number)

                    if ([temp_from_bus_number, temp_to_bus_number, temp_branch_ID] in recorded_line_bus_info) or (
                                [temp_to_bus_number, temp_from_bus_number, temp_branch_ID] in recorded_line_bus_info):
                        recorded_flag = 1
                    else:
                        recorded_line_bus_info.append([temp_from_bus_number, temp_to_bus_number, temp_branch_ID])

                    if recorded_flag == 1:
                        recorded_flag = 0
                        continue

                    if temp_from_bus_number in bus_number:
                        PQ_i = from_bus_PQ_set_trn[idx3] * 1000
                        for idx31 in range(two_winding_trn_num * 2):
                            if (from_bus_number_set_trn[idx31] == temp_to_bus_number) & (
                                        to_bus_number_set_trn[idx31] == temp_from_bus_number) & (
                                        branch_ID_set_trn[idx31] == temp_branch_ID):
                                PQ_k = from_bus_PQ_set_trn[idx31] * 1000

                        I_ik_i = numpy.conjugate(
                            PQ_i / (bus_voltage_set[from_index] * baseKV_set[from_index])) / numpy.sqrt(3)
                        I_ik_k = numpy.conjugate(
                            PQ_k / (bus_voltage_set[to_index] * baseKV_set[to_index])) / numpy.sqrt(3)

                        I_ik_i_R = numpy.real(I_ik_i)
                        I_ik_i_I = numpy.imag(I_ik_i)
                        I_ik_k_R = numpy.real(I_ik_k)
                        I_ik_k_I = numpy.imag(I_ik_k)

                        # save voltage magnitudes and imags in csv
                        # if (I_ik_i_R >0.01) & (I_ik_k_R >0.01):
                        connected_trn_currents_row.extend(
                            [temp_from_bus_number, temp_to_bus_number, temp_branch_ID, 1])
                        connected_trn_currents_row.extend([I_ik_i_R, I_ik_i_I, I_ik_k_R, I_ik_k_I])

                    if temp_to_bus_number in bus_number:
                        PQ_i = from_bus_PQ_set_trn[idx3] * 1000
                        for idx31 in range(two_winding_trn_num * 2):
                            if (from_bus_number_set_trn[idx31] == temp_to_bus_number) & (
                                        to_bus_number_set_trn[idx31] == temp_from_bus_number) & (
                                        branch_ID_set_trn[idx31] == temp_branch_ID):
                                PQ_k = from_bus_PQ_set_trn[idx31] * 1000

                        I_ik_i = numpy.conjugate(
                            PQ_i / (bus_voltage_set[from_index] * baseKV_set[from_index])) / numpy.sqrt(3)
                        I_ik_k = numpy.conjugate(
                            PQ_k / (bus_voltage_set[to_index] * baseKV_set[to_index])) / numpy.sqrt(3)

                        I_ik_i_R = numpy.real(I_ik_i)
                        I_ik_i_I = numpy.imag(I_ik_i)
                        I_ik_k_R = numpy.real(I_ik_k)
                        I_ik_k_I = numpy.imag(I_ik_k)

                        # save voltage magnitudes and imags in csv
                        # if (I_ik_i != 0) & (I_ik_k != 0):
                        connected_trn_currents_row.extend(
                            [temp_from_bus_number, temp_to_bus_number, temp_branch_ID, 0])
                        connected_trn_currents_row.extend([I_ik_i_R, I_ik_i_I, I_ik_k_R, I_ik_k_I])

                    if temp_from_bus_number == inspected_bus_number:
                        record_inspected_bus_current.append(I_ik_i)
                        record_inspected_bus_line_bus_info.append(
                            [temp_from_bus_number, temp_to_bus_number, temp_branch_ID])
                    if temp_to_bus_number == inspected_bus_number:
                        record_inspected_bus_current.append(I_ik_k)
                        record_inspected_bus_line_bus_info.append(
                            [temp_from_bus_number, temp_to_bus_number, temp_branch_ID])

                # injections = numpy.sum(record_8_current)

                # Plants
                ierr, gen_bus_number_set = psspy.agenbusint(sid=1, string='NUMBER')
                gen_bus_number_set = gen_bus_number_set[0]
                ierr, gen_bus_PQ_set = psspy.agenbuscplx(sid=1, string='PQGEN')
                gen_bus_PQ_set = gen_bus_PQ_set[0]
                gen_num = len(gen_bus_number_set)

                connected_gen_currents_row = []  # save lines currents
                for idx4 in range(gen_num):
                    temp_bus_number = gen_bus_number_set[idx4]

                    bus_index = bus_number_set.index(temp_bus_number)

                    PQ = gen_bus_PQ_set[idx4] * 1000

                    I = numpy.conjugate(PQ / (bus_voltage_set[bus_index] * baseKV_set[bus_index])) / numpy.sqrt(3)
                    I = -1*I

                    I_R = numpy.real(I)
                    I_I = numpy.imag(I)

                    connected_gen_currents_row.extend([temp_bus_number,-1])
                    connected_gen_currents_row.extend([I_R, I_I])

                    if temp_bus_number == inspected_bus_number:
                        record_inspected_bus_current.append(I)
                        record_inspected_bus_line_bus_info.append([temp_bus_number, -1])

                injections = numpy.sum(record_inspected_bus_current)

                writer = csv.writer(f1, delimiter=',', lineterminator='\n')
                writer.writerow(connected_line_currents_row)
                writer.writerow(connected_trn_currents_row)
                writer.writerow(connected_gen_currents_row)

                # # record percentage information
                # msg1 = 'Current percentage is: %s'
                # print msg1 % current_percentage
                # msg2 = 'Level is: %s'
                # print msg2 % level
                # msg3 = 'Case percent is: %s'
                # print msg3 % case_percent
                # msg4 = 'Current case is: %s'
                # print msg4 % current_CASE
                # msg5 = 'Percentage is: %s'
                # print msg5 % percentage

            else:

                print '### system collapses ###'
                #break

            # record time stamp
            msg = 'Completion of Time Stamp: %s'
            time_stamp_number = index + 1
            print msg % time_stamp_number

print 'The process has ended.'

