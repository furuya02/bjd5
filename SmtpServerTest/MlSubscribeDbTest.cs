using System;
using Bjd.mail;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;
using Bjd;
using BjdTest;
using System.Threading;

namespace SmtpServerTest {
    [TestFixture]
    class MlSubscribeDbTest {
        
        MlSubscribeDb _mlSubscribeDb;
        private const double EffectiveMsec = 50; //50msec(本来は60秒以上とするが、テストのため50msecで初期化する)

        [SetUp]
        public void SetUp() {
            //var tsDir = new TsDir();
            var manageDir = TestUtil.GetTmpDir("TestDir");

            const string mlName = "1ban";
            _mlSubscribeDb = new MlSubscribeDb(manageDir, mlName, EffectiveMsec);
        }
        
        [TearDown]
        public void TearDown(){

            _mlSubscribeDb.Remove();
        }

        [TestCase(0, true)]//時間内なので有効
        [TestCase(50, false)]//経過時間を超えたので無効
        public void EffectioveMSecTest(int msec, bool success) {
            var addr = new MailAddress("user1@example.com");
            _mlSubscribeDb.Add(addr,"NAME");
            Thread.Sleep(msec);
            var o = _mlSubscribeDb.Search(addr);
            if(success){
                Assert.IsNotNull(o);
            }else{
                Assert.IsNull(o);
            }
        }

        [TestCase("user1,user2,user3", "user1","user1",false)]//user1,user2,user3を登録して、user1を削除してuser1を検索（失敗）
        [TestCase("user1,user2,user3", "user1", "user2", true)]//user1,user2,user3を登録して、user1を削除してuser2を検索（成功）
        [TestCase("user1,user2,user3", "user4", "user1", true)]//user1,user2,user3を登録して、(無効値)user4を削除してuser2を検索（成功）
        public void DelTest(string users, string delUser, string searchUser, bool success) {

            //複数のユーザを追加
            string[] ar = users.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var user in ar) {
                var addr = new MailAddress(string.Format("{0}@example.com", user));
                _mlSubscribeDb.Add(addr, user);
            }
            //削除アドレス
            if (delUser != "") {
                var delAddr = new MailAddress(delUser, "example.com");
                _mlSubscribeDb.Del(delAddr);
            }
            //検索アドレス
            var searchAddr = new MailAddress(searchUser, "example.com");
            var o = _mlSubscribeDb.Search(searchAddr);

            if (success) {
                Assert.AreEqual(o.Name,searchUser);
                //Assert.IsNotNull(o);
            } else {
                Assert.IsNull(o);
            }
        }

        [TestCase("user1,user2","user1",true)]//検索成功(user1,user2を追加してuser1を検索)
        [TestCase("user1,user2", "user2", true)]//検索成功
        [TestCase("user1,user2", "user3", false)]//検索失敗(user1,user2を追加してuser3を検索)
        [TestCase("user1", "user2", false)]
        [TestCase("", "user1",false)]//登録が無い場合の検索は失敗する
        public void AddSearchTest(string users, string searchUser, bool success) {
            
            //複数のユーザを追加
            var ar = users.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries);
            foreach (var user in ar) {
                var addr = new MailAddress(string.Format("{0}@example.com",user));
                _mlSubscribeDb.Add(addr, user);
            }
            //検索アドレス
            var searchAddr = new MailAddress(searchUser, "example.com");
            //検索実行
            var o = _mlSubscribeDb.Search(searchAddr);
            if (success) {//成功の場合
                Assert.AreEqual(o.MailAddress.ToString(), searchAddr.ToString());
            } else {//失敗の場合
                Assert.IsNull(o);//NULLが返る
            }
            
        }
    }
}
