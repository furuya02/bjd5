using System;
using System.IO;
using Bjd;
using Bjd.log;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class OneFetchJobTest : ILife{
        
        private TestServer _testServer;

        // ログイン失敗などで、しばらくサーバが使用できないため、TESTごとサーバを立ち上げて試験する必要がある
        [SetUp]
        public void SetUp() {

            _testServer = new TestServer(TestServerType.Pop, "SmtpServerTest\\Fetch", "PopClientTest.ini");

            //usrr2のメールボックスへの２通のメールをセット
            _testServer.SetMail("user2", "00635026511425888292");
            _testServer.SetMail("user2", "00635026511765086924");

        }


        [TearDown]
        public void TearDown(){
            _testServer.Dispose();
            //fetchDbの削除
            File.Delete(@"c:\tmp2\bjd5\BJD\out\fetch.127.0.0.1.9110.user2.localuser.db");
            File.Delete(@"c:\tmp2\bjd5\BJD\out\fetch.127.0.0.1.9110.user1.localuser.db");
        }
    
       

        [Test]
        public void 接続のみの確認() {
            //setUp
            var interval = 10;//10分
            var synchronize = 0;
            var keepTime = 100;//100分
            var oneFetch = new OneFetch(interval, "127.0.0.1", 9110, "user1", "user1", "localuser", synchronize, keepTime);
            var sut = new OneFetchJob(new Kernel(), oneFetch, 3, 1000);
            var expected = true;
            //exercise
            var actual = sut.Job(new Logger(), DateTime.Now, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //tearDown
            sut.Dispose();
        }

        [Test]
        public void ホスト名の解決に失敗している時_処理はキャンセルされる() {
            //setUp
            var interval = 10;//10分
            var synchronize = 0;
            var keepTime = 100;//100分
            //不正ホスト名 xxxxx
            var oneFetch = new OneFetch(interval, "xxxxx", 9110, "user1", "user1", "localuser", synchronize, keepTime);
            var sut = new OneFetchJob(new Kernel(), oneFetch, 3, 1000);
            var expected = false;
            //exercise
            var actual = sut.Job(new Logger(), DateTime.Now, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //tearDown
            sut.Dispose();
        }


        [Test]
        public void インターバルが10分の時_5分後の処理はキャンセルされる() {
            //setUp
            var interval = 10;//10分
            var synchronize = 0;
            var keepTime = 100;//100分
            var oneFetch = new OneFetch(interval,"127.0.0.1",9110,"user1","user1","localuser",synchronize,keepTime);
            var sut = new OneFetchJob(new Kernel(),oneFetch, 3, 1000);
            var expected = false;
            //exercise
            //１回目の接続
            sut.Job(new Logger(), DateTime.Now, this);
            //２回目（5分後）の接続
            var actual = sut.Job(new Logger(), DateTime.Now.AddMinutes(5), this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //tearDown
            sut.Dispose();
        }

        [Test]
        public void 動作確認() {
            //setUp
            var interval = 10;//10分
            var synchronize = 0;
            var keepTime = 100;//100分
            var oneFetch = new OneFetch(interval, "127.0.0.1", 9110, "user2", "user2", "localuser", synchronize, keepTime);
            var sut = new OneFetchJob(new Kernel(), oneFetch, 3, 1000);
            var expected = true;
            //exercise
            var actual = sut.Job(new Logger(), DateTime.Now, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //tearDown
            sut.Dispose();
        }



        public bool IsLife(){
            return true;
        }
    }
}
