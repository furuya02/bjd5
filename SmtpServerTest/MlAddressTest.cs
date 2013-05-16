using System.Collections.Generic;
using NUnit.Framework;
using SmtpServer;
using Bjd;

namespace SmtpServerTest {
    [TestFixture]
    class MlAddressTest {

        string name = "1ban";
        List<string> domainList = new List<string>() { "example.com" };
        MlAddr mlAddr;//テスト対象クラス

        [SetUp]
        public void SetUp(){
            mlAddr = new MlAddr(name, domainList);
        }
        [TearDown]
        public void TearDown() {
        }

        [TestCase("1ban-admin@example.com",MlAddrKind.Admin)]
        [TestCase("1ban-ctl@example.com", MlAddrKind.Ctrl)]
        [TestCase("1ban@example.com", MlAddrKind.Post)]
        public void MlAddress_Test(string mailAddress, MlAddrKind mlAddrKind) {
            switch (mlAddrKind) {
                case MlAddrKind.Admin:
                    Assert.AreEqual(mailAddress,mlAddr.Admin.ToString());
                    break;
                case MlAddrKind.Ctrl:
                    Assert.AreEqual(mailAddress, mlAddr.Ctrl.ToString());
                    break;
                case MlAddrKind.Post:
                    Assert.AreEqual(mailAddress, mlAddr.Post.ToString());
                    break;
            }
        }
        
        
        [TestCase("1ban-admin@example.com",MlAddrKind.Admin)]
        [TestCase("1ban-ctl@example.com", MlAddrKind.Ctrl)]
        [TestCase("1ban@example.com", MlAddrKind.Post)]
        [TestCase("1@1", MlAddrKind.None)]
        [TestCase("admin@example.com", MlAddrKind.None)]
        [TestCase("ctl-1ban@example.com", MlAddrKind.None)]
        public void GetKind_Test(string mailAddress, MlAddrKind kind) {
            Assert.AreEqual(mlAddr.GetKind(new MailAddress(mailAddress)),kind);
        }

        [TestCase("1ban-admin@example.com", true)]
        [TestCase("1ban-ctl@example.com", true)]
        [TestCase("admin@example.com", false)]
        [TestCase("ctl-1ban@example.com", false)]
        [TestCase("1ban@example.com", true)]
        [TestCase("1@1", false)]
        public void IsUser_Test(string mailAddress, bool isUser) {
            Assert.AreEqual(mlAddr.IsUser(new MailAddress(mailAddress)),isUser);
        }
    }
}
