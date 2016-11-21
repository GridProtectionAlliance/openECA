#region [ Environmental Setup ]
from __future__ import with_statement
from __future__ import division
from contextlib import contextmanager
import os,sys,glob,csv,pdb,pprint

PSSE_LOCATION_34 = r"""C:\Program Files (x86)\PTI\PSSE34\PSSPY27"""
PSSE_LOCATION_33 = r"""C:\Program Files (x86)\PTI\PSSE33\PSSBIN"""
if os.path.isdir(PSSE_LOCATION_34):
    sys.path.append(PSSE_LOCATION_34)
    import psse34, psspy
    
else:
    os.environ['PATH'] = PSSE_LOCATION_33 + ';' + os.environ['PATH']
    sys.path.append(PSSE_LOCATION_33)
    import psspy
    
MyLibraryLocation = r"""C:\Users\niezj\Desktop\16a_Fall\openECA_proj\LocalVoltageControl20161110\Test1\pythonCode\MyLibrary"""
sys.path.append(MyLibraryLocation)

from psspy import _i,_f # importing the default integer and float values used by PSS\E(every API uses them)
import PowerSystemPsseLibrary as powerlib
import redirect,StringIO
redirect.psse2py()
psspy.psseinit(80000)


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



# Read the C# information
inputDataFolder = sys.argv[1]
logsDataFolder = sys.argv[2]
caseName = sys.argv[-2]
testCaseName = sys.argv[-1]

# copy benchmark case as the test case
psspy.case(caseName)
savecase = (testCaseName)
psspy.save(savecase)



os.chdir(inputDataFolder)
for file in glob.glob("2016*.xml"):
        os.remove(file)

os.chdir(logsDataFolder)
for file in glob.glob("2016*.xml"):
        os.remove(file)

# delete all the transformers' input data files starts from the THIRD row
for i in range(0,2):
    k = i + 1
    InputTransformerFile = (inputDataFolder +"\\transformer" + str(k) + ".csv")
    ReadFile = open(InputTransformerFile)
    r = csv.reader(ReadFile)
    lines = [line for line in r]
    ReadFile.close()
    os.remove(InputTransformerFile)

    WriteFile = open(InputTransformerFile,'a')
    w = csv.writer(WriteFile,delimiter = ',',lineterminator = '\n')
    for rownumber in range(0,2):
        w.writerow(lines[rownumber])
    WriteFile.close()

# delete all the capbank' input data files starts from the THIRD row
for i in range(0,2):
    k = i + 1
    InputCapbankFile = (inputDataFolder +"\\CapBank" + str(k) + ".csv")
    ReadFile = open(InputCapbankFile)
    r = csv.reader(ReadFile)
    lines = [line for line in r]
    ReadFile.close()
    os.remove(InputCapbankFile)

    WriteFile = open(InputCapbankFile,'a')
    w = csv.writer(WriteFile,delimiter = ',',lineterminator = '\n')
    for rownumber in range(0,2):
        w.writerow(lines[rownumber])
    WriteFile.close()

# delete all the subInformation s' input data files starts from the THIRD row
InputInformationFile = (inputDataFolder +"\\SubInformation.csv")
ReadFile = open(InputInformationFile)
r = csv.reader(ReadFile)
lines = [line for line in r]
ReadFile.close()
os.remove(InputInformationFile)

WriteFile = open(InputInformationFile,'a')
w = csv.writer(WriteFile,delimiter = ',',lineterminator = '\n')
for rownumber in range(0,2):
    w.writerow(lines[rownumber])
WriteFile.close()

# remove the pfDifference
os.remove(inputDataFolder + "\\pfDifference.csv")

# remove the TxRatio Files
os.remove(inputDataFolder +"\\transformerRatio" + ".csv")
