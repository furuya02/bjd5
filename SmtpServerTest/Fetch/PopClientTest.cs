using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Bjd;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class PopClientTest : ILife{
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

        private PopClient CreatePopClient(InetKind inetKind){
            if (inetKind == InetKind.V4){
                return new PopClient(new Ip(IpKind.V4Localhost), 9110, 3, this);
            }
            return new PopClient(new Ip(IpKind.V6Localhost), 9110, 3, this);
        }




        [TestCase(InetKind.V4,"127.0.0.1",9112)]
        [TestCase(InetKind.V6, "::1", 9112)]
        public void 接続失敗_ポート間違い(InetKind inetKind,String addr,int port) {
            //setUp
            var sut = new PopClient(new Ip(addr), port, 3, this);
            var expected = false;

            //exercise
            var actual = sut.Connect();

            //verify
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(sut.GetLastError(), Is.EqualTo("Faild in PopClient Connect()"));

            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4,"127.0.0.2")]
        [TestCase(InetKind.V6, "::2")]
        public void 接続失敗_アドレス間違い(InetKind inetKind, String addr) {
            //setUp
            var sut = new PopClient(new Ip(addr), 9110, 3, this);
            var expected = false;

            //exercise
            var actual = sut.Connect();

            //verify
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(sut.GetLastError(), Is.EqualTo("Faild in PopClient Connect()"));

            //tearDown
            sut.Dispose();
        }
        
        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void ログイン成功(InetKind inetKind) {
            //setUp
            var sut = CreatePopClient(inetKind);
            var expected = true;

            //exercise
            sut.Connect();
            var actual = sut.Login("user1", "user1");

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void ログイン失敗_パスワードの間違い(InetKind inetKind) {
            //setUp
            var sut = CreatePopClient(inetKind);
            var expected = false;
            
            //exercise
            sut.Connect();
            var actual = sut.Login("user1","xxx");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(sut.GetLastError(), Is.EqualTo("Timeout in PopClient RecvStatus()"));

            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void user1のUidl取得(InetKind inetKind) {
            //setUp
            var sut = CreatePopClient(inetKind);
            var expected = true;

            //exercise
            sut.Connect();
            sut.Login("user1", "user1");

            var lines = new List<string>();
            var actual = sut.Uidl(lines);

            //verify
            Assert.That(actual,Is.EqualTo(expected));
            Assert.That(lines.Count,Is.EqualTo(0));
            
            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void user2のUidl取得(InetKind inetKind) {
            //setUp
            var sut = CreatePopClient(inetKind);
            var expected = true;

            //exercise
            sut.Connect();
            sut.Login("user2", "user2");

            var lines = new List<string>();
            var actual = sut.Uidl(lines);

            //verify
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(lines[0], Is.EqualTo("1 bjd.00635026511425808252.000"));
            Assert.That(lines[1], Is.EqualTo("2 bjd.00635026511765066907.001"));

            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RETRによるメール取得(InetKind inetKind) {
            //setUp
            var sut = CreatePopClient(inetKind);
            var expected = true;

            //exercise
            sut.Connect();
            sut.Login("user2", "user2");

            var mail = new Mail();
            var actual = sut.Retr(0,mail);

            //verify
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(mail.GetBytes().Length, Is.EqualTo(317));
            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RETRによるメール取得_失敗(InetKind inetKind) {
            //setUp
            var sut = CreatePopClient(inetKind);
            var expected = false;

            //exercise
            sut.Connect();
            sut.Login("user1", "user1");

            var mail = new Mail();
            var actual = sut.Retr(0, mail); //user1は滞留が0通なので、存在しないメールをリクエストしている

            //verify
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(sut.GetLastError(), Is.EqualTo("Not Found +OK in PopClient RecvStatus()"));
            //tearDown
            sut.Dispose();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DELEによるメール削除(InetKind inetKind) {
            //setUp
            var sut = CreatePopClient(inetKind);
            var expected = true;

            //exercise
            sut.Connect();
            sut.Login("user2", "user2");

            //verify
            sut.Dele(0);//1通削除
            Assert.That(CountMail("user2"), Is.EqualTo(2));//QUIT前は２通

            sut.Quit();
            Assert.That(CountMail("user2"), Is.EqualTo(1));//QUIT後は１通



            //tearDown
            sut.Dispose();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DELEによるメール削除_失敗(InetKind inetKind) {
            //setUp
            var sut = CreatePopClient(inetKind);
            var expected = false;

            //exercise
            sut.Connect();
            sut.Login("user1", "user1");

            //verify
            var actual = sut.Dele(0);
            Assert.That(actual,Is.EqualTo(expected));
            Assert.That(sut.GetLastError(), Is.EqualTo("Not Found +OK in PopClient RecvStatus()"));


            //tearDown
            sut.Quit();
            sut.Dispose();
        }

        //メール通数の確認
        int CountMail(String user){
            //メールボックス内に蓄積されたファイル数を検証する
            var path = String.Format("c:\\tmp2\\bjd5\\SmtpServerTest\\mailbox\\{0}",user);
            var di = new DirectoryInfo(path);

            //DF_*がn個存在する
            var files = di.GetFiles("DF_*");
            return files.Count();
        }
        public bool IsLife() {
            return true;
        }

    }
}
