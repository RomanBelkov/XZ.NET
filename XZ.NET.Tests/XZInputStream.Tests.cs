using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XZ.NET;

namespace XZ.NET.Tests
{
    [TestClass]
    public class XZInputStreamTests
    {
        [TestMethod]
        [TestCategory("XZ.NET")]
        public void Decompress()
        {
            Decompress_Template("A.xz", "A.pdf");
            Decompress_Template("B9.xz", "B.txt");
            Decompress_Template("B1.xz", "B.txt");
            Decompress_Template("C.xz", "C.bin");
        }

        public void Decompress_Template(string xzFileName, string originFileName)
        {
            byte[] decompDigest;
            byte[] originDigest;

            string xzFile = Path.Combine(TestSetup.SampleDir, xzFileName);
            string originFile = Path.Combine(TestSetup.SampleDir, originFileName);
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (FileStream compFs = new FileStream(xzFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XZInputStream xz = new XZInputStream(compFs))
                {
                    xz.CopyTo(decompMs);
                }
                decompMs.Position = 0;

                HashAlgorithm hash = SHA256.Create();
                decompDigest = hash.ComputeHash(decompMs);
            }

            using (FileStream originFs = new FileStream(originFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                HashAlgorithm hash = SHA256.Create();
                originDigest = hash.ComputeHash(originFs);
            }

            Assert.IsTrue(decompDigest.SequenceEqual(originDigest));
        }
    }
}
