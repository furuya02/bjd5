using System;
using System.Collections.Generic;
using System.IO;
using Bjd.ctrl;
using Bjd.log;
using Bjd.mail;
using Bjd.option;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    [TestFixture]
    class AliasTest{
        private MailBox _mailBox;
        private List<String> _domainList;

        [SetUp]
        public void SetUp(){
            _domainList = new List<string>();
            _domainList.Add("example.com");

            
            var datUser = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox });
            datUser.Add(true, "user1\t3OuFXZzV8+iY6TC747UpCA==");
            datUser.Add(true, "user2\tNKfF4/Tw/WMhHZvTilAuJQ==");
            datUser.Add(true, "user3\tjNBu6GHNV633O4jMz1GJiQ==");
            _mailBox = new MailBox(new Logger(), datUser, "c:\\tmp2\\bjd5\\SmtpServerTest\\mailbox");
            
        }
        [TearDown]
        public void TearDown(){
            try{
                Directory.Delete(_mailBox.Dir);
            } catch (Exception){
                Directory.Delete(_mailBox.Dir, true);
            }
        }

        [Test]
        public void Reflectionによる宛先の変換_ヒットあり() {
            //setUp
            var sut = new Alias(_domainList,_mailBox);
            sut.Add("user1", "user2,user3", new Logger());
            
            var rcptList = new List<MailAddress>();
            rcptList.Add(new MailAddress("user1@example.com"));

            //exercise
            var actual = sut.Reflection(rcptList,new Logger());
            //verify
            Assert.That(actual.Count,Is.EqualTo(2));
            Assert.That(actual[0].ToString(), Is.EqualTo("user2@example.com"));
            Assert.That(actual[1].ToString(), Is.EqualTo("user3@example.com"));

        }
        [Test]
        public void Reflectionによる宛先の変換_ヒットなし() {
            //setUp
            var sut = new Alias(_domainList, _mailBox);
            sut.Add("user1", "user2,user3", new Logger());

            var rcptList = new List<MailAddress>();
            rcptList.Add(new MailAddress("user2@example.com"));

            //exercise
            var actual = sut.Reflection(rcptList,new Logger());
            //verify
            Assert.That(actual.Count, Is.EqualTo(1));
            Assert.That(actual[0].ToString(), Is.EqualTo("user2@example.com"));

        }
        [Test]
        public void Reflectionによる宛先の変換_ALL() {
            //setUp
            var sut = new Alias(_domainList, _mailBox);
            sut.Add("user1", "$ALL", new Logger());

            var rcptList = new List<MailAddress>();
            rcptList.Add(new MailAddress("user1@example.com"));

            //exercise
            var actual = sut.Reflection(rcptList, new Logger());
            //verify
            Assert.That(actual.Count, Is.EqualTo(3));
            Assert.That(actual[0].ToString(), Is.EqualTo("user1@example.com"));
            Assert.That(actual[1].ToString(), Is.EqualTo("user2@example.com"));
            Assert.That(actual[2].ToString(), Is.EqualTo("user3@example.com"));

        }

        [Test]
        public void Reflectionによる宛先の変換_USER() {
            //setUp
            var sut = new Alias(_domainList, _mailBox);
            sut.Add("user1", "$USER,user2", new Logger());

            var rcptList = new List<MailAddress>();
            rcptList.Add(new MailAddress("user1@example.com"));

            //exercise
            var actual = sut.Reflection(rcptList, new Logger());
            //verify
            Assert.That(actual.Count, Is.EqualTo(2));
            Assert.That(actual[0].ToString(), Is.EqualTo("user1@example.com"));
            Assert.That(actual[1].ToString(), Is.EqualTo("user2@example.com"));

        }

        [Test]
        public void Reflectionによる宛先の変換_仮想ユーザ() {
            //setUp
            var sut = new Alias(_domainList, _mailBox);
            sut.Add("dmy", "user1,user2", new Logger());
            var rcptList = new List<MailAddress>();
            rcptList.Add(new MailAddress("dmy@example.com"));

            //exercise
            var actual = sut.Reflection(rcptList,new Logger());
            //verify
            Assert.That(actual.Count, Is.EqualTo(2));
            Assert.That(actual[0].ToString(), Is.EqualTo("user1@example.com"));
            Assert.That(actual[1].ToString(), Is.EqualTo("user2@example.com"));

        }

        [TestCase("dmy",true)]
        [TestCase("xxx", false)]
        [TestCase("user1", true)]
        [TestCase("user2", false)]
        public void IsUserによる登録ユーザの確認(String user, bool expected) {
            //setUp
            var sut = new Alias( _domainList, _mailBox);
            sut.Add("dmy","user1,user2",new Logger());
            sut.Add("user1", "user3,user4", new Logger());

            var rcptList = new List<MailAddress>();
            rcptList.Add(new MailAddress("dmy@example.com"));

            //exercise
            var actual = sut.IsUser(user);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

    }

}
