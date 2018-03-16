using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XZ.NET;

namespace XZ.NET.Tests
{
    [TestClass]
    public class XZOutputStreamTests
    {
        [TestMethod]
        [TestCategory("XZ.NET")]
        public void Compress()
        {
            Compress_Template("A.pdf", 9);
            Compress_Template("B.txt", XZOutputStream.DefaultPreset);
            Compress_Template("C.bin", 1);
        }

        public void Compress_Template(string sampleFileName, uint preset)
        {
            string tempDecompFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string tempXzFile = tempDecompFile + ".xz";
            try
            {
                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);
                using (FileStream xzCompFs = new FileStream(tempXzFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XZOutputStream xz = new XZOutputStream(xzCompFs, Environment.ProcessorCount, preset, true))
                {
                    sampleFs.CopyTo(xz);
                }

                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = Path.Combine(TestSetup.SampleDir, "xz.exe"),
                        Arguments = $"-k -d {tempXzFile}",
                    }
                };
                proc.Start();
                proc.WaitForExit();
                Assert.IsTrue(proc.ExitCode == 0);

                byte[] decompDigest;
                byte[] originDigest;
                using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    HashAlgorithm hash = SHA256.Create();
                    originDigest = hash.ComputeHash(fs);
                }

                using (FileStream fs = new FileStream(tempDecompFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    HashAlgorithm hash = SHA256.Create();
                    decompDigest = hash.ComputeHash(fs);
                }

                Assert.IsTrue(originDigest.SequenceEqual(decompDigest));

            }
            finally
            {
                if (File.Exists(tempXzFile))
                    File.Delete(tempXzFile);
                if (File.Exists(tempDecompFile))
                    File.Delete(tempDecompFile);
            }
        }
    }
}
