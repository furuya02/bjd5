using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    
    [TestFixture]
    class Md5Test {
        [TestCase("password", "solt", "f6a4e260a28ece018b556fb5336d0e34")]
        [TestCase("", "", "74e6f7298a9c2d168935f58c001bad88")]
        [TestCase("", "###", "59b38243e644af8be7cb910cb8739608")]
        [TestCase("$$$", "", "363408996ea1e1c3fcd88a88ae639f7c")]
        public void HashStrTest(string passStr, string timestampStr, string hashStr) {
            var s = Md5.Hash(passStr,timestampStr);
            Assert.AreEqual(s,hashStr);
        }
    }
}
