using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SmtpServer;
using Bjd;

namespace SmtpServerTest
{
    public class MlTotalTest
    {
        private Ml ml;
        private MailAddress user1;
        private MailAddress user2;
        //MailAddress user3;
        private MailAddress user4;
        private MailAddress admin;
        private MailAddress admin2;
        //MailAddress mlCtrl;
        //MailAddress mlPost;
        private MailAddress mlAdmin;
        //MailAddress mailDaemon;
        private string user1Str;
        //string user2Str;
        //string user3Str;
        private string user4Str;
        //string adminStr;
        private string admin2Str;
        private string mlCtrlStr;
        private string mlPostStr;
        private string mlAdminStr;
        //string mailDaemonStr;

        private Initialization2 init;

        [SetUp]
        public void SetUp()
        {
            init = new Initialization2();

            ml = init.CreateMl();

            user1 = new MailAddress("user1@example.com");
            user2 = new MailAddress("user2@example.com");
            //user3 = new MailAddress("user3@example.com");
            user4 = new MailAddress("user4@example.com");
            admin = new MailAddress("admin@example.com");
            admin2 = new MailAddress("admin2@example.com");
            //mlCtrl = init.MlAddr.Ctrl;
            //mlPost = init.MlAddr.Post;
            mlAdmin = init.MlAddr.Admin;
            //mailDaemon = new MailAddress("MAILER-DAEMON@example.com");
            user1Str = string.Format("\"USER1\" <{0}>", user1.ToString());
            //user2Str = string.Format("\"USER2\" <{0}>", user2.ToString());
            //user3Str = string.Format("\"USER3\" <{0}>", user3.ToString());
            user4Str = string.Format("\"USER4\" <{0}>", user4.ToString());
            //adminStr = string.Format("\"ADMIN\" <{0}>", admin.ToString());
            admin2Str = string.Format("\"ADMIN2\" <{0}>", admin2.ToString());
            mlCtrlStr = string.Format("\"1BAN(CTRL)\" <{0}>", init.MlAddr.Ctrl);
            mlPostStr = string.Format("\"1BAN\" <{0}>", init.MlAddr.Post);
            mlAdminStr = init.MlAddr.Admin.ToString();
            //mailDaemonStr = string.Format("\"Mail Delivery Subsystem\"<{0}>", mailDaemon.ToString());

            //init.MailSave.Clear();//初期化
        }

        [TearDown]
        public void TearDown()
        {
            init.Dispose();
        }

        //***************************************************
        //get
        //***************************************************
        /*
        [Test]
        public void Ml_Get1_Test() {

            var current = init.Ml.Count();
            var max = init.MlOption.MaxGet;//添付メールの最大数

         * //******************************************************************
            //DBの蓄積を10件にする
            //******************************************************************

         * current = 10;

           
            //テスト「get 15-25」 範囲を超えてリクエスト
            s = 15;
            e = 25;
            ar = TestGet("get 15-25");
            Assert.AreEqual(ar.Count, 1); //返されるメールは1通
            TestGetSubject(ar[0], s, -1);//Getの件名確認
            Assert.AreEqual(GetAttach(ar[0]).Count(), 0);//添付なしを確認する

        }
        //Getの件名確認
        void TestGetSubject(Mail mail, int start, int end) {
            var subject = mail.GetHeader("subject");
            if (end == -1) {
                Assert.AreEqual(subject, string.Format("not found No.{0} ({1} ML)", start, init.MlName));
            } else {
                Assert.AreEqual(subject, string.Format("result for get [{0}-{1} MIME/multipart] ({2} ML)", start, end, init.MlName));
            }

        }
        //エンベロープの試験だけ行い、受け取ったメールを返す
        //List<Mail> TestGet(int start, int end, string body) {
        List<Mail> TestGet(string body) {
            init.MailSave.Clear();
            var t = new TsMail(user1Str, mlCtrlStr, body);
            ml.Job(t.MlEnvelope, t.Mail);

            var ar = new List<Mail>();
            for (int i = 0; i < init.MailSave.Count(); i++) {
                //エンベロープの確認
                var from = init.MailSave.GetFrom(i).ToString();
                var to = init.MailSave.GetTo(i).ToString();
                Assert.AreEqual(from.ToString(), mlAdmin.ToString());
                Assert.AreEqual(to.ToString(), user1.ToString());
                //メールは、Listにして呼び出しもとに返す
                ar.Add(init.MailSave.GetMail(i));
            }
            return ar;
        }

        //添付されたメールの件名の確認
        void TestAttachSubject(int start, Mail mail) {
            var ar = GetAttach(mail);//添付メールの取得
            for (int i = 0; i < ar.Count; i++) {
                //添付メールのタイトル確認
                var subject = ar[i].GetHeader("subject");
                var s = string.Format("[{0}:{1:D5}]TITLE", init.MlName, i + start);
                Assert.AreEqual(subject, s);
            }
        }
        //メール本文から添付されているメールを取り出す
        List<Mail> GetAttach(Mail orgMail) {
            var ar = new List<Mail>();

            List<string> lines = new List<string>();
            foreach (var buf in Inet.GetLines(orgMail.GetBody())) {
                var s = Encoding.ASCII.GetString(buf);
                lines.Add(s);
            }
            Mail mail = null;
            for (int i = 0; i < lines.Count; i++) {
                if (lines[i].IndexOf("--BJD-Boundary--") != -1) {
                    break;
                } else if (lines[i].IndexOf("--BJD-Boundary") != -1) {
                    if (mail != null)
                        ar.Add(mail);
                    do {
                        i++;
                    } while (lines[i] != "\r\n");
                    mail = new Mail(null);
                    continue;
                } else {
                    if (mail != null) {
                        mail.Init(Encoding.ASCII.GetBytes(lines[i]));
                    }
                }
            }
            if (mail != null)
                ar.Add(mail);
            return ar;
        }
        */
        //***************************************************
        //summary
        //***************************************************
        /*
        [Test]
        public void Ml_Summary_Test() {

            var max = init.MlOption.MaxSummary;//添付メールの最大数(20)

            //テスト「summary 3」
            int s = 3;
            int e = 3;
            var ar = TestSummary("summary 3");
            Assert.AreEqual(ar.Count, 1); //返されるメールは１通
            TestSummarySubject(ar[0], s, e);//Summaryの件名確認
            TestSummaryLines(s, e, ar[0]);//本文内の件名一覧確認

            //テスト「summary 2-6」
            s = 2;
            e = 6;
            ar = TestSummary("summary 2-6");
            Assert.AreEqual(ar.Count, 1); //返されるメールは１通
            TestSummarySubject(ar[0], s, e);//Summaryの件名確認
            TestSummaryLines(s, e, ar[0]);//本文内の件名一覧確認

            //テスト「summary last:3」
            s = 28;
            e = 30;
            ar = TestSummary("summary last:3");
            Assert.AreEqual(ar.Count, 1); //返されるメールは１通
            TestSummarySubject(ar[0], s, e);//Summaryの件名確認
            TestSummaryLines(s, e, ar[0]);//本文内の件名一覧確認

            //テスト「summary first:5」
            s = 1;
            e = 5;
            ar = TestSummary("summary first:5");
            Assert.AreEqual(ar.Count, 1); //返されるメールは１通
            TestSummarySubject(ar[0], s, e);//Summaryの件名確認
            TestSummaryLines(s, e, ar[0]);//添付メールのタイトル確認

            ////テスト 「summary」DBには30件あり、添付最大数は20なので、２通に分けて返信される
            int current = init.Ml.Count();
            s = 1;
            e = 30;
            ar = TestSummary("summary");
            Assert.AreEqual(ar.Count, 2); //返されるメールは１通
            //1通名の確認
            TestSummarySubject(ar[0], s, max);//Summaryの件名確認
            TestSummaryLines(s, max, ar[0]);//添付メールのタイトル確認
            //2通名の確認
            TestSummarySubject(ar[1], max + 1, current);//Summaryの件名確認
            TestSummaryLines(max + 1, current, ar[1]);//添付メールのタイトル確認

            ////DBにないデータ番号をリクエストした場合

            //テスト「summary 100」
            s = 100;
            e = 100;
            ar = TestSummary("summary 100");
            Assert.AreEqual(ar.Count, 1); //返されるメールは１通
            TestSummarySubject(ar[0], s, -1);//Summaryの件名確認 not found
            TestSummaryLines(s, current, ar[0]);//添付メールのタイトル確認

            ////******************************************************************
            ////DBの蓄積を10件にする
            ////******************************************************************
            //for (int i = 11; i < 300; i++) {
            //    init.TsMlDb.Clear(i);
            //}
            //init.TsMlDb.SetNo(10);//連番を10にセットする
            init.Ml.Remove();
            for (int i = 0; i < 10; i++) {
                var mail = new Mail(null);
                mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
                mail.AddHeader("subject", string.Format("[{0}:{1:D5}]TITLE", init.MlName, i + 1));
                mail.Init(Encoding.ASCII.GetBytes("1\r\n"));//本文
                mail.Init(Encoding.ASCII.GetBytes("2\r\n"));//本文
                mail.Init(Encoding.ASCII.GetBytes("3\r\n"));//本文

                init.Ml.Save(mail);
            }
            current = 10;


            
            ////******************************************************************
            ////DBの蓄積を0件にする
            ////******************************************************************
            //for (int i = 1; i < 300; i++) {
            //    init.TsMlDb.Clear(i);
            //}
            //init.TsMlDb.SetNo(0);//連番を0にセットする
            init.Ml.Remove();
            current = 0;

            
        }
        //エンベロープの試験だけ行い、受け取ったメールを返す
        //List<Mail> TestSummary(int start, int end, string body) {
        List<Mail> TestSummary(string body) {
            init.MailSave.Clear();
            var t = new TsMail(user1Str, mlCtrlStr, body);
            ml.Job(t.MlEnvelope, t.Mail);

            var ar = new List<Mail>();
            for (int i = 0; i < init.MailSave.Count(); i++) {
                //エンベロープの確認
                var from = init.MailSave.GetFrom(i).ToString();
                var to = init.MailSave.GetTo(i).ToString();
                Assert.AreEqual(from.ToString(), mlAdmin.ToString());
                Assert.AreEqual(to.ToString(), user1.ToString());
                //メールは、Listにして呼び出しもとに返す
                ar.Add(init.MailSave.GetMail(i));
            }
            return ar;
        }


        //Summaryの件名確認
        void TestSummarySubject(Mail mail, int start, int end) {
            if (end == -1) {
                Assert.AreEqual(mail.GetHeader("subject"), string.Format("not found No.{0} ({1} ML)", start, init.MlName));
            } else {
                Assert.AreEqual(mail.GetHeader("subject"), string.Format("result for summary [{0}-{1}] ({2} ML)", start, end, init.MlName));
            }

        }

        //本文中のsummaryリクエストの件名の確認
        void TestSummaryLines(int start, int end, Mail mail) {
            var lines = new List<string>();
            var bufs = mail.GetBody();
            if (bufs.Length > 0) {
                foreach (var buf in Inet.GetLines(bufs)) {
                    lines.Add(Encoding.ASCII.GetString(buf));
                }
            }
            if (end == -1) {
                Assert.AreEqual(lines.Count(), 0);
            } else {
                for (int i = 0; i < end - start + 1; i++) {
                    var s = string.Format("[{0}:{1:D5}]TITLE\r\n", init.MlName, i + start);
                    Assert.AreEqual(lines[i], s);
                }
            }
        }
        */
        //***************************************************
        //mlAdminに対するメール
        //***************************************************
        /*
        [Test]
        public void Ml_Admin_Test() {

            //user1(メンバ）からのmlAdminに対するメール
            TestToAdmin(user1Str, user1);
            //user4(メンバ外）からのmlAdminに対するメール
            TestToAdmin(user4Str, user4);
            //admin2(管理者)からのmlAdminに対するメール
            TestToAdmin(admin2Str, admin2);
        }

        void TestToAdmin(string userStr, MailAddress user) {
            //初期化
            init.MailSave.Clear();

            var t = new TsMail(userStr, mlAdminStr, "123");
            string subject = "TEST";
            t.Mail.ConvertHeader("subject", subject);
            ml.Job(t.MlEnvelope, t.Mail);

            //***************************************************
            //管理者全員 admin admin2への2通を確認
            //***************************************************
            Assert.AreEqual(2, init.MailSave.Count());

            int index = 0;
            var fromStr = userStr;
            var toStr = admin.ToString();
            var from = user;
            var to = admin;
            Comfirm(index, fromStr, toStr, from, to, subject);

            index = 1;
            toStr = admin2.ToString();
            to = admin2;
            Comfirm(index, fromStr, toStr, from, to, subject);
        }
        */

        //***************************************************
        //user1(メンバ）からのctlに対するguide送信
        //***************************************************
        /*
        [Test]
        public void Ml_Guide1_Test() {
            //返されるはmlAdmin->user4へのGuideメール
            var subject = string.Format("guide ({0} ML)", init.MlName);//件名
            ReturnTest("guide", user1Str, user1, subject);
        }
        */
        //***************************************************
        //user4(メンバ以外）からのctlに対するguide送信
        //***************************************************
        /*
        [Test]
        
        public void Ml_Guide2_Test() {
            //返されるはmlAdmin->user4へのGuideメール
            var subject = string.Format("guide ({0} ML)", init.MlName);//件名
            ReturnTest("guide", user4Str, user4, subject);
        }
        */
        //***************************************************
        //user1(メンバ）からのctlに対するhelp送信
        //***************************************************
        /*[Test]
        public void Ml_Help1_Test() {
            //返されるはmlAdmin->user1へのHelpメール
            var subject = string.Format("help ({0} ML)", init.MlName);//件名
            ReturnTest("help", user1Str, user1, subject);
        }*/
        //***************************************************
        //admin2(管理者）からのctlに対するhelp送信
        //***************************************************
        /*[Test]
        public void Ml_Help2_Test() {
            //返されるはmlAdmin->admin2へのHelpメール
            var subject = string.Format("admin ({0} ML)", init.MlName);//件名
            ReturnTest("help", admin2Str, admin2, subject);
        }*/
        //***************************************************
        //user4(メンバ外）からのctlに対するhelp送信
        //***************************************************
        /*[Test]
        public void Ml_Help3_Test() {
            //返されるはmlAdmin->user4へのDenyメール
            var subject = string.Format("You are not member ({0} ML)", init.MlName);//件名
            ReturnTest("help", user4Str, user4, subject);
        }
        */
        //管理者から返信されるメールのテスト
        /*void ReturnTest(string body, string userStr, MailAddress user, string subject) {
            var t = new TsMail(userStr, mlCtrlStr, body);
            ml.Job(t.MlEnvelope, t.Mail);

            //***************************************************
            //返されるはmlAdmin->送信者へ
            //***************************************************
            Assert.AreEqual(1, init.MailSave.Count());
            int index = 0;
            var fromStr = mlAdminStr;//管理者
            var toStr = userStr;//送信者
            var from = mlAdmin;//管理者
            var to = user;//送信者
            Comfirm(index, fromStr, toStr, from, to, subject);
        }
        */
        //確認
        /*void Comfirm(int n, string fromStr, string toStr, MailAddress from, MailAddress to, string subject) {
            //エンベロープの確認
            Assert.AreEqual(init.MailSave.GetFrom(n).ToString(), from.ToString());
            Assert.AreEqual(init.MailSave.GetTo(n).ToString(), to.ToString());
            //ヘッダの確認
            var mail = init.MailSave.GetMail(n);
            Assert.AreEqual(mail.GetHeader("from"), fromStr);
            Assert.AreEqual(mail.GetHeader("to"), toStr);
            //件名
            Assert.AreEqual(mail.GetHeader("subject"), subject);
        }
         * */
    }
}


