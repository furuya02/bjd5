using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest.Fetch {
    class PopClientTest {
        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ

        [SetUp]
        public void SetUp() {
            //MailBoxは、Pop3ServerTest.iniの中で「c:\tmp2\bjd5\Pop3ServerTest\mailbox」に設定されている
            //また、上記のMaloBoxには、user1=0件　user2=2件　のメールが着信している

            //設定ファイルの退避と上書き
            _op = new TmpOption("SmtpServerTest", "PopClientTest.ini");
            var kernel = new Kernel();
            var option = kernel.ListOption.Get("Pop3");
            var conf = new Conf(option);

            //サーバ起動
            _v4Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
            _v4Sv.Start();

            _v6Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
            _v6Sv.Start();

            //メールボックスへのデータセット
            var srcDir = @"c:\tmp2\bjd5\SmtpServerTest\";
            var dstDir = @"c:\tmp2\bjd5\SmtpServerTest\mailbox\user2\";
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

        //クライアントの生成
        SockTcp CreateClient(InetKind inetKind) {
            int port = 9110;
            if (inetKind == InetKind.V4) {
                return Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), port, 10, null);
            }
            return Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), port, 10, null);

        }
        
        [Test]
        public void AAA(){
            //setUp
            var sut = new PopClient();
            //exercise
            sut.Recv();

            //verify
            Assert.That(1, Is.EqualTo(1));

        }
    }
}
