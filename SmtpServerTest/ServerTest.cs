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
    class ServerTest : ILife {

        private TestServer _testServer;

        [SetUp]
        public void SetUp() {
            _testServer = new TestServer(TestServerType.Smtp, "SmtpServerTest","ServerTest.ini");

        }

        [TearDown]
        public void TearDown() {
            _testServer.Dispose();

        }

        //クライアントの生成
        SockTcp CreateClient(InetKind inetKind) {
            const int port = 8825; //ウイルススキャンにかかるため25を避ける
            if (inetKind == InetKind.V4) {
                return Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), port, 10, null);
            }
            return Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), port, 10, null);

        }


        private void Helo(SockTcp cl) {
            var localPort = cl.LocalAddress.Port; //なぜかローカルのポートアドレスは１つ小さい

            //バナー
            const string bannerStr = "220 localhost SMTP BlackJumboDog ";
            Assert.That(cl.StringRecv(3, this).Substring(0, 33), Is.EqualTo(bannerStr));

            //HELO
            cl.StringSend("HELO 1");

            var str = string.Format("250 localhost Helo 127.0.0.1[127.0.0.1:{0}], Pleased to meet you.\r\n", localPort);
            if (cl.LocalAddress.Address.ToString() == "::1") {
                str = string.Format("250 localhost Helo ::1[[::1]:{0}], Pleased to meet you.\r\n", localPort);
            }
            Assert.That(cl.StringRecv(3, this), Is.EqualTo(str));

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
            Assert.That(lines[3], Is.EqualTo("250 HELP"));

        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V4() {

            //var sv = _v4Sv;
            var expected = "+ サービス中 \t                Smtp\t[127.0.0.1\t:TCP 8825]\tThread";

            //exercise
            var actual = _testServer.ToString(InetKind.V4).Substring(0, 58);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V6() {

            //var sv = _v6Sv;
            var expected = "+ サービス中 \t                Smtp\t[::1\t:TCP 8825]\tThread";

            //exercise
            var actual = _testServer.ToString(InetKind.V6).Substring(0, 52);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }



        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void HELOコマンド(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            
            //exercise verify
            Helo(cl);

            //tearDown
            cl.Close();
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
        public void 無効コマンド(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
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
        public void HELOのパラメータ不足(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var banner = cl.StringRecv(3, this);
            var expected = "501 HELO requires domain address\r\n";

            //exercise
            cl.StringSend("HELO");
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
        public void MAILコマンド_正常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            var expected = "250 1@1... Sender ok\r\n";

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
        public void MAILコマンド_異常ー_Fromなし(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            var expected = "501 5.5.2 Syntax error in parameters scanning 1@1\r\n";

            //exercise
            cl.StringSend("MAIL 1@1");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void MAILコマンド_異常_メールアドレスなし(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            var expected = "501 Syntax error in parameters scanning \"\"\r\n";

            //exercise
            cl.StringSend("MAIL From:");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RCPTコマンド_正常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            var expected = "250 user1@example.com... Recipient ok\r\n";

            //exercise
            cl.StringSend("RCPT To:user1@example.com");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RCPTコマンド_正常_ドメイン無し(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            var expected = "250 user1@example.com... Recipient ok\r\n";

            //exercise
            cl.StringSend("RCPT To:user1");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }



        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RCPTコマンド_異常_無効ユーザ(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            var expected = "550 xxx... User unknown\r\n";

            //exercise
            cl.StringSend("RCPT To:xxx@example.com");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RCPTコマンド_異常_無効ユーザ_ドメインなし(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            var expected = "550 xxx... User unknown\r\n";

            //exercise
            cl.StringSend("RCPT To:xxx");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RCPTコマンド_異常_MAILの前(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);

            var expected = "503 Need MAIL before RCPT\r\n";

            //exercise
            cl.StringSend("RCPT To:user1@example.com");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RCPTコマンド_異常_パラメータなし(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            var expected = "501 Syntax error in parameters scanning \"\"\r\n";

            //exercise
            cl.StringSend("RCPT");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RCPTコマンド_異常_メールアドレスなし(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            var expected = "501 Syntax error in parameters scanning \"\"\r\n";

            //exercise
            cl.StringSend("RCPT To:");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DATAコマンド_正常(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);
            //RCPT
            cl.StringSend("RCPT To:user1@example.com");
            cl.StringRecv(3, this);

            var expected = "354 Enter mail,end with \".\" on a line by ltself\r\n";

            //exercise
            cl.StringSend("DATA");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        
        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DATAコマンド_正常_送信(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);
            //RCPT
            cl.StringSend("RCPT To:user1@example.com");
            cl.StringRecv(3, this);

            cl.StringSend("DATA");
            cl.StringRecv(3, this);


            var expected = "250 OK\r\n";

            //exercise
            cl.StringSend(".");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DATAコマンド_正常_メールボックス確認(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            
            cl.StringSend("MAIL From:1@1");
            var l0 = cl.StringRecv(5, this);
            cl.StringSend("RCPT To:user1@example.com");
            var l1 = cl.StringRecv(5, this);

            cl.StringSend("DATA");
            var l2 = cl.StringRecv(5, this);

            cl.StringSend(".");
            var l3 = cl.StringRecv(5, this);

            var expected = 1;

            //exercise
            var actual = _testServer.GetDf("user1").Length;

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DATAコマンド_空行あり_正常_メールボックス確認(InetKind inetKind)
        {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);

            cl.StringSend("MAIL From:1@1");
            var l0 = cl.StringRecv(5, this);
            cl.StringSend("RCPT To:user1@example.com");
            var l1 = cl.StringRecv(5, this);

            cl.StringSend("DATA");
            var l2 = cl.StringRecv(5, this);

            cl.StringSend("Subject:TEST");
            var l3 = cl.StringRecv(5, this);

            cl.StringSend("");
            var l4 = cl.StringRecv(5, this);

            cl.StringSend("body-1");//本文１行目
            var l5 = cl.StringRecv(5, this);

            cl.StringSend("body-2");//本文２行目
            var l6 = cl.StringRecv(5, this);
            
            cl.StringSend(".");
            var l7 = cl.StringRecv(5, this);

            var expected = 2; //本文は２行になる

            //exercise
            var mail = _testServer.GetMf("user1")[0];
            var lines = Inet.GetLines(mail.GetBody());
            var actual = lines.Count;//本分の行数を取得

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        //ヘッダとボディの間に空白行が無い場合に対応
        [TestCase(InetKind.V4)]
        //[TestCase(InetKind.V6)]
        public void DATAコマンド_空行なし_正常_メールボックス確認(InetKind inetKind)
        {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);

            cl.StringSend("MAIL From:1@1");
            var l0 = cl.StringRecv(5, this);
            cl.StringSend("RCPT To:user1@example.com");
            var l1 = cl.StringRecv(5, this);

            cl.StringSend("DATA");
            var l2 = cl.StringRecv(5, this);

            cl.StringSend("Subject:TEST");
            var l3 = cl.StringRecv(5, this);

            //cl.StringSend("");
            //var l4 = cl.StringRecv(5, this);

            cl.StringSend("body-1");//本文１行目
            var l5 = cl.StringRecv(5, this);

            cl.StringSend("body-2");//本文２行目
            var l6 = cl.StringRecv(5, this);

            cl.StringSend(".");
            var l7 = cl.StringRecv(5, this);

            var expected = 2; //本文は２行になる

            //exercise
            var mail = _testServer.GetMf("user1")[0];
            var lines = Inet.GetLines(mail.GetBody());
            var actual = lines.Count;//本分の行数を取得

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DATAコマンド_正常_連続２通(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);

            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            cl.StringSend("RCPT To:user1@example.com");
            cl.StringRecv(3, this);

            cl.StringSend("DATA");
            cl.StringRecv(3, this);

            cl.StringSend(".");
            cl.StringRecv(3, this);

            cl.StringSend("RCPT To:user1@example.com");
            cl.StringRecv(3, this);

            cl.StringSend("DATA");
            cl.StringRecv(3, this);

            cl.StringSend(".");
            cl.StringRecv(3, this);

            var expected = 2;

            //exercise
            var actual = _testServer.GetDf("user1").Length;

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DATAコマンド_異常_MAILの前(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);

            var expected = "503 Need MAIL command\r\n";

            //exercise
            cl.StringSend("DATA");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DATAコマンド_異常_RCPTの前(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);
            //MAIL
            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            var expected = "503 Need RCPT (recipient)\r\n";

            //exercise
            cl.StringSend("DATA");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 中継は拒否される(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);

            cl.StringSend("MAIL From:1@1");
            cl.StringRecv(3, this);

            cl.StringSend("RCPT To:user1@other.domain");

            var expected = "553 user1@other.domain... Relay operation rejected\r\n";

            //exercise
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        //[TestCase(InetKind.V6)]
        public void 複数行にわたるReceived行に対応(InetKind inetKind)
        {
            //setUp
            var cl = CreateClient(inetKind);
            Helo(cl);

            cl.StringSend("MAIL From:1@1");
            var l0 = cl.StringRecv(5, this);
            cl.StringSend("RCPT To:user1@example.com");
            var l1 = cl.StringRecv(5, this);

            cl.StringSend("DATA");
            var l2 = cl.StringRecv(5, this);

            cl.StringSend("Received: from mail.testdomain.co.jp (unknown [127.0.0.1]) by");
            cl.StringRecv(5, this);

            cl.StringSend(" IMSVA (Postfix) with ESMTP id 1F5C5D0037 for <test@testdomain.co.jp>;");
            cl.StringRecv(5, this);

            cl.StringSend(" Tue, 24 Feb 2015 16:28:44 +0900 (JST)");
            cl.StringRecv(5, this);

            cl.StringSend("");
            cl.StringRecv(5, this);
            
            cl.StringSend("BODY");
            cl.StringRecv(5, this);

            cl.StringSend(".");
            cl.StringRecv(5, this);

            var expected = 1; //本文は1行になる

            //exercise
            var mail = _testServer.GetMf("user1")[0];
            var lines = Inet.GetLines(mail.GetBody());
            var actual = lines.Count;//本分の行数を取得

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
