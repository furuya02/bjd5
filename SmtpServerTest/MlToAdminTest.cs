using System;
using System.Collections.Generic;
using System.Linq;
using Bjd.ctrl;
using Bjd.log;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;
using Bjd;
using BjdTest;

namespace SmtpServerTest {
    [TestFixture]
    internal class MlToAdminTest{
        private Ml _ml;
        private TsMailSave _tsMailSave;

        [SetUp]
        public void SetUp(){
            const string mlName = "1ban";
            var domainList = new List<string>{"example.com"};
            //var tsDir = new TsDir();
            var kernel = new Kernel(null, null, null, null);
            var logger = new Logger();
            var manageDir = TestUtil.GetTmpDir("TestDir");

            _tsMailSave = new TsMailSave(); //MailSaveのモックオブジェクト

            var memberList = new Dat(new[] { CtrlType.TextBox, CtrlType.TextBox, CtrlType.CheckBox, CtrlType.CheckBox, CtrlType.CheckBox, CtrlType.TextBox });
            memberList.Add(true,string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER1", "user1@example.com", false, true, true,"")); //一般・読者・投稿
            memberList.Add(true,string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER2", "user2@example.com", false, true,false, "")); //一般・読者・×
            memberList.Add(true,string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER3", "user3@example.com", false, false,true, "")); //一般・×・投稿
            memberList.Add(true,string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN", "admin@example.com", true, false, true,"123")); //管理者・×・投稿
            memberList.Add(true,string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN2", "admin2@example.com", true, true,true, "456")); //管理者・読者・投稿
            var docs = (from object o in Enum.GetValues(typeof (MlDocKind)) select "").ToList();
            const int maxSummary = 10;
            const int getMax = 10;
            const bool autoRegistration = true;
            const int titleKind = 5;
            var mlOption = new MlOption(maxSummary, getMax, autoRegistration, titleKind, docs, manageDir,
                                        memberList);

            _ml = new Ml(kernel, logger, _tsMailSave, mlOption, mlName, domainList);

        }

        [TearDown]
        public void TearDown(){
            _tsMailSave.Dispose();
            _ml.Remove();
        }

        [TestCase("user1@example.com")]//メンバから
        [TestCase("xxx@example.com")]//メンバ外から
        [TestCase("admin@example.com")]
        [TestCase("admin2@example.com")]
        public void AdminTest(string from) {

            var mail = new TsMail(from, "1ban-admin@example.com","DMY");
            _ml.Job(mail.MlEnvelope, mail.Mail);

            //管理者全員にメールが配信される
            Assert.AreEqual(_tsMailSave.Count(), 2);
            //送信者の確認
            Assert.AreEqual(_tsMailSave.GetMail(0).GetHeader("from"),from);
        }

    }
}
