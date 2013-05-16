using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.ctrl;
using Bjd.log;
using Bjd.option;
using BjdTest.test;
using SmtpServer;
using NUnit.Framework;
using Bjd;
using BjdTest;

namespace SmtpServerTest {

    [TestFixture]
    class MlSummaryTest {
        private Ml _ml;
        private TsMailSave _tsMailSave;

        [SetUp]
        public void SetUp() {
            const string mlName = "1ban";
            var domainList = new List<string>{ "example.com" };
            //var tsDir = new TsDir();
            var kernel = new Kernel(null, null, null, null);
            var logger = new Logger();
            var manageDir = TestUtil.GetTmpDir("TestDir");

            _tsMailSave = new TsMailSave();//MailSaveのモックオブジェクト

            var memberList = new Dat(new[] { CtrlType.TextBox, CtrlType.TextBox, CtrlType.CheckBox, CtrlType.CheckBox, CtrlType.CheckBox, CtrlType.TextBox });
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER1", "user1@example.com", false, true, true, "")); //一般・読者・投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER2", "user2@example.com", false, true, false, ""));//一般・読者・×
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER3", "user3@example.com", false, false, true, ""));//一般・×・投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN", "admin@example.com", true, false, true, "123"));//管理者・×・投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN2", "admin2@example.com", true, true, true, "456"));//管理者・読者・投稿
            var docs = (from object o in Enum.GetValues(typeof(MlDocKind)) select "").ToList();
            const int maxSummary = 10;
            const int getMax = 10;
            const bool autoRegistration = true;
            const int titleKind = 5;
            var mlOption = new MlOption(maxSummary, getMax, autoRegistration, titleKind, docs, manageDir, memberList);

            _ml = new Ml(kernel, logger, _tsMailSave, mlOption, mlName, domainList);

        }

        [TearDown]
        public void TearDown() {
            _tsMailSave.Dispose();
            _ml.Remove();
        }
        
        //蓄積されていないメールをリクエスト
        [TestCase("summary 3", "ERROR \"SUMMARY 3\"")]//エラーのためメールは帰らない（エラーがログに出力されている）
        [TestCase("summary 5-10", "ERROR \"SUMMARY 5-10\"")]//エラーのためメールは帰らない（エラーがログに出力されている）
        [TestCase("summary", "not found No.1 (1ban ML)")]
        public void Summary0Test(string body, string subject) {
            //    ドメインを追加
            const string domain = "@example.com";
            const string from = "user1" + domain;

            var mail = new TsMail(from, "1ban-ctl" + domain, body);
            _ml.Job(mail.MlEnvelope, mail.Mail);

            Assert.AreEqual(_tsMailSave.Count(), 1); //返されるエラーメールは1通
            Assert.AreEqual(_tsMailSave.GetMail(0).GetHeader("subject"), subject);
            Assert.AreEqual(_tsMailSave.GetFrom(0).ToString(), "1ban-admin" + domain);

        }

        //３０通蓄積された状態
        [TestCase("summary", 3, "result for summary [21-30] (1ban ML)")]
        [TestCase("summary 3", 1, "result for summary [3-3] (1ban ML)")]
        [TestCase("summary 1-10",1, "result for summary [1-10] (1ban ML)")]
        [TestCase("summary 1-25",3, "result for summary [21-25] (1ban ML)")]
        [TestCase("summary 25-35", 1, "result for summary [25-30] (1ban ML)")]
        [TestCase("summary 45", 1, "ERROR \"SUMMARY 45\"")]
        [TestCase("summary 45-60", 1, "ERROR \"SUMMARY 45-60\"")]
        [TestCase("summary last:3", 1, "result for summary [28-30] (1ban ML)")]
        [TestCase("summary first:5", 1, "result for summary [1-5] (1ban ML)")]
        public void Summary1Test(string body, int count, string subject) {

            const string domain = "@example.com";
            const string from = "user1" + domain;

            for (var i = 0; i < 30; i++) {
                var m = new TsMail(from, "1ban" + domain, "DMY");
                m.Mail.AddHeader("subject", string.Format("TEST_{0}", i));//試験的に件名を挿入する

                _ml.Job(m.MlEnvelope, m.Mail);
            }
            //この時点で、user1,user2,adin2のそれぞれ30通が送信されているため_tsMailSave.Count()は90となる
            //事後のテストのため一度クリアする
            _tsMailSave.Clear();

            
            var mail = new TsMail(from,"1ban-ctl"+domain,body);
            _ml.Job(mail.MlEnvelope, mail.Mail);

            Assert.AreEqual(_tsMailSave.Count(), count); //返されるエラーメールは1通
            Assert.AreEqual(_tsMailSave.GetMail(count - 1).GetHeader("subject"), subject);
            Assert.AreEqual(_tsMailSave.GetFrom(count - 1).ToString(), "1ban-admin" + domain);
            
        }

        //３０通蓄積された状態
        [TestCase("summary 3","[1ban:00003] TEST_2\r\n")]
        [TestCase("summary 3-5", "[1ban:00003] TEST_2\r\n[1ban:00004] TEST_3\r\n[1ban:00005] TEST_4\r\n")]
        public void Summary2Test(string body, string response) {

            const string domain = "@example.com";
            const string from = "user1" + domain;

            for (var i = 0; i < 30; i++) {
                var m = new TsMail(from, "1ban" + domain, "DMY");
                m.Mail.AddHeader("subject", string.Format("TEST_{0}", i));//試験的に件名を挿入する

                _ml.Job(m.MlEnvelope, m.Mail);
            }
            //この時点で、user1,user2,adin2のそれぞれ30通が送信されているため_tsMailSave.Count()は90となる
            //事後のテストのため一度クリアする
            _tsMailSave.Clear();


            var mail = new TsMail(from, "1ban-ctl" + domain, body);
            _ml.Job(mail.MlEnvelope, mail.Mail);

            var s = Encoding.ASCII.GetString(_tsMailSave.GetMail(0).GetBody());
            Assert.AreEqual(s, response);



        }

    }
}
