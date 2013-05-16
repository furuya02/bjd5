using System.Text;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using WebServer;

namespace WebServerTest {
    [TestFixture]
    class ServerTest : ILife{


        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ

        [TestFixtureSetUp]
        public static void BeforeClass() {

            //設定ファイルの退避と上書き
            _op = new TmpOption("WebServerTest","WebServerTest.ini");
            var kernel = new Kernel();
            var option = kernel.ListOption.Get("Web-localhost:88");
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


        [Test]
        public void ステータス情報_ToString_の出力確認_V4() {

            var sv = _v4Sv;
            var expected = "+ サービス中 \t    Web-localhost:88\t[127.0.0.1\t:TCP 88]\tThread";

            //exercise
            var actual = sv.ToString().Substring(0, 56);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V6() {

            var sv = _v6Sv;
            var expected = "+ サービス中 \t    Web-localhost:88\t[::1\t:TCP 88]\tThread";

            //exercise
            var actual = sv.ToString().Substring(0, 50);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        
        [Test]
        public void Http10Test() {

            //setUp
            var _v4Cl = Inet.Connect(new Kernel(),new Ip(IpKind.V4Localhost),88, 10, null);
            var expected = "HTTP/1.0 200 Document follows\r\n";

            //exercise
            _v4Cl.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.0\n\n"));
            var buf = _v4Cl.LineRecv(3, this);
            var actual = Encoding.ASCII.GetString(buf);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDoen
            _v4Cl.Close();


        }

        [Test]
        public void Http11Test() {

            //setUp
            var _v4Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 88, 10, null);
            var expected = "HTTP/1.1 400 Missing Host header or incompatible headers detected.\r\n";

            //exercise
            _v4Cl.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\n\n"));
            var buf = _v4Cl.LineRecv(3, this);
            var actual = Encoding.ASCII.GetString(buf);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDoen
            _v4Cl.Close();

        }

        [TestCase("9.0")]
        [TestCase("1")]
        [TestCase("")]
        [TestCase("?")]
        public void サポート外バージョンのリクエストは処理されない(string ver) {

            //setUp
            var _v4Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 88, 10, null);
            string expected = null;

            //exercise
            _v4Cl.Send(Encoding.ASCII.GetBytes(string.Format("GET / HTTP/{0}\n\n",ver)));
            var actual = _v4Cl.LineRecv(3, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDoen
            _v4Cl.Close();


        }

        [TestCase("XXX")]
        [TestCase("")]
        [TestCase("?")]
        [TestCase("*")]
        public void 無効なプロトコルのリクエストは処理されない(string protocol) {

            //setUp
            var _v4Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 88, 10, null);
            string expected = null;

            //exercise
            _v4Cl.Send(Encoding.ASCII.GetBytes(string.Format("GET / {0}/1.0\n\n",protocol)));
            var actual = _v4Cl.LineRecv(3, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDoen
            _v4Cl.Close();
        }


        [TestCase("?")]
        [TestCase(",")]
        [TestCase(".")]
        [TestCase("aaa")]
        [TestCase("")]
        [TestCase("_")]
        [TestCase("????")]
        public void 無効なURIは処理されない(string uri) {

            //setUp
            var _v4Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 88, 10, null);
            string expected = null;

            //exercise
            _v4Cl.Send(Encoding.ASCII.GetBytes(string.Format("GET {0} HTTP/1.0\n\n",uri)));
            var actual = _v4Cl.LineRecv(3, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDoen
            _v4Cl.Close();
        }

        [TestCase("SET")]
        [TestCase("POP")]
        [TestCase("")]
        public void 無効なメソッドは処理されない(string method) {

            //setUp
            var _v4Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 88, 10, null);
            string expected = null;

            //exercise
            _v4Cl.Send(Encoding.ASCII.GetBytes(string.Format("{0} / HTTP/1.0\n\n", method)));
            var actual = _v4Cl.LineRecv(3, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDoen
            _v4Cl.Close();
        }


        [TestCase("GET / HTTP/111")]
        [TestCase("GET /")]
        [TestCase("GET")]
        [TestCase("HTTP/1.0")]
        [TestCase("XXX / HTTP/1.0")]
        [TestCase("GET_/_HTTP/1.0")]
        [TestCase("")]
        public void 無効なリクエストは処理されない(string reauest) {

            //setUp
            var _v4Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 88, 10, null);
            string expected = null;

            //exercise
            _v4Cl.Send(Encoding.ASCII.GetBytes(reauest));
            var actual = _v4Cl.LineRecv(3, this);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDoen
            _v4Cl.Close();
        }



        public bool IsLife() {
            return true;
        }
    }
}
