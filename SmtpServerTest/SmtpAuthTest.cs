using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.ctrl;
using Bjd.option;
using Bjd.util;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class SmtpAuthTest{
        private SmtpAuthUserList _smtpAuthUserList;
        [SetUp]
        public void SetUp(){
            var esmtpUserList = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox });
            esmtpUserList.Add(true, "user1\t3OuFXZzV8+iY6TC747UpCA==");
            _smtpAuthUserList = new SmtpAuthUserList(false, null, esmtpUserList);
        }

        [Test]
        public void 認証前にIsFinishはfalseがセットされる(){
            //setUp
            const bool usePlain = true;
            const bool useLogin = true;
            const bool useCramMd5 = true;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);
            var expected = false;

            //exercise
            var actual = sut.IsFinish;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 認証前でも有効なモードが無いときIsFinishはtrueがセットされる() {
            //setUp
            const bool usePlain = false;
            const bool useLogin = false;
            const bool useCramMd5 = false;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);
            var expected = true;
            
            //exercise
            var actual = sut.IsFinish;

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 無効なAUTHコマンド() {
            //setUp
            const bool usePlain = true;
            const bool useLogin = true;
            const bool useCramMd5 = true;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);

            var expected = "504 Unrecognized authentication type.";

            //exercise
            var actual = sut.Job("AUTH XXXX");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void 無効なAUTHコマンド2() {
            //setUp
            const bool usePlain = false; //無効になっている
            const bool useLogin = true;
            const bool useCramMd5 = true;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);

            var expected = "500 command not understood: AUTH PLAIN\r\n";
            //exercise
            var actual = sut.Job("AUTH PLAIN");//本来は、正しいコマンドだが

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void PLAINによる認証_成功() {
            //setUp
            const bool usePlain = true;
            const bool useLogin = false;
            const bool useCramMd5 = false;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);
            Assert.That(sut.Job("AUTH PLAIN"), Is.EqualTo("334 "));
            Assert.That(sut.Job(Base64.Encode("user1\0user1\0user1")), Is.EqualTo("235 Authentication successful."));
            var expected = true;

            //exercise
            var actual = sut.IsFinish;

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }




        [Test]
        public void PLAINによる認証_失敗() {
            //setUp
            const bool usePlain = true;
            const bool useLogin = false;
            const bool useCramMd5 = false;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);
            Assert.That(sut.Job("AUTH PLAIN"), Is.EqualTo("334 "));
            String expected = null;
            
            //exercise
            var actual = sut.Job(Base64.Encode("user1\0user1\0user2"));

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void LOGINによる認証_成功() {
            //setUp
            const bool usePlain = false;
            const bool useLogin = true;
            const bool useCramMd5 = false;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);

            Assert.That(sut.Job("AUTH LOGIN"), Is.EqualTo("334 VXNlcm5hbWU6"));
            Assert.That(sut.Job(Base64.Encode("user1")), Is.EqualTo("334 UGFzc3dvcmQ6"));
            Assert.That(sut.Job(Base64.Encode("user1")), Is.EqualTo("235 Authentication successful."));
            
            var expected = true;

            //exercise
            var actual = sut.IsFinish;

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void LOGINによる認証_失敗() {
            //setUp
            const bool usePlain = false;
            const bool useLogin = true;
            const bool useCramMd5 = false;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);

            Assert.That(sut.Job("AUTH LOGIN"), Is.EqualTo("334 VXNlcm5hbWU6"));
            Assert.That(sut.Job(Base64.Encode("user1")), Is.EqualTo("334 UGFzc3dvcmQ6"));

            String expected = null;
            //exercise
            var actual = sut.Job(Base64.Encode("xxx"));

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void CRAM_MD5による認証_成功() {
            //setUp
            const bool usePlain = false;
            const bool useLogin = false;
            const bool useCramMd5 = true;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);

            var str = sut.Job("AUTH CRAM-MD5");

            var hash = Md5.Hash("user1", Base64.Decode(str.Substring(4)));
            Assert.That(sut.Job(Base64.Encode(string.Format("user1 {0}", hash))),Is.EqualTo("235 Authentication successful."));
            var expected = true;

            //exercise
            var actual = sut.IsFinish;

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void CRAM_MD5による認証_失敗() {
            //setUp
            const bool usePlain = false;
            const bool useLogin = false;
            const bool useCramMd5 = true;
            var sut = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);

            var str = sut.Job("AUTH CRAM-MD5");

            var hash = Md5.Hash("user2", Base64.Decode(str.Substring(4)));
            var expected = (String)null;

            //exercise
            var actual = sut.Job(Base64.Encode(string.Format("user1 {0}", hash)));

            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

    }
}
