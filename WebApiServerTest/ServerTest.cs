using System.Text;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using WebApiServer;

namespace WebApiServerTest {
    [TestFixture]
    internal class ServerTest : ILife {

        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ

        [TestFixtureSetUp]
        public static void BeforeClass(){

            TestUtil.CopyLangTxt();//BJD.Lang.txt


            //設定ファイルの退避と上書き
            _op = new TmpOption("WebApiServerTest", "WebApiServerTest.ini");
            var kernel = new Kernel();
            var option = kernel.ListOption.Get("WebApi");
            var conf = new Conf(option);

            //サーバ起動
            _v4Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
            _v4Sv.Start();

            _v6Sv = new Server(kernel, conf, new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
            _v6Sv.Start();

        }

        [TestFixtureTearDown]
        public static void AfterClass(){

            //サーバ停止
            _v4Sv.Stop();
            _v6Sv.Stop();

            _v4Sv.Dispose();
            _v6Sv.Dispose();

            //設定ファイルのリストア
            _op.Dispose();

        }

        //クライアントの生成
        SockTcp CreateClient(InetKind inetKind) {
            var port = 5050;
            if (inetKind == InetKind.V4) {
                return Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), port, 10, null);
            }
            return Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), port, 10, null);
        }


        [Test]
        public void ステータス情報_ToString_の出力確認_V4() {
            //setUP
            var sv = _v4Sv;
            var expected = "+ サービス中 \t              WebApi\t[127.0.0.1\t:TCP 5050]\tThread";
            //exercise
            var actual = sv.ToString().Substring(0, 58);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V6() {
            //setUP
            var sv = _v6Sv;
            var expected = "+ サービス中 \t              WebApi\t[::1\t:TCP 5050]\tThread";
            //exercise
            var actual = sv.ToString().Substring(0, 52);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Test(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "{\"code\":500,\"message\":\"Not Implemented []\"}";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\n\n"));

            var buf = cl.Recv(3000, 3, this);

            var str = Encoding.UTF8.GetString(buf);
            var actual = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();


        }


        public bool IsLife(){
            return true;
        }
    }
}

