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
    internal class SmtpClientTest : ILife{

        [SetUp]
        public void SetUp(){
        }
        [TearDown]
        public void TearDown(){
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
            var testServe = new TestServer(TestServerType.Smtp, "SmtpClientTest.ini");
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
            testServe.Dispose();
        }


        public bool IsLife(){
            return true;
        }
    }
}
