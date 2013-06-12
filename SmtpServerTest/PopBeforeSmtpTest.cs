using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd.ctrl;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class PopBeforeSmtpTest{
        private MailBox _mailBox; 
        [SetUp]
        public void SetUp(){
            var datUser = new Dat(new CtrlType[]{CtrlType.TextBox, CtrlType.TextBox});
            datUser.Add(true, "user1\t3OuFXZzV8+iY6TC747UpCA==");
            _mailBox = new MailBox(null, datUser, "c:\\tmp2\\bjd5\\SmtpServerTest\\mailbox");
        }
        [TearDown]
        public void TearDown(){
            try{
                Directory.Delete(_mailBox.Dir);
            } catch (Exception){
                Directory.Delete(_mailBox.Dir,true);
            }
        }

        [Test]
        public void 事前にログインが無い場合_許可されない(){
            //setUp
            var sut = new PopBeforeSmtp(true, 10, _mailBox);
            var expected = false;

            //exercise
            var actual = sut.Auth(new Ip("127.0.0.1"));
            //verify
            Assert.That(actual,Is.EqualTo(expected));
        }

        [Test]
        public void 事前にログインが有る場合_許可される() {
            //setUp
            var sut = new PopBeforeSmtp(true, 10, _mailBox);
            var ip = new Ip("192.168.0.1");
            var expected = true;

            _mailBox.Login("user1", ip);
            _mailBox.Logout("user1");

            //exercise
            var actual = sut.Auth(ip);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 事前にログインが有るが時間が経過してる場合_許可されない() {
            //setUp
            var sut = new PopBeforeSmtp(true, 1, _mailBox);//１秒以内にログインが必要
            var ip = new Ip("192.168.0.1");
            var expected = false;

            _mailBox.Login("user1", ip);
            _mailBox.Logout("user1");
            Thread.Sleep(1100);//ログアウトしてから１.1秒経過
            //exercise
            var actual = sut.Auth(ip);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
