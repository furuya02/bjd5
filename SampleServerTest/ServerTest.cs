using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using SampleServer;

namespace SampleServerTest{
    [TestFixture]
    internal class ServerTest{

        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ
        private SockTcp _v6Cl; //クライアント
        private SockTcp _v4Cl; //クライアント

        [TestFixtureSetUp]
        public static void BeforeClass(){

            //設定ファイルの退避と上書き
            _op = new TmpOption("SampleServerTest","SampleServerTest.ini");
            var kernel = new Kernel();
            var option = kernel.ListOption.Get("Sample");
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

        [SetUp]
        public void SetUp() {
            //クライアント起動
            _v4Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 9999, 10, null);
            _v6Cl = Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), 9999, 10, null);

        }

        [TearDown]
        public void TearDown() {
            //クライアント停止
            _v4Cl.Close();
            _v6Cl.Close();
        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V4() {
            //setUP
            var sv = _v4Sv;
            var expected = "+ サービス中 \t              Sample\t[127.0.0.1\t:TCP 9999]\tThread";
            //exercise
            var actual = sv.ToString().Substring(0, 58);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V6() {
            //setUP
            var sv = _v6Sv;
            var expected = "+ サービス中 \t              Sample\t[::1\t:TCP 9999]\tThread";
            //exercise
            var actual = sv.ToString().Substring(0, 52);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

    }
}

