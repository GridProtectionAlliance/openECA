﻿using System.Reflection;
using System.Runtime.InteropServices;

// Assembly identity attributes.
[assembly: AssemblyVersion("1.4.290.0")]

// Informational attributes.
[assembly: AssemblyCompany("Grid Protection Alliance")]
[assembly: AssemblyCopyright("Copyright © 2015.  All Rights Reserved.")]
[assembly: AssemblyProduct("openECA")]

// Assembly manifest attributes.
#if DEBUG
[assembly: AssemblyConfiguration("Debug Build")]
#else
[assembly: AssemblyConfiguration("Release Build")]
#endif
[assembly: AssemblyDescription("Web services used by openECA.")]
[assembly: AssemblyTitle("openECA Web Services")]

// Other configuration attributes.
[assembly: ComVisible(false)]
[assembly: Guid("08996eaa-cea3-492f-b3d4-12af2277f17c")]
