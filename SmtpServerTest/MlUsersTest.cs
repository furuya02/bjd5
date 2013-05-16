using Bjd.ctrl;
using Bjd.mail;
using Bjd.option;
using NUnit.Framework;
using SmtpServer;
using Bjd;

namespace SmtpServerTest {
    [TestFixture]
    class MlUsersTest {

        MlUserList _mlUserList;

        [SetUp]
        public void SetUp() {
            //var kernel = new Kernel(null,null,null,null);
            //var logger = new Logger(kernel,"",false,null);

            //参加者
            var dat = new Dat(new[] { CtrlType.TextBox, CtrlType.TextBox, CtrlType.CheckBox, CtrlType.CheckBox, CtrlType.CheckBox, CtrlType.TextBox });
            bool manager = false;
            dat.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER1", "user1@example.com", manager, true, true, "")); //読者・投稿
            dat.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER2", "user2@example.com", manager, true, false, ""));//読者 　×
            dat.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "USER3", "user3@example.com", manager, false, true, ""));//×　　投稿
            manager = true;//管理者
            dat.Add(true, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "ADMIN", "admin@example.com", manager, false, true, "123"));//×　　投稿

            _mlUserList = new MlUserList(dat);
        }


        [TearDown]
        public void TearDown() {
        
        }

        //検索に成功するテスト
        [TestCase("user1@example.com","USER1",false,true,true,"")]
        [TestCase("user2@example.com", "USER2", false, true, false, "")]
        [TestCase("user3@example.com", "USER3", false, false, true, "")]
        [TestCase("admin@example.com", "ADMIN", true, false, true, "")]
        public void SearchSuccessTest(string mailAddress, string name, bool isManager, bool isReader, bool isContributor, string password) {
            //正常に登録されているか検索してみる
            var o = _mlUserList.Search(new MailAddress(mailAddress));//検索
            //名前
            Assert.AreEqual(o.Name,name);
            //メールアドレス
            Assert.AreEqual(o.MailAddress.ToString(), mailAddress);
            //管理者
            Assert.AreEqual(o.IsManager, isManager);
            //読者
            Assert.AreEqual(o.IsReader, isReader);
            //投稿
            Assert.AreEqual(o.IsContributor,isContributor);
            //パスワード
            Assert.AreEqual(o.Psssword,password);
        }

        //検索に失敗するテスト
        [TestCase("1@1", false)]
        public void SearchErrorTest(string mailAddress, bool find) {
            var o = _mlUserList.Search(new MailAddress(mailAddress));//検索
            Assert.IsNull(o);
        }

        //追加して検索するテスト
        [TestCase("1@1")]
        public void AddSearchTest(string mailAddress) {

            //ユーザを追加する
            _mlUserList.Add(new MailAddress(mailAddress), "追加");
            var o = _mlUserList.Search(new MailAddress(mailAddress));//追加したユーザを検索する
            
            //内容を確認する
            Assert.AreNotEqual(o, null);
            Assert.AreEqual(o.IsManager, false);
            Assert.AreEqual(o.IsContributor, true);
            Assert.AreEqual(o.IsReader, true);
            Assert.AreEqual(o.MailAddress.ToString(),mailAddress);
        }



    }
}
