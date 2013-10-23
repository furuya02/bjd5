using System;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using FtpServer;
using NUnit.Framework;

namespace FtpServerTest {
    [TestFixture]
    public class ServerTest : ILife {

        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ
        private SockTcp _v6Cl; //クライアント
        private SockTcp _v4Cl; //クライアント

        [TestFixtureSetUp]
        public static void BeforeClass() {

            //設定ファイルの退避と上書き
            _op = new TmpOption("FtpServerTest","FtpServerTest.ini");
            Kernel kernel = new Kernel();
            var option = kernel.ListOption.Get("Ftp");
            Conf conf = new Conf(option);

            //サーバ起動
            _v4Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
            _v4Sv.Start();

            _v6Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
            _v6Sv.Start();

        }

        [TestFixtureTearDown]
        public static void AfterClass() {

            //サーバ停止
            _v4Sv.Stop();
            _v6Sv.Stop();

            _v4Sv.Dispose();
            _v6Sv.Dispose();

            //設定ファイルのリストア
            _op.Dispose();

        }

        [SetUp]
        public void SetUp() {
            //クライアント起動
            _v4Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 21, 10, null);
            _v6Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), 21, 10, null);
            //クライアントの接続が完了するまで、少し時間がかかる
            //Thread.Sleep(10);

        }

        [TearDown]
        public void TearDown() {
            //クライアント停止
            _v4Cl.Close();
            _v6Cl.Close();
        }

        //共通処理(バナーチェック)  Resharperのバージョンを吸収
        private void CheckBanner(string str) {
            //テストの際は、バージョン番号はテストツール（ReSharper）のバージョンになる
            const string bannerStr0 = "220 FTP ( BlackJumboDog Version 7.1.2000.1478 ) ready\r\n";
            const string bannerStr1 = "220 FTP ( BlackJumboDog Version 7.1.1000.900 ) ready\r\n";
            const string bannerStr2 = "220 FTP ( BlackJumboDog Version 8.0.2000.2660 ) ready\r\n";
            //Assert.That(_v6cl.StringRecv(1, this), Is.EqualTo(BannerStr));
            if (str != bannerStr0 && str != bannerStr1 && str != bannerStr2)
            {
                Assert.Fail();
            }
        }


        //共通処理(ログイン成功)
        private void Login(string userName,SockTcp cl) {

            CheckBanner(cl.StringRecv(1, this));//バナーチェック

            cl.StringSend(string.Format("USER {0}", userName));
            Assert.That(cl.StringRecv(1, this), Is.EqualTo(string.Format("331 Password required for {0}.\r\n", userName)));
            cl.StringSend(string.Format("PASS {0}", userName));
            Assert.That(cl.StringRecv(10, this), Is.EqualTo(string.Format("230 User {0} logged in.\r\n", userName)));
        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V4() {

            var sv = _v4Sv;
            var expected = "+ サービス中 \t                 Ftp\t[127.0.0.1\t:TCP 21]\tThread";

            //exercise
            var actual = sv.ToString().Substring(0, 56);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V6() {

            var sv = _v6Sv;
            var expected = "+ サービス中 \t                 Ftp\t[::1\t:TCP 21]\tThread";

            //exercise
            var actual = sv.ToString().Substring(0, 50);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void パスワード認証成功_V4(){

            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("user user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("331 Password required for user1.\r\n"));
            cl.StringSend("PASS user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("230 User user1 logged in.\r\n"));

        }

        [Test]
        public void パスワード認証成功_V6() {

            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("user user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("331 Password required for user1.\r\n"));
            cl.StringSend("PASS user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("230 User user1 logged in.\r\n"));
        }

        [Test]
        public void アノニマス認証成功_V4() {

            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("USER Anonymous");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("331 Password required for Anonymous.\r\n"));
            cl.StringSend("PASS user@aaa.com");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("230 User Anonymous logged in.\r\n"));

        }

        [Test]
        public void アノニマス認証成功_V6() {

            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("USER Anonymous");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("331 Password required for Anonymous.\r\n"));
            cl.StringSend("PASS user@aaa.com");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("230 User Anonymous logged in.\r\n"));

        }

        [Test]
        public void アノニマス認証成功2_V4() {
            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("USER ANONYMOUS");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("331 Password required for ANONYMOUS.\r\n"));
            cl.StringSend("PASS xxx");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("230 User ANONYMOUS logged in.\r\n"));

        }

        [Test]
        public void アノニマス認証成功2_V6() {
            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("USER ANONYMOUS");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("331 Password required for ANONYMOUS.\r\n"));
            cl.StringSend("PASS xxx");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("230 User ANONYMOUS logged in.\r\n"));

        }

        [Test]
        public void パスワード認証失敗_V4() {
            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("USER user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("331 Password required for user1.\r\n"));
            cl.StringSend("PASS xxxx");
            Assert.That(cl.StringRecv(10, this), Is.EqualTo("530 Login incorrect.\r\n"));
        }

        [Test]
        public void パスワード認証失敗_V6() {
            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("USER user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("331 Password required for user1.\r\n"));
            cl.StringSend("PASS xxxx");
            Assert.That(cl.StringRecv(10, this), Is.EqualTo("530 Login incorrect.\r\n"));
        }

        [Test]
        public void USERの前にPASSコマンドを送るとエラーが返る_V4() {
            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック

            cl.StringSend("PASS user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("503 Login with USER first.\r\n"));

        }

        [Test]
        public void USERの前にPASSコマンドを送るとエラーが返る_V6() {
            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック

            cl.StringSend("PASS user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("503 Login with USER first.\r\n"));

        }

        [Test]
        public void パラメータが必要なコマンドにパラメータ指定が無かった場合エラーが返る_V4() {
            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("USER");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("500 USER: command requires a parameter.\r\n"));
        }

        [Test]
        public void パラメータが必要なコマンドにパラメータ指定が無かった場合エラーが返る_V6() {
            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("USER");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("500 USER: command requires a parameter.\r\n"));
        }

        [Test]
        public void 無効なコマンドでエラーが返る_V4() {
            var cl = _v4Cl;
            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("xxx");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("500 Command not understood.\r\n"));
        }

        [Test]
        public void 無効なコマンドでエラーが返る_V6() {
            var cl = _v6Cl;
            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("xxx");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("500 Command not understood.\r\n"));
        }

        [Test]
        public void 空行を送るとエラーが返る_V4() {
            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("500 Invalid command: try being more creative.\r\n"));
        }

        [Test]
        public void 空行を送るとエラーが返る_V6() {
            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("500 Invalid command: try being more creative.\r\n"));
        }

        [Test]
        public void 認証前に無効なコマンド_list_を送るとエラーが返る_V4() {
            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("LIST");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("530 Please login with USER and PASS.\r\n"));
        }

        [Test]
        public void 認証前に無効なコマンド_list_を送るとエラーが返る_V6() {
            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("LIST");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("530 Please login with USER and PASS.\r\n"));
        }

        [Test]
        public void 認証前に無効なコマンド_dele_を送るとエラーが返る_V4() {
            var cl = _v4Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("DELE");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("530 Please login with USER and PASS.\r\n"));
        }

        [Test]
        public void 認証前に無効なコマンド_dele_を送るとエラーが返る_V6() {
            var cl = _v6Cl;

            CheckBanner(cl.StringRecv(1, this));//バナーチェック
            cl.StringSend("DELE");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("530 Please login with USER and PASS.\r\n"));
        }

        [Test]
        public void 認証後にUSERコマンドを送るとエラーが返る_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //user
            cl.StringSend("USER user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("530 Already logged in.\r\n"));

        }
        [Test]
        public void 認証後にUSERコマンドを送るとエラーが返る_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //user
            cl.StringSend("USER user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("530 Already logged in.\r\n"));

        }

        [Test]
        public void 認証後にPASSコマンドを送るとエラーが返る_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //pass
            cl.StringSend("PASS user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("530 Already logged in.\r\n"));

        }
        [Test]
        public void 認証後にPASSコマンドを送るとエラーが返る_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //pass
            cl.StringSend("PASS user1");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("530 Already logged in.\r\n"));

        }
        
        [Test]
        public void PWDコマンド_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //pwd
            cl.StringSend("PWD");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("257 \"/\" is current directory.\r\n"));

        }

        [Test]
        public void PWDコマンド_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //pwd
            cl.StringSend("PWD");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("257 \"/\" is current directory.\r\n"));

        }

        [Test]
        public void SYSTコマンド_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //syst
            cl.StringSend("SYST");

            // Assert.That(cl.StringRecv(1, this), Is.EqualTo("215 Microsoft Windows NT 6.2.9200.0\r\n"));
            Assert.That(cl.StringRecv(1, this).Substring(0, 26), Is.EqualTo("215 Microsoft Windows NT 6"));

        }

        [Test]
        public void SYSTコマンド_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //syst
            cl.StringSend("SYST");

            // Assert.That(cl.StringRecv(1, this), Is.EqualTo("215 Microsoft Windows NT 6.2.9200.0\r\n"));
            Assert.That(cl.StringRecv(1, this).Substring(0, 26), Is.EqualTo("215 Microsoft Windows NT 6"));

        }

        [Test]
        public void TYPEコマンド_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //type
            cl.StringSend("TYPE A");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 Type set 'A'\r\n"));
            cl.StringSend("TYPE I");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 Type set 'I'\r\n"));
            cl.StringSend("TYPE X");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("500 command not understood.\r\n"));

        }

        [Test]
        public void TYPEコマンド_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //type
            cl.StringSend("TYPE A");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 Type set 'A'\r\n"));
            cl.StringSend("TYPE I");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 Type set 'I'\r\n"));
            cl.StringSend("TYPE X");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("500 command not understood.\r\n"));

        }
        [Test]
        public void PORTコマンド() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            int port = 256; //テストの連続のためにPORTコマンドのテストとはポート番号をずらす必要がある
            cl.StringSend("PORT 127,0,0,1,0,256");
            SockTcp dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null,this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            dl.Close();
        }

        [Test]
        public void PORTコマンド_パラメータ誤り() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("PORT 127,3,x,x,1,0,256");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("501 Illegal PORT command.\r\n"));

        }

        [Test]
        public void PASVコマンド() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("PASV");

            //227 Entering Passive Mode. (127,0,0,1,xx,xx)
            string[] t = cl.StringRecv(1, this).Split(new[] { '(', ')' });
            string[] tmp = t[1].Split(',');
            int n = Convert.ToInt32(tmp[4]);
            int m = Convert.ToInt32(tmp[5]);
            int port = n * 256 + m;

            Thread.Sleep(10);
            SockTcp dl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), port, 10, null);
            Assert.That(dl.SockState, Is.EqualTo(SockState.Connect));
            dl.Close();
        }

        [Test]
        public void EPSVコマンド() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("EPSV");

            //229 Entering Extended Passive Mode. (|||xxxx|)
            var tmp = cl.StringRecv(1, this).Split('|');
            var port = Convert.ToInt32(tmp[3]);
            var dl = Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), port, 10, null);
            Assert.That(dl.SockState, Is.EqualTo(SockState.Connect));
            dl.Close();
        }

        [Test]
        public void EPRTコマンド() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            var port = 252; //テストの連続のためにPORTコマンドのテストとはポート番号をずらす必要がある
            cl.StringSend("EPRT |2|::1|252|");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V6Localhost), port,null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 EPRT command successful.\r\n"));

            dl.Close();
        }

        [Test]
        public void EPORTコマンド_パラメータ誤り() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("EPRT |x|");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("501 Illegal EPRT command.\r\n"));

        }

        [Test]
        public void MKD_RMDコマンド_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("MKD test");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("257 Mkd command successful.\r\n"));

            cl.StringSend("RMD test");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 Rmd command successful.\r\n"));
        }
        [Test]
        public void MKD_RMDコマンド_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("MKD test");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("257 Mkd command successful.\r\n"));

            cl.StringSend("RMD test");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 Rmd command successful.\r\n"));
        }

        [Test]
        public void MKDコマンド_既存の名前を指定するとエラーとなる_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("MKD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("451 Mkd error.\r\n"));

        }

        [Test]
        public void MKDコマンド_既存の名前を指定するとエラーとなる_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("MKD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("451 Mkd error.\r\n"));

        }

        [Test]
        public void RMDコマンド_存在しない名前を指定するとエラーとなる_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("RMD test");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("451 Rmd error.\r\n"));

        }
        [Test]
        public void RMDコマンド_存在しない名前を指定するとエラーとなる_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("RMD test");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("451 Rmd error.\r\n"));
       
        }

        [Test]
        public void RETRコマンド_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //port
            var port = 250;
            cl.StringSend("PORT 127,0,0,1,0,250");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null,this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //retr
            cl.StringSend("RETR 3.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("150 Opening ASCII mode data connection for 3.txt (24 bytes).\r\n"));
            Thread.Sleep(10);
            Assert.That(dl.Length(), Is.EqualTo(24));

            dl.Close();
        }


        [Test]
        public void RETRコマンド_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //port
            var port = 250;
            cl.StringSend("PORT 127,0,0,1,0,250");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //retr
            cl.StringSend("RETR 3.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("150 Opening ASCII mode data connection for 3.txt (24 bytes).\r\n"));
            Thread.Sleep(10);
            Assert.That(dl.Length(), Is.EqualTo(24));

            dl.Close();
        }
        [Test]
        public void STOR_DELEマンド_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //port
            var port = 249;
            cl.StringSend("PORT 127,0,0,1,0,249");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //stor
            cl.StringSend("STOR 0.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("150 Opening ASCII mode data connection for 0.txt.\r\n"));

            dl.Send(new byte[3]);
            dl.Close();

            Assert.That(cl.StringRecv(1, this), Is.EqualTo("226 Transfer complete.\r\n"));

            //dele
            cl.StringSend("DELE 0.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 Dele command successful.\r\n"));

        }

        [Test]
        public void STOR_DELEマンド_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //port
            var port = 249;
            cl.StringSend("PORT 127,0,0,1,0,249");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //stor
            cl.StringSend("STOR 0.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("150 Opening ASCII mode data connection for 0.txt.\r\n"));

            dl.Send(new byte[3]);
            dl.Close();

            Assert.That(cl.StringRecv(1, this), Is.EqualTo("226 Transfer complete.\r\n"));

            //dele
            cl.StringSend("DELE 0.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 Dele command successful.\r\n"));

        }


        [Test]
        public void UPユーザはRETRに失敗する_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user2",cl);

            //port
            var port = 250;
            cl.StringSend("PORT 127,0,0,1,0,250");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //retr
            cl.StringSend("RETR 3.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));
            //		Thread.Sleep(10);
            //		Assert.That(dl.Length, Is.EqualTo(24));

            dl.Close();
        }



        [Test]
        public void UPユーザはRETRに失敗する_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user2",cl);

            //port
            var port = 250;
            cl.StringSend("PORT 127,0,0,1,0,250");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //retr
            cl.StringSend("RETR 3.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));
            //		Thread.Sleep(10);
            //		Assert.That(dl.Length, Is.EqualTo(24));

            dl.Close();
        }

        [Test]
        public void UPユーザはDELEに失敗する_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user2", cl);

            //dele
            cl.StringSend("DELE 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void UPユーザはDELEに失敗する_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user2", cl);

            //dele
            cl.StringSend("DELE 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }
        
        [Test]
        public void UPユーザはRNFR_RNTO_ファイル名変更_に失敗する_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user2", cl);

            cl.StringSend("RNFR 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

            cl.StringSend("RNTO $$$.1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void UPユーザはRNFR_RNTO_ファイル名変更_に失敗する_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user2", cl);

            cl.StringSend("RNFR 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

            cl.StringSend("RNTO $$$.1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void DOWNユーザはSTORに失敗する_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user3",cl);

            //port
            var port = 249;
            cl.StringSend("PORT 127,0,0,1,0,249");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //stor
            cl.StringSend("STOR 0.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void DOWNユーザはSTORに失敗する_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user3",cl);

            //port
            var port = 249;
            cl.StringSend("PORT 127,0,0,1,0,249");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //stor
            cl.StringSend("STOR 0.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void DOWNユーザはDELEに失敗する_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user3", cl);

            //dele
            cl.StringSend("DELE 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void DOWNユーザはDELEに失敗する_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user3", cl);

            //dele
            cl.StringSend("DELE 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void DOWNユーザはRETRに成功する_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user3", cl);

            //port
            var port = 250;
            cl.StringSend("PORT 127,0,0,1,0,250");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //retr
            cl.StringSend("RETR 3.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("150 Opening ASCII mode data connection for 3.txt (24 bytes).\r\n"));

            Thread.Sleep(10);
            Assert.That(dl.Length(), Is.EqualTo(24));

            dl.Close();
        }

        [Test]
        public void DOWNユーザはRETRに成功する_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user3", cl);

            //port
            var port = 250;
            cl.StringSend("PORT 127,0,0,1,0,250");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //retr
            cl.StringSend("RETR 3.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("150 Opening ASCII mode data connection for 3.txt (24 bytes).\r\n"));

            Thread.Sleep(10);
            Assert.That(dl.Length(), Is.EqualTo(24));

            dl.Close();
        }

        [Test]
        public void DOWNユーザはRNFR_RNTO_ファイル名変更_に失敗する_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user3", cl);

            cl.StringSend("RNFR 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

            cl.StringSend("RNTO $$$.1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void DOWNユーザはRNFR_RNTO_ファイル名変更_に失敗する_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user3", cl);

            cl.StringSend("RNFR 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

            cl.StringSend("RNTO $$$.1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 Permission denied.\r\n"));

        }

        [Test]
        public void DELEマンド_存在しない名前を指定するとエラーとなる_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //dele
            cl.StringSend("DELE 0.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("451 Dele error.\r\n"));

        }

        [Test]
        public void DELEマンド_存在しない名前を指定するとエラーとなる_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //dele
            cl.StringSend("DELE 0.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("451 Dele error.\r\n"));

        }
        
        [Test]
        public void LISTコマンド_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //port
            var port = 251;
            cl.StringSend("PORT 127,0,0,1,0,251");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //list
            cl.StringSend("LIST -la");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("150 Opening ASCII mode data connection for ls.\r\n"));

            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("drwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm home0\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("drwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm home1\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("drwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm home2\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("-rwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm 1.txt\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("-rwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm 2.txt\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("-rwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm 3.txt\r\n"));
            
            dl.Close();
        }

        [Test]
        public void LISTコマンド_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //port
            var port = 251;
            cl.StringSend("PORT 127,0,0,1,0,251");
            var dl = SockServer.CreateConnection(new Kernel(), new Ip(IpKind.V4Localhost), port, null, this);
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("200 PORT command successful.\r\n"));

            //list
            cl.StringSend("LIST -la");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("150 Opening ASCII mode data connection for ls.\r\n"));

            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("drwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm home0\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("drwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm home1\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("drwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm home2\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("-rwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm 1.txt\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("-rwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm 2.txt\r\n"));
            Assert.That(listMask(dl.StringRecv(3, this)), Is.EqualTo("-rwxrwxrwx 1 nobody nogroup nnnn mon dd hh:mm 3.txt\r\n"));

            dl.Close();
        }
        private string listMask(string str) {
            var tmp = str.Split(' ');
            return string.Format("{0} {1} {2} {3} nnnn mon dd hh:mm {4}", tmp[0], tmp[1], tmp[2], tmp[3], tmp[8]);
        }

        [Test]
        public void CWDコマンドで有効なディレクトリに移動_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //cwd
            cl.StringSend("CWD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 CWD command successful.\r\n"));

        }
        [Test]
        public void CWDコマンドで有効なディレクトリに移動_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //cwd
            cl.StringSend("CWD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 CWD command successful.\r\n"));

        }
        [Test]
        public void CWDコマンドで無効なディレクトリに移動しようとするとエラーが返る_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //cwd
            cl.StringSend("CWD xxx");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 xxx: No such file or directory.\r\n"));
            cl.StringSend("PWD");

        }
        [Test]
        public void CWDコマンドで無効なディレクトリに移動しようとするとエラーが返る_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //cwd
            cl.StringSend("CWD xxx");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 xxx: No such file or directory.\r\n"));
            cl.StringSend("PWD");

        }
        [Test]
        public void CWDコマンドでルートより上に移動しようとするとエラーが返る_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);


            //cwd
            cl.StringSend("CWD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 CWD command successful.\r\n"));
            cl.StringSend("CWD ..\\..");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 ..\\..: No such file or directory.\r\n"));

        }

        [Test]
        public void CWDコマンドでルートより上に移動しようとするとエラーが返る_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);


            //cwd
            cl.StringSend("CWD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 CWD command successful.\r\n"));
            cl.StringSend("CWD ..\\..");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("550 ..\\..: No such file or directory.\r\n"));

        }
        [Test]
        public void CDUPコマンド_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //cwd
            cl.StringSend("CWD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 CWD command successful.\r\n"));
            //cdup
            cl.StringSend("CDUP");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 CWD command successful.\r\n"));
            //pwd ルートに戻っていることを確認する
            cl.StringSend("PWD");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("257 \"/\" is current directory.\r\n"));

        }
        [Test]
        public void CDUPコマンド_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            //cwd
            cl.StringSend("CWD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 CWD command successful.\r\n"));
            //cdup
            cl.StringSend("CDUP");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 CWD command successful.\r\n"));
            //pwd ルートに戻っていることを確認する
            cl.StringSend("PWD");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("257 \"/\" is current directory.\r\n"));

        }

        [Test]
        public void RNFR_RNTOコマンド_ファイル名変更_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("RNFR 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("350 File exists, ready for destination name.\r\n"));

            cl.StringSend("RNTO $$$.1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 RNTO command successful.\r\n"));

            cl.StringSend("RNFR $$$.1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("350 File exists, ready for destination name.\r\n"));

            cl.StringSend("RNTO 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 RNTO command successful.\r\n"));
        }
        [Test]
        public void RNFR_RNTOコマンド_ファイル名変更_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("RNFR 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("350 File exists, ready for destination name.\r\n"));

            cl.StringSend("RNTO $$$.1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 RNTO command successful.\r\n"));

            cl.StringSend("RNFR $$$.1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("350 File exists, ready for destination name.\r\n"));

            cl.StringSend("RNTO 1.txt");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 RNTO command successful.\r\n"));
        }


        [Test]
        public void RNFR_RNTOコマンド_ディレクトリ名変更_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("RNFR home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("350 File exists, ready for destination name.\r\n"));

            cl.StringSend("RNTO $$$.home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 RNTO command successful.\r\n"));

            cl.StringSend("RNFR $$$.home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("350 File exists, ready for destination name.\r\n"));

            cl.StringSend("RNTO home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 RNTO command successful.\r\n"));
        }

        [Test]
        public void RNFR_RNTOコマンド_ディレクトリ名変更_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("RNFR home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("350 File exists, ready for destination name.\r\n"));

            cl.StringSend("RNTO $$$.home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 RNTO command successful.\r\n"));

            cl.StringSend("RNFR $$$.home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("350 File exists, ready for destination name.\r\n"));

            cl.StringSend("RNTO home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("250 RNTO command successful.\r\n"));
        }
        
        [Test]
        public void RMDコマンド_空でないディレクトリの削除は失敗する_V4() {
            var cl = _v4Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("RMD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("451 Rmd error.\r\n"));

        }

        [Test]
        public void RMDコマンド_空でないディレクトリの削除は失敗する_V6() {
            var cl = _v6Cl;

            //共通処理(ログイン成功)
            Login("user1", cl);

            cl.StringSend("RMD home0");
            Assert.That(cl.StringRecv(1, this), Is.EqualTo("451 Rmd error.\r\n"));

        }

        public bool IsLife() {
            return true;
        }
    }
}