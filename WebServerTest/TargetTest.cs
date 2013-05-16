using Bjd;
using Bjd.net;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using WebServer;

namespace WebServerTest {
    [TestFixture]
    class TargetTest{
        private static OneOption option;
        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ

        [TestFixtureSetUp]
        public static void BeforeClass() {

            //設定ファイルの退避と上書き
            _op = new TmpOption("WebServerTest","WebServerTest.ini");
            Kernel kernel = new Kernel();

            option = kernel.ListOption.Get("Web-localhost:88");


            //サーバ起動
            _v4Sv = new Server(kernel, new Conf(option), new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
            _v4Sv.Start();

            _v6Sv = new Server(kernel, new Conf(option), new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
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


        }
        
        [TearDown]
        public void TearDown() {

            
        }

        //無効なドキュメントルート指定する
        [Test]
        public void DocumentRootTest() {
            Conf conf = new Conf(option);
            var sut = new Target(conf, null);

            //無効なドキュメントルートを設定する
            conf.Set("documentRoot", "q:\\");
            sut = new Target(conf, null);
            Assert.AreEqual(sut.DocumentRoot, null);


        }

        [TestCase("/index.html", "index.html")]
        [TestCase("/index2.html", "index2.html")]//存在しないファイルを指定
        [TestCase("/", "index.html")]// /で指定した時、welcomeファイルに設定しているファイルが存在する場合、そのファイルになる
        [TestCase("/test1/", "test1\\")] //test1には、ファイルが存在しない
        [TestCase("/test2/", "test2\\index.html")] // test2にはindex.htmlが存在する
        [TestCase("/test3/", "test3\\index.php")] // test3にはindex.phpが存在する
        [TestCase("/test4/", "test4\\index.html")] // test4にはindex.htmlとindex.phpが存在する
        public void FullPathTest(string uri, string path) {
            Conf conf = new Conf(option);
            var sut = new Target(conf, null);
            var fullPath = string.Format("{0}\\{1}", _v4Sv.DocumentRoot, path);
            
            sut.InitFromUri(uri);
            Assert.AreEqual(sut.FullPath,fullPath);
        }

        [TestCase("/index2.html", "index2.html")]//存在しないファイル
        [TestCase("/test4/index.html", "test4\\index.html")]//階層下のファイル
        [TestCase("/", "")]//フォルダの指定（※これ意味あるのか？）
        public void InitFromFileTest(string uri, string path) {
            Conf conf = new Conf(option);
            var sut = new Target(conf, null);
            var fullPath = string.Format("{0}\\{1}", _v4Sv.DocumentRoot, path);

            sut.InitFromFile(fullPath);
            Assert.AreEqual(sut.Uri, uri);
        }


       


    }
}
