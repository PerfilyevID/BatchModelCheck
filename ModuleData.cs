﻿using System.Windows;

namespace BatchModelCheck
{
    public static class ModuleData
    {
        public static bool AutoConfirmEnabled = false;
#if Revit2020
        public static string RevitVersion = "2020";
        public static Window RevitWindow { get; set; }
#endif
#if Revit2018
        public static string RevitVersion = "2018";
#endif
        public static System.IntPtr MainWindowHandle { get; set; }
        public static string Build = string.Format("built for Revit {0}", RevitVersion);
        public static string Version = "1.0.0.1b";
        public static string Date = "2020/09/30";
        public static string ModuleName = "Batch ModelChecker";
        public static bool ForceClose = false;
    }
}
