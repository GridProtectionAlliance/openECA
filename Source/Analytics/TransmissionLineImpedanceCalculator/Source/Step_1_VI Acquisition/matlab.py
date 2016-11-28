import ctypes as ct

# Powershell order: ps | where {$_.ProcessName -eq 'Matlab'}
matlabpid = 6388

PROCESS_VM_READ = 0X10

hMatlab = ct.windll.kernel32.OpenProcess(PROCESS_VM_READ, True, matlabpid)

m = 1800#108001
n = 2

buf = (ct.c_double * m * n)()

ct.windll.kernel32.ReadProcessMemory(hMatlab, 0x1bc475e0, buf, m * n * ct.sizeof(ct.c_double), None) # 0x1bc475e0 for 1800 samples; 0x1ac80060 for 108001 samples

ct.windll.kernel32.CloseHandle(hMatlab)

peak_load_alter_percent=list()

for i in range(1800):
    peak_load_alter_percent.append(buf[0][i])

valley_load_alter_percent = buf[1]

print peak_load_alter_percent
print len(peak_load_alter_percent)