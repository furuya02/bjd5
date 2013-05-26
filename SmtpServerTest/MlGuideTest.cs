using System;
using System.Collections.Generic;
using System.Linq;
using Bjd.ctrl;
using Bjd.log;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;
using BjdTest;
using Bjd;

namespace SmtpServerTest {
    [TestFixture]
    class MlGuideTest {
        private Ml _ml;
        private TsMailSave _tsMailSave;

        [SetUp]
        public void SetUp() {
            const string mlName = "1ban";
            var domainList = new List<string> { "example.com" };
            //var tsDir = new TsDir();
            var kernel = new Kernel();
            var logger = new Logger();
            var manageDir = TestUtil.GetTmpDir("TestDir");

            _tsMailSave = new TsMailSave(); //MailSaveのモックオブジェクト

            var memberList = new Dat(new[] { CtrlType.TextBox, CtrlType.TextBox, CtrlType.CheckBox, CtrlType.CheckBox, CtrlType.CheckBox, CtrlType.TextBox });
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER1", "user1@example.com", false, true, true, "")); //一般・読者・投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER2", "user2@example.com", false, true, false, "")); //一般・読者・×
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER3", "user3@example.com", false, false, true, "")); //一般・×・投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN", "admin@example.com", true, false, true, "123")); //管理者・×・投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN2", "admin2@example.com", true, true, true, "456")); //管理者・読者・投稿
            var docs = (from object o in Enum.GetValues(typeof(MlDocKind)) select "").ToList();
            const int maxSummary = 10;
            const int getMax = 10;
            const bool autoRegistration = true;
            const int titleKind = 5;
            var mlOption = new MlOption(maxSummary, getMax, autoRegistration, titleKind, docs, manageDir,
                                        memberList);

            _ml = new Ml(kernel, logger, _tsMailSave, mlOption, mlName, domainList);

        }

        [TearDown]
        public void TearDown() {
            _tsMailSave.Dispose();
            _ml.Remove();
        }

        [TestCase("user1")]//メンバからのリクエスト
        [TestCase("xxxx")]//メンバ外からのリクエスト(メンバ外からもguideは取得できる
        public void GuideTest(string user) {
            //    ドメインを追加
            const string domain = "@example.com";
            var from = user + domain;

            var mail = new TsMail(from, "1ban-ctl" + domain, "guide");
            _ml.Job(mail.MlEnvelope, mail.Mail);

            Assert.AreEqual(_tsMailSave.Count(), 1);
            var m = _tsMailSave.GetMail(0);
            //送信者
            Assert.AreEqual(m.GetHeader("from"), "1ban-admin" + domain);
            //件名
            Assert.AreEqual(m.GetHeader("subject"), "guide (1ban ML)");

        }

        [TestCase("user1","help (1ban ML)")]//メンバからのリクエスト(成功)
        //[TestCase("admin", "help (1ban ML)")]//管理者からのリクエスト(成功)
        [TestCase("xxxx", "You are not member (1ban ML)")]//メンバ外からのリクエスト(失敗)
        public void HelpTest(string user,string subject) {
            //    ドメインを追加
            const string domain = "@example.com";
            string from = user + domain;

            var mail = new TsMail(from, "1ban-ctl" + domain, "help");
            _ml.Job(mail.MlEnvelope, mail.Mail);

            Assert.AreEqual(_tsMailSave.Count(), 1);
            var m = _tsMailSave.GetMail(0);
            //送信者
            Assert.AreEqual(m.GetHeader("from"), "1ban-admin" + domain);
            //件名
            Assert.AreEqual(m.GetHeader("subject"),subject);

        }

    }

}
