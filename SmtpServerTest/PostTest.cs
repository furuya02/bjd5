using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SmtpServer;
using BjdTest;
using Bjd;
using System.IO;

namespace SmtpServerTest {
    
    [TestFixture]
    class PostTest {
        private const string _mlName = "1ban";
        readonly List<string> _domainList = new List<string>() { "example.com" };

        private Ml _ml;
        TsOption _tsOption;
        private MailBox _mailBox;
        private MailQueue _mailQueue;
            

        [SetUp]
        public void SetUp() {
            var tsDir = new TsDir();
            _tsOption = new TsOption(tsDir);
            _tsOption.Set("FOLDER", "MailBox", "dir", string.Format("{0}\\MailBox", tsDir.Src));
            //user1,user2,user3
            _tsOption.Set("DAT", "MailBox", "user", "user1\tpass\buser2\tpass\buser3\tpass");

            var kernel = new Kernel(null, null, null, null);
            var logger = new Logger(kernel, "LOG", false, null);
            var manageDir = tsDir.Src + "\\TestDir";
            //MailQueue
            _mailQueue = new MailQueue(tsDir.Src + "\\MailQueue");
            var oneOption = kernel.ListOption.Get("MailBox");

            _mailBox = new MailBox(kernel, oneOption);

            var mailSave = new MailSave(kernel, _mailBox, logger, _mailQueue, "", _domainList);//モック

            var memberList = new Dat();
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER1", "user1@example.com", false, true, true, "")); //一般・読者・投稿
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER2", "user2@example.com", false, true, false, ""));//一般・読者・×
            memberList.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER3", "user3@example.com", false, false, true, ""));//一般・×・投稿
            //memberList.Add(false, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER6" , "user6@example.com" , false, false, true, ""));//一般・×・投稿 (Disable)
            //memberList.Add(true,  string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN" , "admin@example.com" , true, false, true, "123"));//管理者・×・投稿
            //memberList.Add(true,  string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN2", "admin2@example.com", true, true, true, "456"));//管理者・読者・投稿
            //memberList.Add(false, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN3", "admin3@example.com", true, true, true, "789"));//管理者・読者・投稿 (Disable)
            var docs = (from object o in Enum.GetValues(typeof(MLDocKind)) select "").ToList();
            const int maxSummary = 10;
            const int getMax = 10;
            const bool autoRegistration = true;
            const int titleKind = 1;
            var mlOption = new MlOption(maxSummary, getMax, autoRegistration, titleKind, docs, manageDir, memberList);

            _ml = new Ml(kernel, logger, mailSave, mlOption, _mlName, _domainList);

        }

        [TearDown]
        public void TearDown() {
            _ml.Remove();
            Directory.Delete(_mailBox.Dir,true);
            Directory.Delete(_mailQueue.Dir,true);
            _tsOption.Dispose();
        }

        //user1からの1ban@example.comへのメールは、user1及びuser2に届く
        [TestCase("user1@example.com","1ban@example.com","user1@example.com,user2@example.com")]
        public void TestTest(string from,string to,string recvers)
        {

            var recvList = recvers.Split(new char[]{','},StringSplitOptions.None);
            var mail = new TsMail(from,to, "dmy");
            _ml.Job(mail.MlEnvelope, mail.Mail);
            


            //user1とuser2に届く

            //***************************************************
            //返されるはuser1,user2,admin2への3通
            //ヘッダは、すべて送信者->POST
            //エンベロープは送信者->user1,user2
            //***************************************************
            /*Assert.AreEqual(3, init.MailSave.Count());
            int index = 0;
            var fromStr = user1Str;//送信者
            var toStr = mlPostStr;//post
            var from = user1;//送信者
            var to = user1;//user1
            var subject = string.Format("[{0}]", init._mlName);//件名
            Comfirm(index, fromStr, toStr, from, to, subject);

            index = 1;
            to = user2;//user2
            Comfirm(index, fromStr, toStr, from
                                               to, subject);

            index = 2;
            to = admin2;//admin2
            Comfirm(index, fromStr, toStr, from, to, subject);
            */

        }
    }
}
