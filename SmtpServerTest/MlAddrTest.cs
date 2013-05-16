using System.Collections.Generic;
using Bjd.mail;
using NUnit.Framework;
using SmtpServer;
using Bjd;

namespace SmtpServerTest {
    [TestFixture]
    class MlAddrTest {

        MlAddr _mlAddr;//テスト対象クラス

        [SetUp]
        public void SetUp(){
            _mlAddr = new MlAddr("1ban", new List<string>{ "example.com" });
        }
        [TearDown]
        public void TearDown() {
        
        }

        [TestCase("1ban-admin@example.com",MlAddrKind.Admin)]
        [TestCase("1ban-ctl@example.com", MlAddrKind.Ctrl)]
        [TestCase("1ban@example.com", MlAddrKind.Post)]
        public void MlAddressTest(string mailAddress, MlAddrKind mlAddrKind) {
            switch (mlAddrKind) {
                case MlAddrKind.Admin:
                    Assert.AreEqual(mailAddress,_mlAddr.Admin.ToString());
                    break;
                case MlAddrKind.Ctrl:
                    Assert.AreEqual(mailAddress, _mlAddr.Ctrl.ToString());
                    break;
                case MlAddrKind.Post:
                    Assert.AreEqual(mailAddress, _mlAddr.Post.ToString());
                    break;
            }
        }
        
        
        [TestCase("1ban-admin@example.com",MlAddrKind.Admin)]
        [TestCase("1ban-ctl@example.com", MlAddrKind.Ctrl)]
        [TestCase("1ban@example.com", MlAddrKind.Post)]
        [TestCase("1@1", MlAddrKind.None)]
        [TestCase("admin@example.com", MlAddrKind.None)]
        [TestCase("ctl-1ban@example.com", MlAddrKind.None)]
        public void GetKindTest(string mailAddress, MlAddrKind kind) {
            Assert.AreEqual(_mlAddr.GetKind(new MailAddress(mailAddress)),kind);
        }

        [TestCase("1ban-admin@example.com", true)]
        [TestCase("1ban-ctl@example.com", true)]
        [TestCase("admin@example.com", false)]
        [TestCase("ctl-1ban@example.com", false)]
        [TestCase("1ban@example.com", true)]
        [TestCase("1@1", false)]
        public void IsUserTest(string mailAddress, bool isUser) {
            Assert.AreEqual(_mlAddr.IsUser(new MailAddress(mailAddress)),isUser);
        }
    }
}
