using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XZ.NET.Tests
{
    [TestClass]
    public class TestSetup
    {
        public static string BaseDir;
        public static string SampleDir;

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            BaseDir = Path.GetFullPath(Path.Combine(TestHelper.GetProgramAbsolutePath(), "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");

            /*
            switch (IntPtr.Size)
            {
                case 8:
                    Wim.GlobalInit(Path.Combine("x64", "libwim-15.dll"));
                    break;
                case 4:
                    Wim.GlobalInit(Path.Combine("x86", "libwim-15.dll"));
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }
            */
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            // Wim.GlobalCleanup();
        }
    }

    public class TestHelper
    {
        public static string GetProgramAbsolutePath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (Path.GetDirectoryName(path) != null)
                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return path;
        }
    }
}
