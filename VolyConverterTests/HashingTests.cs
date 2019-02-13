using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VolyConverter;

namespace VolyConverterTests
{
    [TestClass]
    public class HashingTests
    {
        [TestMethod]
        public void TestMd5()
        {
            ConductTest(Hashing.HashFileMd5,
                "D41D8CD98F00B204E9800998ECF8427E",
                "6CD3556DEB0DA54BCA060B4C39479839",
                "9ABDB84F6DDF3093E24ABCDDE5756F0F");
        }

        [TestMethod]
        public void TestSha1()
        {
            ConductTest(Hashing.HashFileSha1,
                "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709",
                "943A702D06F34599AEE1F8DA8EF9F7296031D699",
                "1BCA2B65D3D4835EA4A8236C6A600A0986EEA697");
        }

        [TestMethod]
        public void TestSha256()
        {
            ConductTest(Hashing.HashFileSha256,
                "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
                "315F5BDB76D078C43B8AC0064E4A0164612B1FCE77C869345BFC94C75894EDD3",
                "E09320C5B00B34BB704802136C599A95B3996332BA84D7C7F21112B6231B6BD0");
        }

        private void ConductTest(HashMethod method, string emptyHash, string helloHash, string longfileHash)
        {
            //Assert.ThrowsException<ArgumentException>(() => { method.Invoke(""); });
            //Assert.ThrowsException<FileNotFoundException>(() => { method.Invoke("notarealfile.notreal.donotmake"); });

            const string testPath = "hashtestfile.test";

            File.Create(testPath).Close();
            Assert.AreEqual(emptyHash, method.Invoke(testPath));

            File.WriteAllText(testPath, "Hello, world!");
            Assert.AreEqual(helloHash, method.Invoke(testPath));

            try
            {
                using (Stream s = File.OpenWrite(testPath))
                {
                    long size = 1024 * 1024 * 512;
                    for (long i = 0; i < size; i++)
                    {
                        s.WriteByte((byte)i);
                    }
                }
                Assert.AreEqual(longfileHash, method.Invoke(testPath));
            }
            finally
            {
                File.Delete(testPath);
            }
        }
    }
}
