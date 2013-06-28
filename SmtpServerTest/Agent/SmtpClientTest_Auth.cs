using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest{
    internal class SmtpClientTest_Auth : ILife{
        
        private TestServer _testServer;

        [SetUp]
        public void SetUp(){
            _testServer = new TestServer(TestServerType.Smtp, "SmtpServerTest\\Agent", "SmtpClientTest_Auth.ini");
        }
        [TearDown]
        public void TearDown(){
            _testServer.Dispose();
        }

        private SmtpClient CreateSmtpClient(InetKind inetKind){
            if (inetKind == InetKind.V4){
                return new SmtpClient(new Ip(IpKind.V4Localhost), 9025, 3, this);
            }
            return new SmtpClient(new Ip(IpKind.V6Localhost), 9025, 3, this);
        }

        [TestCase(InetKind.V4, SmtpClientAuthKind.Login)]
        [TestCase(InetKind.V4, SmtpClientAuthKind.CramMd5)]
        [TestCase(InetKind.V4, SmtpClientAuthKind.Plain)]
        public void 正常系(InetKind inetKind, SmtpClientAuthKind kind){
            //setUp
            var sut = CreateSmtpClient(inetKind);

            //exercise
            Assert.That(sut.Connect(), Is.EqualTo(true));
            Assert.That(sut.Helo(), Is.EqualTo(true));
            Assert.That(sut.Auth(kind, "user1", "user1"), Is.EqualTo(true));
            Assert.That(sut.Mail("1@1"), Is.EqualTo(true));
            Assert.That(sut.Rcpt("user1@example.com"), Is.EqualTo(true));
            Assert.That(sut.Data(new Mail()), Is.EqualTo(true));

            Assert.That(sut.Quit(), Is.EqualTo(true));

            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4)]
        public void 認証前にMAILコマンド(InetKind inetKind) {
            //setUp
            var sut = CreateSmtpClient(inetKind);

            //exercise
            Assert.That(sut.Connect(), Is.EqualTo(true));
            Assert.That(sut.Helo(), Is.EqualTo(true));
            Assert.That(sut.Mail("1@1"), Is.EqualTo(false));
            
            var expected = "530 Authentication required.\r\n";
            var actual =sut.GetLastError();
            Assert.That(actual,Is.EqualTo(expected));

            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4)]
        public void HELOの前にMAILコマンド(InetKind inetKind) {
            //setUp
            var sut = CreateSmtpClient(inetKind);

            //exercise
            Assert.That(sut.Connect(), Is.EqualTo(true));
            //Assert.That(sut.Helo(), Is.EqualTo(true));
            Assert.That(sut.Mail("1@1"), Is.EqualTo(false));

            var expected = "Mail() Status != Transaction";
            var actual = sut.GetLastError();
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            sut.Dispose();
        }


        public bool IsLife(){
            return true;
        }
    }
}
