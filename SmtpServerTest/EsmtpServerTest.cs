using System;
using System.IO;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;
using System.Collections.Generic;

namespace SmtpServerTest {
    [TestFixture]
    class EsmtpServerTest : ILife {

        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v4Sv; //サーバ
        private static Server _v6Sv; //サーバ


        [SetUp]
        public void SetUp() {
            //MailBoxは、Smtp3ServerTest.iniの中で「c:\tmp2\bjd5\SmtpServerTest\mailbox」に設定されている
            //また、上記のMaloBoxには、user1=0件　user2=2件　のメールが着信している

            //設定ファイルの退避と上書き
            _op = new TmpOption("SmtpServerTest", "EsmtpServerTest.ini");
            var kernel = new Kernel();
            var option = kernel.ListOption.Get("Smtp");
            var conf = new Conf(option);

            //サーバ起動
            _v4Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
            _v4Sv.Start();
            _v6Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
            _v6Sv.Start();


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



        //DFファイルの一覧を取得する
        private string[] GetDf(string user){
            var dir = string.Format("c:\\tmp2\\bjd5\\SmtpServerTest\\mailbox\\{0}", user);
            var files = Directory.GetFiles(dir,"DF*");
            return files;
        }

        //クライアントの生成
        SockTcp CreateClient(InetKind inetKind) {
            int port = 8825;  //ウイルススキャンにかかるため25を避ける
            if (inetKind == InetKind.V4) {
                return Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), port, 10, null);
            }
            return Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), port, 10, null);

        }

        private void Ehlo(SockTcp cl) {
            var localPort = cl.LocalAddress.Port; //なぜかローカルのポートアドレスは１つ小さい

            //バナー
            const string bannerStr = "220 localhost SMTP BlackJumboDog ";
            Assert.That(cl.StringRecv(3, this).Substring(0, 33), Is.EqualTo(bannerStr));

            //EHLO
            cl.StringSend("EHLO 1");
            var lines = Inet.RecvLines(cl, 4, this);

            var str = string.Format("250-localhost Helo 127.0.0.1[127.0.0.1:{0}], Pleased to meet you.", localPort);
            if (cl.LocalAddress.Address.ToString() == "::1") {
                str = string.Format("250-localhost Helo ::1[[::1]:{0}], Pleased to meet you.", localPort);
            }
            Assert.That(lines[0], Is.EqualTo(str));
            Assert.That(lines[1], Is.EqualTo("250-8BITMIME"));
            Assert.That(lines[2], Is.EqualTo("250-SIZE=5000"));
            Assert.That(lines[3], Is.EqualTo("250-AUTH LOGIN PLAIN CRAM-MD5"));
            Assert.That(lines[4], Is.EqualTo("250 HELP"));

        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void EHLOコマンド(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);

            //exercise verify
            Ehlo(cl);

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 認証前の無効コマンド(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);
            var expected = "500 command not understood: XXX\r\n";

            //exercise
            cl.StringSend("XXX");
            var actual = cl.StringRecv(3, this);
            
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 無効なAUTHコマンド(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);
            var expected = "504 Unrecognized authentication type.\r\n";

            //exercise
            cl.StringSend("AUTH XXX");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void EHLOのパラメータ不足(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var banner = cl.StringRecv(3, this);
            var expected = "501 EHLO requires domain address\r\n";

            //exercise
            cl.StringSend("EHLO");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 認証前にMAILコマンドを送るとエラーになる(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);
            var expected = "530 Authentication required.\r\n";

            //exercise
            cl.StringSend("MAIL From:1@1");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void AUTH_PLAIN認証_正常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);

            
            var expected = "235 Authentication successful.\r\n";

            //exercise
            cl.StringSend("AUTH PLAIN");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("334 \r\n"));

            //UserID\0UserID\0Password」をBase64でエンコード
            var str = string.Format("user1\0user1\0user1");
            str = Base64.Encode(str);

            cl.StringSend(str);
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void AUTH_PLAIN認証_異常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);


            //↓これは、後ほど仕様変更が必要
            var expected = "500 command not understood: dXNlcjEAdXNlcjEAdXNlcjFYWFg=\r\n";

            //exercise
            cl.StringSend("AUTH PLAIN");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("334 \r\n"));

            //UserID\0UserID\0Password」をBase64でエンコード
            var str = string.Format("user1\0user1\0user1");
            str = Base64.Encode(str+"XXX"); //<=ゴミデータ追加

            cl.StringSend(str);
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void AUTH_LOGIN認証_正常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);


            var expected = "235 Authentication successful.\r\n";

            //exercise
            cl.StringSend("AUTH LOGIN");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("334 VXNlcm5hbWU6\r\n"));

            cl.StringSend(Base64.Encode("user1")); //ユーザ名をBase64で送る
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("334 UGFzc3dvcmQ6\r\n"));

            cl.StringSend(Base64.Encode("user1"));//パスワードをBase64で送る
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void AUTH_LOGIN認証_異常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);


            String expected = null;

            //exercise
            cl.StringSend("AUTH LOGIN");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("334 VXNlcm5hbWU6\r\n"));

            cl.StringSend(Base64.Encode("user1")); //ユーザ名をBase64で送る
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("334 UGFzc3dvcmQ6\r\n"));

            cl.StringSend(Base64.Encode("user1")+"XXX");//パスワードをBase64で送る <=ゴミデータ追加
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void AUTH_CRAM_MD5認証_正常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);


            var expected = "235 Authentication successful.\r\n";

            //exercise
            cl.StringSend("AUTH CRAM-MD5");
            var recvStr = cl.StringRecv(3, this);
            recvStr = Inet.TrimCrlf(recvStr);
            Assert.That(recvStr.Substring(0,3), Is.EqualTo("334"));
            var hash = Md5.Hash("user1",Base64.Decode(recvStr.Substring(4)));
            cl.StringSend(Base64.Encode(string.Format("user1 {0}", hash)));
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void AUTH_CRAM_MD5認証_異常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);

            String expected = null;

            //exercise
            cl.StringSend("AUTH CRAM-MD5");
            var recvStr = cl.StringRecv(3, this);
            recvStr = Inet.TrimCrlf(recvStr);
            Assert.That(recvStr.Substring(0, 3), Is.EqualTo("334"));
            var hash = Md5.Hash("user1", Base64.Decode(recvStr.Substring(4)));
            cl.StringSend(Base64.Encode(string.Format("user1 {0}", hash))+"XXX"); //<=ゴミデータ追加
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 正常系メール送信(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Ehlo(cl);

            //exercise
            cl.StringSend("AUTH PLAIN");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("334 \r\n"));
            cl.StringSend(Base64.Encode("user1\0user1\0user1"));
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("235 Authentication successful.\r\n"));


            cl.StringSend("MAIL From:1@1");
            var s =  cl.StringRecv(3, this);

            cl.StringSend("RCPT To:user1@example.com");
            s = cl.StringRecv(3, this);

            cl.StringSend("DATA");
            s = cl.StringRecv(3, this);

            cl.StringSend(".");
            s = cl.StringRecv(3, this);

            var expected = 1;

            //exercise
            var actual = GetDf("user1").Length;

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();

        }

       
        public bool IsLife() {
            return true;
        }
    }
}
