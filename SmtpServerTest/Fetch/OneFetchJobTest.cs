using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest.Fetch {
    class OneFetchJobTest : ILife{
        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Pop3Server.Server _v6Sv; //サーバ
        private static Pop3Server.Server _v4Sv; //サーバ

        [SetUp]
        public void SetUp() {
            //MailBoxは、Pop3ServerTest.iniの中で「c:\tmp2\bjd5\SmtpServerTest\mailbox」に設定されている
            //また、上記のMaloBoxには、user1=0件　user2=2件　のメールが着信している

            //設定ファイルの退避と上書き
            _op = new TmpOption("SmtpServerTest", "PopClientTest.ini");
            var kernel = new Kernel();
            var option = kernel.ListOption.Get("Pop3");
            var conf = new Conf(option);

            //サーバ起動
            _v4Sv = new Pop3Server.Server(kernel, conf, new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
            _v4Sv.Start();

            _v6Sv = new Pop3Server.Server(kernel, conf, new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
            _v6Sv.Start();

            //メールボックスへのデータセット
            const string srcDir = @"c:\tmp2\bjd5\SmtpServerTest\";
            const string dstDir = @"c:\tmp2\bjd5\SmtpServerTest\mailbox\user2\";
            File.Copy(srcDir + "DF_00635026511425888292", dstDir + "DF_00635026511425888292", true);
            File.Copy(srcDir + "DF_00635026511765086924", dstDir + "DF_00635026511765086924", true);
            File.Copy(srcDir + "MF_00635026511425888292", dstDir + "MF_00635026511425888292", true);
            File.Copy(srcDir + "MF_00635026511765086924", dstDir + "MF_00635026511765086924", true);

            Thread.Sleep(100);//少し余裕がないと多重でテストした場合に、サーバが起動しきらないうちにクライアントからの接続が始まってしまう。

        }

        // ログイン失敗などで、しばらくサーバが使用できないため、TESTごとサーバを立ち上げて試験する必要がある
        [TearDown]
        public void TearDown() {
            //サーバ停止
            _v4Sv.Stop();
            _v6Sv.Stop();

            _v4Sv.Dispose();
            _v6Sv.Dispose();

            //設定ファイルのリストア
            _op.Dispose();

            //メールボックスの削除
            Directory.Delete(@"c:\tmp2\bjd5\SmtpServerTest\mailbox", true);

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
