using System.IO;
using System.Reflection;
using static BatchModelCheck.Common.Collections;

namespace BatchModelCheck.Source
{
    public class Source
    {
        public string Value { get; }
        private static string AssemblyPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
        public Source(Icon icon)
        {
            switch (icon)
            {
                case Icon.OpenManager:
                    Value = Path.Combine(AssemblyPath, @"Source\icon_manager.png");
                    break;
                case Icon.Statistics:
                    Value = Path.Combine(AssemblyPath, @"Source\icon_browser.png");
                    break;
                case Icon.Preferences:
                    Value = Path.Combine(AssemblyPath, @"Source\icon_setup.png");
                    break;
            }
        }
    }
}
