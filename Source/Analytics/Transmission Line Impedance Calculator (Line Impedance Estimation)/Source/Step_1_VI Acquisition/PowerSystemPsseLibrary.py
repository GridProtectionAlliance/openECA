# File:"C:\Users\Duotong\Documents\DuotongYang\PSSE_simulation\ICSEG Power Case 1 - IEEE 14 Bus Systems\20150917_simulation.py", generated on THU, SEP 17 2015  10:10, release 32.00.03
from __future__ import with_statement
from __future__ import division
from contextlib import contextmanager
import os,sys
import csv



PSSE_LOCATION = r"C:\Program Files (x86)\PTI\PSSE32\PSSBIN"
sys.path.append(PSSE_LOCATION)
os.environ['PATH'] = os.environ['PATH'] + ';' + PSSE_LOCATION

import psspy       # importing python
from psspy import _i,_f # importing the default integer and float values used by PSS\E(every API uses them)
import redirect, random, pdb, time
redirect.psse2py() # redirecting PSS\E output to python)

import numpy
import difflib
import pdb
import scipy
import heapq
import itertools
from scipy import special,optimize
from scipy.sparse import bsr_matrix
from numpy import genfromtxt,max




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

#### Update on OC changing: the region changes becomes owner changes
        
def change_load(load_bus,percentage):
    psspy.bsys(0,0,[0.0,0.0],0,[],len(load_bus),load_bus,0,[],0,[])
    psspy.scal(sid = 0,all = 0, apiopt = 0,status1 = 2, status3 = 1, status4 = 1, scalval1 = percentage)


def change_gen(gen_bus,increment):
    psspy.bsys(0,0,[0.0,0.0],0,[],len(gen_bus),gen_bus,0,[],0,[])
    psspy.scal(sid = 0,all = 0, apiopt = 0,status1 = 3, scalval2 = increment)

def LoadIncreaseMW(load_bus,percentage):
    psspy.bsys(0,0,[0.0,0.0],0,[],len(load_bus),load_bus,0,[],0,[])
    ierr,allBusLoad = psspy.aloadcplx(0,1,['MVAACT'])
    allBusLoad = allBusLoad[0]
    BusLoadReal = numpy.real(allBusLoad)
    return numpy.sum(BusLoadReal)*percentage/100


def changeOperatingCondition(numberofRegions,index_OC,loadIncrease,load_bus_region,gen_bus_region):
    #change load operating points
    for region in range(0,numberofRegions):
        
        # Compute load increament in MW   
        loadIncrementMW = LoadIncreaseMW(load_bus_region[region],loadIncrease[index_OC,region])

        # change region load 
        change_load(load_bus_region[region],loadIncrease[index_OC,region])
        
        # re-dispatch Pgen
        change_gen(gen_bus_region[region],loadIncrementMW)
        
##########################################################################
def product(*args, **kwds):
    # product('ABCD', 'xy') --> Ax Ay Bx By Cx Cy Dx Dy
    # product(range(2), repeat=3) --> 000 001 010 011 100 101 110 111
    pools = map(tuple, args) * kwds.get('repeat', 1)
    result = [[]]
    for pool in pools:
        result = [x+[y] for x in result for y in pool]
    for prod in result:
        yield tuple(prod)

def zerolistmaker(n):
    listofzeros = [0] * n
    return listofzeros

def computeJacobianMatrix(linedata,bus_voltage,bus_angle,nbus,pq,npq,response_buses,nonSlackBus):
## 20160212: update angle calculation
## 20160327: compared with all other methods already

    fb = linedata[:,0]
    tb = linedata[:,1]
    G_y = linedata[:,2]
    B_y = linedata[:,3]
    nb = len(response_buses)
    nl= len(fb)
    G = bsr_matrix((nb,nb)).todense()
    B = bsr_matrix((nb,nb)).todense()

    ### computes G and B matix based on Y
    for k in range(0,nl):
        i = response_buses.index(fb[k])
        j = response_buses.index(tb[k])
        G[i,j] = linedata[k,2]
        G[j,i] = G[i,j]

    for k in range(0,nl):
        i = response_buses.index(fb[k])
        j = response_buses.index(tb[k])
        B[i,j] = linedata[k,3]
        B[j,i] = B[i,j]

    ### compute the jacobian matrix
    from numpy import sin
    from numpy import cos

    J1 = bsr_matrix((len(nonSlackBus),len(nonSlackBus))).todense() # -1 is to remove the slack bus
    for i in range(0,len(nonSlackBus)):
        m = response_buses.index(nonSlackBus[i])
        for k in range(0,len(nonSlackBus)):
            n = response_buses.index(nonSlackBus[k])
            if n == m:
                for n in range(0,nbus):
                    J1[i,k] = J1[i,k] + bus_voltage[m]*bus_voltage[n]*(-1*G[m,n]*sin(bus_angle[m]-bus_angle[n]) + B[m,n]*cos(bus_angle[m] - bus_angle[n]))
                J1[i,k] = J1[i,k] - numpy.square(bus_voltage[m])*B[m,m]
            else:
                J1[i,k] = bus_voltage[m]*bus_voltage[n]*(G[m,n]*sin(bus_angle[m]-bus_angle[n]) - B[m,n]*cos(bus_angle[m] - bus_angle[n]))


    J2 = bsr_matrix((len(nonSlackBus),npq)).todense() # -1 is to remove the slack bus
    for i in range(0,len(nonSlackBus)):
        m = response_buses.index(nonSlackBus[i])
        for k in range(0,npq):
            n = response_buses.index(pq[k])
            if n == m:
                for n in range(0,nbus):
                    J2[i,k] = J2[i,k] + bus_voltage[n]*(G[m,n]*cos(bus_angle[m]-bus_angle[n]) + B[m,n]*sin(bus_angle[m] - bus_angle[n]))
                J2[i,k] = J2[i,k] + bus_voltage[m]*G[m,m]
            else:
                J2[i,k] = bus_voltage[m]*(G[m,n]*cos(bus_angle[m]-bus_angle[n]) + B[m,n]*sin(bus_angle[m] - bus_angle[n]))


    J3 = bsr_matrix((npq,len(nonSlackBus))).todense() # -1 is to remove the slack bus
    for i in range(0,npq):
        m = response_buses.index(pq[i])
        for k in range(0,len(nonSlackBus)):
            n = response_buses.index(nonSlackBus[k])
            if n == m:
                for n in range(0,nbus):
                    J3[i,k] = J3[i,k] + bus_voltage[m]*bus_voltage[n]*(G[m,n]*cos(bus_angle[m]-bus_angle[n]) + B[m,n]*sin(bus_angle[m] - bus_angle[n]))
                J3[i,k] = J3[i,k] - numpy.square(bus_voltage[m])*G[m,m]
            else:
                J3[i,k] = bus_voltage[m]*bus_voltage[n]*(-1*G[m,n]*cos(bus_angle[m]-bus_angle[n]) - B[m,n]*sin(bus_angle[m] - bus_angle[n]))



    J4 = bsr_matrix((npq,npq)).todense() # load_bus is the PQ bus
    for i in range(0,npq):
        m = response_buses.index(pq[i])
        for k in range(0,npq):
            n = response_buses.index(pq[k])
            if n == m:
                for n in range(0,nbus):
                    J4[i,k] = J4[i,k] + bus_voltage[n]*((G[m,n]*sin(bus_angle[m]-bus_angle[n]) - B[m,n]*cos(bus_angle[m] - bus_angle[n])))
                J4[i,k] = J4[i,k] - bus_voltage[m]*B[m,m]
            else:
                J4[i,k] = bus_voltage[m]*(G[m,n]*sin(bus_angle[m]-bus_angle[n]) - B[m,n]*cos(bus_angle[m] - bus_angle[n]))

    return J1,J2,J3,J4

def computeCriticalBus(J4,pq,thresholdParticipationFactor):
    #compute the eigen values and left/right eigenvector
    from scipy.linalg import eig
    eigenvalue,leftEigVector,rightEigVector = eig(J4,left = True)

    #compute the critical mode
    min_eig_index = numpy.argmin(eigenvalue)
    min_eigvalue = eigenvalue[min_eig_index]

    #compute the participation factor 
    ParticipationFactor = []
    for k in range(0,npq):
        ParticipationFactor.append(rightEigVector[k][min_eig_index]*leftEigVector[min_eig_index][k])

    #compute the critical buses based on threshold value
    LargestParticipationFactor = []
    CriticalBus = []
    NormalizedParticipationFactor = numpy.true_divide(ParticipationFactor,max(ParticipationFactor))
    for k in range(0,len(NormalizedParticipationFactor)):
        if NormalizedParticipationFactor[k] >= thresholdParticipationFactor:
            LargestParticipationFactor.append(NormalizedParticipationFactor[k])
            CriticalBus.append(pq[k])

    #rank the buses
    order = NormalizedParticipationFactor.argsort()
    ranks = order.argsort()
    NormalizedRank = numpy.true_divide(ranks,max(ranks))
    
    return CriticalBus,LargestParticipationFactor,NormalizedParticipationFactor,ranks,NormalizedRank


def performModalAnalysis(Jr,pq,CriticalEigenValueNumber,CriticalBusesNumber):
#############################################
#This function is able to compute critical bus and critical eigenvalues

#20160211 update the eigenvalue computation: minimum eigenvalue is determined based on their magnitude only
#         update the PF computation: PF is determined by its eigenvector element's magnitude only, since the eigenvector could be complex.

#20160212 update the critical eigenvalue selection method: negative eigenvalues could be voltage unstable so they will be considered as critical buses

#20160325 can update more critical eigenvalues and more PF.

#20160327 functionalized
#############################################
    #compute the eigenvalues and left/right eigenvector
    import operator
    from scipy.linalg import eig
    eigenvalue,leftEigVector,rightEigVector = eig(Jr,left = True)

    # compute the magnitude of eigenvalue
    negative_EigenvalueRealPart = []
    negative_EigenvalueRealPart_index = []

    #if all eigenvalue are larger than 0...
    if all(numpy.real(value) >= 0 for value in eigenvalue) == True:

        eigenvalue_magnitude = numpy.abs(eigenvalue)

        # compute the critical mode based on the smallest eigenvalues
        min_eig_index = sorted(range(len(eigenvalue_magnitude)), key=lambda x: eigenvalue_magnitude[x])
        critical_eig_index = min_eig_index[0:CriticalEigenValueNumber]
        critical_eig = [eigenvalue[i] for i in critical_eig_index]

    else:

        #output the eigenvalue's real part smaller than 0
        for index_eigvalue in range(0,len(eigenvalue)):
            if numpy.real(eigenvalue[index_eigvalue]) < 0:
                negative_EigenvalueRealPart.append(eigenvalue[index_eigvalue])
                negative_EigenvalueRealPart_index.append(index_eigvalue)

        #output minimum critical eigenvalue & index
        min_critical_eig_index, min_critical_eigvalue = min(enumerate(negative_EigenvalueRealPart), key=operator.itemgetter(1))
        critical_eig_index = negative_EigenvalueRealPart_index[min_critical_eig_index]
        critical_eig = min_critical_eigvalue

    #initialize the participation factor
    npq = len(pq)
    CriticalBus_CriticalMode = []
    LargestNormalizedParticipationFactor_CriticalMode = []
    LargestParticipationFactor_CriticalMode = []
    indexofLargestNormalizedParticipationFactor_CriticalMode = []

    #for each citical mode compute its PFs
    for eig_index in range(0,len(critical_eig_index)):
        ParticipationFactor = []
        for k in range(0,npq):
            ParticipationFactor.append(numpy.abs(rightEigVector[k][critical_eig_index[eig_index]])*numpy.abs(leftEigVector[k][critical_eig_index[eig_index]]))

        #Find the index largest PF and its associated critical buses
        NormalizedParticipationFactor = numpy.true_divide(ParticipationFactor,max(ParticipationFactor))
        indexNormalizedParticipationFactor = sorted(range(len(NormalizedParticipationFactor)), key=lambda x: NormalizedParticipationFactor[x])
        indexofLargestNormalizedParticipationFactor = indexNormalizedParticipationFactor[::-1]
        indexofLargestNormalizedParticipationFactor = indexofLargestNormalizedParticipationFactor[0:CriticalBusesNumber]

        #Find the largest PF and its associated critical buses
        LargestNormalizedParticipationFactor = [NormalizedParticipationFactor[i] for i in indexofLargestNormalizedParticipationFactor]
        LargestParticipationFactor = [ParticipationFactor[i] for i in indexofLargestNormalizedParticipationFactor]
        CriticalBus = [pq[i] for i in indexofLargestNormalizedParticipationFactor]

        #save the critical buses, largest normalized PF, largest PF to each critical mode
        CriticalBus_CriticalMode.append(CriticalBus)
        LargestNormalizedParticipationFactor_CriticalMode.append(LargestNormalizedParticipationFactor)
        LargestParticipationFactor_CriticalMode.append(LargestParticipationFactor)
        indexofLargestNormalizedParticipationFactor_CriticalMode.append(indexofLargestNormalizedParticipationFactor)

    return CriticalBus_CriticalMode,LargestNormalizedParticipationFactor_CriticalMode,LargestParticipationFactor_CriticalMode,indexofLargestNormalizedParticipationFactor_CriticalMode,critical_eig, critical_eig_index, eigenvalue

def inverseMatrix(Matrix):
    try:
        inverse = numpy.linalg.inv(Matrix)
    except numpy.linalg.LinAlgError:
        # Not invertible. Skip this one.
        pass
    else:
        return inverse

def most_common_N_items(lst,N):
    lst = CriticalBus_InsecuredOC                    
    freq = zerolistmaker(npq)
    for item_index in range(0,npq):
       item = pq[item_index]
       freq[item_index] = lst.count(item)
    freq_ind = heapq.nlargest(N, range(len(freq)), freq.__getitem__)
    mostcomm_freq = [freq[i] for i in freq_ind]
    mostcomm_bus = [pq[i] for i in freq_ind]
    return mostcomm_freq,mostcomm_bus


def control_cap(combination):
    for m in range(0,len(combination)):
        if combination[0] == 1:
            psspy.shunt_data(117,r""" 1""",1,[_f, 345])
        if combination[1] == 1:
            psspy.shunt_data(120,r""" 1""",1,[_f, 65])  # this bus cannot be higher than 65
        if combination[2] == 1:
            psspy.shunt_data(154,r""" 1""",1,[_f, 54.5]) # this bus cannot be higher than 65
        if combination[3] == 1:
            psspy.shunt_data(173,r""" 1""",1,[_f, 63]) # this bus cannot be higher than 64
        if combination[4] == 1:
            psspy.shunt_data(179,r""" 1""",1,[_f, 55]) # this bus cannot be higher than 65
        if combination[5] == 1:
            psspy.shunt_data(248,r""" 1""",1,[_f, 55.6]) # this bus cannot be to high

def writefiletotxt(filepath,filename):
    import pickle
    with open(filepath, 'wb') as f:
        pickle.dump(filename, f)

def readfilefromtxt(filepath):
    import pickle
    with open(filepath, 'rb') as f:
        filename = pickle.load(f)
    return filename

def getbus(region):
    region = region - 1
    psspy.bsys(0,0,[ 0.6, 345.],1,[region],0,[],0,[],0,[])
    ierr,busInterestedRegion = psspy.abusint(1,1,['number'])
    busInterestedRegion = busInterestedRegion[0]
    return busInterestedRegion

def clusterCriticalBuses(CriticalBus_InsecuredOC,similarityThreshold):
    ## Group the buses as one cluster if they are similar
    cluster_index = 0
    resulting_cluster = []
    basecase_index = 0
    resulting_cluster_allOC = []
    basecase_index_allOC = []
    index_OC_similartobasecase_allOC = []
    length_index_OC_similartobasecase_allOC = []
    for cluster_index in range(0,1000):
        #check if all operating conditions are murged
        if len(numpy.nonzero(CriticalBus_InsecuredOC)[0]) == 0:
            break
        else:
            # find the next non-empty/non-zero operating conidtion
            basecase_index = numpy.nonzero(CriticalBus_InsecuredOC)[0][0]
            basecase = CriticalBus_InsecuredOC[basecase_index]
            
            # empty the basecase
            CriticalBus_InsecuredOC[basecase_index] = []

            # find the clusters with bus similar to the basecase
            similarity_allOC = []
            OC_similartobasecase = []
            index_OC_similartobasecase = []
            for OC in range(0,len(CriticalBus_InsecuredOC)):
                sm=difflib.SequenceMatcher(None,basecase,CriticalBus_InsecuredOC[OC])
                similarity = sm.ratio()
                similarity_allOC.append(similarity)
                if similarity >= similarityThreshold:
                    OC_similartobasecase.append(CriticalBus_InsecuredOC[OC])
                    index_OC_similartobasecase.append(OC)
                    
            # initialize the first resulting cluster
            if len(OC_similartobasecase) == 0:
                resulting_cluster = basecase
            else:
                resulting_cluster = basecase + [i for i in OC_similartobasecase[0] if i not in basecase]

            # compute the resulting cluster for the rest of the cases
            for OC_similar in range(0,len(OC_similartobasecase)):
                
                #compute the resulting cluster
                resulting_cluster = resulting_cluster + [i for i in OC_similartobasecase[OC_similar] if i not in resulting_cluster]
         
                #empty the similar OCs
                CriticalBus_InsecuredOC[index_OC_similartobasecase[OC_similar]] = []
                CriticalBus_InsecuredOC_nonSimilar = CriticalBus_InsecuredOC

            resulting_cluster_allOC.append(resulting_cluster)
            index_OC_similartobasecase_allOC.append(index_OC_similartobasecase)
            length_index_OC_similartobasecase_allOC.append(len(index_OC_similartobasecase))
            basecase_index_allOC.append(basecase_index)
        
    return resulting_cluster_allOC, length_index_OC_similartobasecase_allOC

def getMeasurements(response_buses):
    psspy.bsys(sid = 1,numbus = len(response_buses), buses = response_buses)
    ierr,bus_voltage = psspy.abusreal(1,1,['PU'])
    bus_voltage = bus_voltage[0]
    ierr,bus_angle = psspy.abusreal(1,1,['ANGLE'])
    bus_angle = bus_angle[0]
    return bus_voltage,bus_angle