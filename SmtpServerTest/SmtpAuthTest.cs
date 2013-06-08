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
        public void PLAINによる認証() {
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

        
        Login
        MD5

        その他のモードも試験する


    }
}
