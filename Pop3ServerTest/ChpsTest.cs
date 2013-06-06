using System;
using System.IO;
using Bjd.ctrl;
using Bjd.mail;
using Bjd.option;
using NUnit.Framework;
using Pop3Server;

namespace Pop3ServerTest {
    internal class ChpsTest{
        private Conf _conf;
        private MailBox _mailBox;

        [SetUp]
        public void SetUp(){
            var datUser = new Dat(new CtrlType[2] { CtrlType.TextBox, CtrlType.TextBox });
            datUser.Add(true, "user1\t3OuFXZzV8+iY6TC747UpCA==");
            datUser.Add(true, "user2\tNKfF4/Tw/WMhHZvTilAuJQ==");
            datUser.Add(true, "user3\tXXX");

            _conf = new Conf();
            _conf.Add("user", datUser);

            _mailBox = new MailBox(null, datUser, "c:\\tmp2\\bjd5\\Pop3Server\\mailbox");
        }

        [TearDown]
        public void TearDown(){
            try{
                Directory.Delete(_mailBox.Dir);
            }catch (Exception){
                Directory.Delete(_mailBox.Dir, true);
            }
        }

        [TestCase("user1", "123")]//user1のパスワードを123に変更する
        [TestCase("user3", "123")]//user3のパスワードを123に変更する
        public void Changeによるパスワード変更_成功(string user, string pass) {
            //setUp
            bool expected = true;
            //exercise
            var actual = Chps.Change(user, pass, _mailBox, _conf);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("user1", "123")]//user1のパスワードを123に変更する
        [TestCase("user3", "123")]//user3のパスワードを123に変更する
        public void Changeによるパスワード変更_変更確認(string user, string pass) {
            //setUp
            var expected = true;
            Chps.Change(user, pass, _mailBox,_conf);
            //exercise
            var actual = _mailBox.Auth(user, pass);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("user1", null)]//無効パスワードの指定は失敗する
        [TestCase("xxx", "123")]//無効ユーザのパスワード変更は失敗する
        [TestCase(null, "123")]//無効ユーザのパスワード変更は失敗する
        public void Changeによるパスワード変更_失敗(string user, string pass) {
            //setUp
            bool expected = false;

            //exercise
            var actual = Chps.Change(user, pass, _mailBox,_conf);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


    }
}
