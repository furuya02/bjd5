using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest.Fetch {
    class OneFetchJobTest {
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
        }
        
        [Test]
        public void A(){
            //setUp
            int synchronize = 0;
            int keepTime = 100;//100分
            var oneFetch = new OneFetch(1,"127.0.0.1",9110,"user1","user1","localuser1",synchronize,keepTime);
            var sut = new OneFetchJob(new Kernel(), oneFetch, 3, 1000);
            var expected = true;
            //exercise
            //var actual = sut.Job();
            //verify
            //Assert.That(actual, Is.EqualTo(expected));
            
            //tearDown
            sut.Dispose();

        }
    }
}
