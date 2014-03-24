using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using Pop3Server;

namespace Pop3ServerTest {
    class ServerTest : ILife {

        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ

        //        [TestFixtureSetUp]
        //        public static void BeforeClass() {
        //        }
        //
        //        [TestFixtureTearDown]
        //        public static void AfterClass() {
        //        }


        [SetUp]
        public void SetUp() {
            //MailBoxは、Pop3ServerTest.iniの中で「c:\tmp2\bjd5\Pop3ServerTest\mailbox」に設定されている
            //また、上記のMaloBoxには、user1=0件　user2=2件　のメールが着信している

            //設定ファイルの退避と上書き
            _op = new TmpOption("Pop3ServerTest", "Pop3ServerTest.ini");
            var kernel = new Kernel();
            var option = kernel.ListOption.Get("Pop3");
            var conf = new Conf(option);

            //サーバ起動
            _v4Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
            _v4Sv.Start();

            _v6Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
            _v6Sv.Start();

            //メールボックスへのデータセット
            var srcDir = @"c:\tmp2\bjd5\Pop3ServerTest\";
            var dstDir = @"c:\tmp2\bjd5\Pop3ServerTest\mailbox\user2\";
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
            Directory.Delete(@"c:\tmp2\bjd5\Pop3ServerTest\mailbox", true);
        }

        //クライアントの生成
        SockTcp CreateClient(InetKind inetKind) {
            int port = 9110;
            if (inetKind == InetKind.V4) {
                return Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), port, 10, null);
            }
            return Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), port, 10, null);

        }

        //共通処理(バナーチェック)  Resharperのバージョンを吸収
        private void CheckBanner(string str) {
            //テストの際は、バージョン番号はテストツール（ReSharper）のバージョンになる
            const string bannerStr1 = "+OK BlackJumboDog (Version 7.1.2000.1478) ready <";
            const string bannerStr2 = "+OK BlackJumboDog (Version 7.1.1000.900) ready <";
            const string bannerStr3 = "+OK BlackJumboDog (Version 8.0.2000.2660) ready <";
            const string bannerStr4 = "+OK BlackJumboDog (Version 8.2.0.2160) ready <";


            //Assert.That(_v6cl.StringRecv(3, this), Is.EqualTo(BannerStr));

            if (str.IndexOf(bannerStr1) != 0 && str.IndexOf(bannerStr2) != 0 && str.IndexOf(bannerStr3) != 0 && str.IndexOf(bannerStr4) != 0)
            {
                Assert.Fail();
            }
        }

        //共通処理(ログイン成功)
        //ユーザ名、メール蓄積数、蓄積サイズ
        void Login(string userName, string password, int n, int size, SockTcp cl) {
            CheckBanner(cl.StringRecv(3, this));//バナーチェック

            cl.StringSend(string.Format("USER {0}", userName));
            Assert.That(cl.StringRecv(3, this), Is.EqualTo(string.Format("+OK Password required for {0}.\r\n", userName)));
            cl.StringSend(string.Format("PASS {0}", password));
            Assert.That(cl.StringRecv(10, this), Is.EqualTo(string.Format("+OK {0} has {1} message ({2} octets).\r\n", userName, n, size)));
        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V4() {
            //setUp
            var sv = _v4Sv;
            var expected = "+ サービス中 \t                Pop3\t[127.0.0.1\t:TCP 9110]\tThread";

            //exercise
            var actual = sv.ToString().Substring(0, 58);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V6() {

            //setUp
            var sv = _v6Sv;
            var expected = "+ サービス中 \t                Pop3\t[::1\t:TCP 9110]\tThread";

            //exercise
            var actual = sv.ToString().Substring(0, 52);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void パスワード認証成功(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);

            //exercise verify
            CheckBanner(cl.StringRecv(10, this));//バナーチェック
            cl.StringSend("user user1");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("+OK Password required for user1.\r\n"));
            cl.StringSend("PASS user1");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("+OK user1 has 0 message (0 octets).\r\n"));
            cl.StringSend("QUIT");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("+OK Pop Server at localhost signing off.\r\n"));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 複数ログイン(InetKind inetKind) {
            //setUp
            var cl1 = CreateClient(inetKind);
            var cl2 = CreateClient(inetKind);
            var cl3 = CreateClient(inetKind);
            var expected = "+OK 2 message (633 octets)\r\n";

            //exercise
            Login("user1", "user1", 0, 0, cl1);
            Login("user2", "user2", 2, 633, cl2);
            Login("user3", "user3", 0, 0, cl3);
            cl2.StringSend("UIDL");
            var actual = cl2.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl1.StringSend("QUIT");
            cl1.Close();
            cl2.StringSend("QUIT");
            cl2.Close();
            cl3.StringSend("QUIT");
            cl3.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 多重ログイン(InetKind inetKind) {
            //setUp
            var clDmy = CreateClient(inetKind);
            Login("user1", "user1", 0, 0, clDmy);
            var cl = CreateClient(inetKind);
            var expected = "-ERR Double login\r\n";

            //exercise
            CheckBanner(cl.StringRecv(3, this));//バナーチェック
            cl.StringSend("user user1");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("+OK Password required for user1.\r\n"));
            cl.StringSend("PASS user1");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            clDmy.StringSend("QUIT");
            clDmy.Close();
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void CHPSによるパスワード変更_文字数不足による失敗(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR The number of letter is not enough.\r\n";

            //exercise
            Login("user1", "user1", 0, 0, cl);
            cl.StringSend("CHPS abc");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void CHPSによるパスワード変更成功(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            //exercise verify
            Login("user1", "user1", 0, 0, cl);
            cl.StringSend("CHPS ABCabc#123"); //パスワード変更
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("+OK Password changed.\r\n"));
            cl.StringSend("QUIT"); //コネクション終了

            cl = CreateClient(inetKind); //再接続
            Login("user1", "ABCabc#123", 0, 0, cl); //変更後のパスワードでログインする

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void パスワード認証失敗(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            string expected = null;

            //exercise
            CheckBanner(cl.StringRecv(3, this));//バナーチェック
            cl.StringSend("user user1");
            Assert.That(cl.StringRecv(3, this), Is.EqualTo("+OK Password required for user1.\r\n"));
            cl.StringSend("PASS xxx");
            var actual = cl.StringRecv(3, this);

            //verify
            //パスワードに誤りがある場合、ブルートフォース対策のためしばらくレスポンスが無い
            Assert.That(actual, Is.EqualTo(expected));



            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void USERの前にPASSコマンドを送るとエラーが返る(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Invalid command.\r\n";

            //exercise
            CheckBanner(cl.StringRecv(3, this));//バナーチェック
            cl.StringSend("PASS user1");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void APOP認証成功(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK user1 has 0 message (0 octets).\r\n";

            //exercise
            var challengeStr = Inet.TrimCrlf(cl.StringRecv(3, this)).Split(' ')[5];
            var result = (new MD5CryptoServiceProvider()).ComputeHash(Encoding.ASCII.GetBytes(challengeStr + "user1"));
            var sb = new StringBuilder();
            for (int i = 0; i < 16; i++) {
                sb.Append(string.Format("{0:x2}", result[i]));
            }
            cl.StringSend("APOP user1 " + sb.ToString());
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));



            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void APOP認証失敗(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            string expected = null;

            //exercise
            var challengeStr = Inet.TrimCrlf(cl.StringRecv(3, this)).Split(' ')[5];
            var result = (new MD5CryptoServiceProvider()).ComputeHash(Encoding.ASCII.GetBytes(challengeStr + "user1"));
            var sb = new StringBuilder();
            for (int i = 0; i < 16; i++) {
                sb.Append(string.Format("{0:x2}", result[i]));
            }
            cl.StringSend("APOP user2 " + sb.ToString());
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }



        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 無効なコマンドでエラーが返る(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Invalid command.\r\n";

            //exercise
            CheckBanner(cl.StringRecv(3, this));//バナーチェック
            cl.StringSend("xxx");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 空行を送るとエラーが返る(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Invalid command.\r\n";

            //exercise
            Login("user1", "user1", 0, 0, cl);
            cl.StringSend("");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));


            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void LISTコマンドの応答_メール蓄積が無い場合(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);

            //exercise
            Login("user1", "user1", 0, 0, cl);
            cl.StringSend("LIST");
            var actual = Inet.RecvLines(cl, 3, this);

            //verify
            Assert.That(actual.Count, Is.EqualTo(2));
            Assert.That(actual[0], Is.EqualTo("+OK 0 message (0 octets)"));
            Assert.That(actual[1], Is.EqualTo("."));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void LISTコマンドの応答_メール蓄積がある場合(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("LIST");
            var actual = Inet.RecvLines(cl, 3, this);

            //verify
            Assert.That(actual.Count, Is.EqualTo(4));
            Assert.That(actual[0], Is.EqualTo("+OK 2 message (633 octets)"));
            Assert.That(actual[1], Is.EqualTo("1 317"));
            Assert.That(actual[2], Is.EqualTo("2 316"));
            Assert.That(actual[3], Is.EqualTo("."));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void LISTコマンドの有効パラメータの場合のレスポンス確認(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK 1 317\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("LIST 1");
            var actual = cl.StringRecv(3, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void LISTコマンドの無効パラメータの場合のレスポンス確認(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Message 3 does not exist.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("LIST 3");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RETRによるメール受信(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("RETR 1");
            var actual = Inet.RecvLines(cl, 3, this);

            //verify
            Assert.That(actual.Count, Is.EqualTo(13));
            Assert.That(actual[0], Is.EqualTo("+OK 317 octets"));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RETR_パラメータ無しによるエラー(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Too few arguments for the RETR command.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("RETR");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void RETR_無効パラメータによるエラー(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Message 3 does not exist.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("RETR 3"); //存在しないメール
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void NOOPコマンド(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("NOOP");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void STATコマンド(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK 2 633\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("STAT");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void STAT_パラメータ有りの場合の確認(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK 2 633\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("STAT 2");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void TOP_パラメータ無しによるエラー(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Too few arguments for the TOP command.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("TOP");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void TOP_無効パラメータの確認(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Too few arguments for the TOP 1 command.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("TOP 1");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void TOP_有効パラメータによるデータ取得(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("TOP 1 2");
            var actual = Inet.RecvLines(cl, 3, this);

            //verify
            Assert.That(actual.Count, Is.EqualTo(13));
            Assert.That(actual[0], Is.EqualTo("+OK 317 octets"));
            Assert.That(actual[5], Is.EqualTo("Message-ID: <bjd.00635026511425808252.000@example.com>"));
            Assert.That(actual[6], Is.EqualTo("From: <1@1>"));
            Assert.That(actual[12], Is.EqualTo("."));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void TOP_無効パラメータ_存在しないデータ(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Message 3 does not exist.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("TOP 3 2");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void UIDLコマンドの確認(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK 2 message (633 octets)\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("UIDL");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void UIDL_パラメータ有り(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK 1 bjd.00635026511425808252.000\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("UIDL 1");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void UIDL_無効パラメータ_無効データ(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Message 3 does not exist.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("UIDL 3");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DELEコマンドによるデータ削除成功(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK 317 octets\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("DELE 1");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DELEコマンド_無効パラメータによる失敗(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Message 3 does not exist.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("DELE 3");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DELEコマンド_無効パラメータによる失敗2(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "-ERR Invalid message number.\r\n";

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("DELE ABC");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DELEコマンドによるデータ削除成功後のメール数(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK 1 message (316 octets)\r\n"; //１通に減少する

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("DELE 1");
            cl.StringRecv(3, this);
            cl.StringSend("LIST");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void DELEコマンドによるデータ削除後のRSETによる復活(InetKind inetKind) {
            //setUp
            var cl = CreateClient(inetKind);
            var expected = "+OK 2 message (633 octets)\r\n"; // 最初の状態に戻る

            //exercise
            Login("user2", "user2", 2, 633, cl);
            cl.StringSend("DELE 1");
            cl.StringRecv(3, this);
            cl.StringSend("RSET");
            cl.StringRecv(3, this);
            cl.StringSend("LIST");
            var actual = cl.StringRecv(3, this);

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.StringSend("QUIT");
            cl.Close();
        }


        public bool IsLife() {
            return true;
        }
    }
}