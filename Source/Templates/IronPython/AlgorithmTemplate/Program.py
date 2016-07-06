import clr
import traceback

clr.AddReference('System.Windows.Forms')

from System import *
from System.Windows.Forms import *

try:
    Application.EnableVisualStyles()
    Application.SetCompatibleTextRenderingDefault(False)
    Application.Run()
except Exception:
    print traceback.format_exc()
