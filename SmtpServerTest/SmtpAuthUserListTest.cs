using System;
using System.IO;
using Bjd.ctrl;
using Bjd.mail;
using Bjd.option;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class SmtpAuthUserListTest {
        private MailBox _mailBox;
        private Dat _esmtpUserList;
        [SetUp]
        public void SetUp() {
            //mailBoxに"user1"を登録
            var datUser = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox });
            datUser.Add(true, "user1\t3OuFXZzV8+iY6TC747UpCA==");
            _mailBox = new MailBox(null, datUser, "c:\\tmp2\\bjd5\\SmtpServerTest\\mailbox");
            //esmtpUserListに"user2"を登録
            _esmtpUserList = new Dat(new CtrlType[]{CtrlType.TextBox, CtrlType.TextBox });
            _esmtpUserList.Add(true, "user2\tNKfF4/Tw/WMhHZvTilAuJQ==");
        }
        [TearDown]
        public void TearDown() {
            try {
                Directory.Delete(_mailBox.Dir);
            } catch (Exception) {
                Directory.Delete(_mailBox.Dir, true);
            }
        }


        [TestCase("user1","user1",true)] //mailBoxのユーザは成功
        [TestCase("user2", "user2", false)] //esmtpUserListのユーザは失敗
        [TestCase("user1", "user2",false)]//mailBoxのユーザのパスワード間違い
        public void MailBoxが有効な場合の認証(String user, String pass, bool expected) {
            //setUp
            var sut = new SmtpAuthUserList(true,_mailBox, _esmtpUserList);
            //exercise
            var actual = sut.Auth(user,pass);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
        [TestCase("user1", "user1", false)]//mailBoxのユーザは失敗
        [TestCase("user2", "user2", true)]//esmtpUserListのユーザは成功
        [TestCase("user2", "user1", false)]//esmtpUserListのユーザのパスワード間違い
        public void EsmtpUserListが有効な場合の認証(String user, String pass, bool expected) {
            //setUp
            var sut = new SmtpAuthUserList(false, _mailBox, _esmtpUserList);
            //exercise
            var actual = sut.Auth(user, pass);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
        [TestCase("user1", "user1", true)]//mailBoxのユーザ成功する
        [TestCase("user2", "user2", false)] //mailBoxが有効な場合、_esmtpUserListは無効になる
        [TestCase("user1", "user2", false)]//mailBoxのユーザのパスワード間違い
        public void 両方有効な場合の認証(String user, String pass, bool expected) {
            //setUp
            var sut = new SmtpAuthUserList(true, _mailBox, _esmtpUserList);
            //exercise
            var actual = sut.Auth(user, pass);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
        [TestCase("user1", "user1")]//mailBoxのユーザは成功する
        [TestCase("user2", null)]//esmtpUserListのユーザは失敗する
        public void GetPass_MailBoxが有効な場合(String user, String pass) {
            //setUp
            var sut = new SmtpAuthUserList(true, _mailBox, _esmtpUserList);
            var expected = pass;
            //exercise
            var actual = sut.GetPass(user);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [TestCase("user1", null)]//mailBoxのユーザは失敗する
        [TestCase("user2", "user2")]//esmtpUserListのユーザは成功する
        public void GetPass_esmtpUserListが有効な場合(String user, String pass) {
            //setUp
            var sut = new SmtpAuthUserList(false, _mailBox, _esmtpUserList);
            var expected = pass;
            //exercise
            var actual = sut.GetPass(user);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
        [TestCase("user1", "user1")]//mailBoxのユーザは成功する
        [TestCase("user2", null)] //mailBoxが有効な場合esmtpUserListは無効になる
        public void GetPass_両方有効な場合(String user, String pass) {
            //setUp
            var sut = new SmtpAuthUserList(true, _mailBox, _esmtpUserList);
            var expected = pass;
            //exercise
            var actual = sut.GetPass(user);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
        
    }
}
