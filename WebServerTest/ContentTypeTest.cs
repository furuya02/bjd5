using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using WebServer;
using Bjd;
using BjdTest;

namespace WebServerTest {
    [TestFixture]
    public class ContentTypeTest {

        private static TmpOption _op; //設定ファイルの上書きと退避

        ContentType _contentType;

        [SetUp]
        public void SetUp() {

            //設定ファイルの退避と上書き
            _op = new TmpOption("WebServerTest","WebServerTest.ini");
            Kernel kernel = new Kernel();
            var option = kernel.ListOption.Get("Web-localhost:88");
            Conf conf = new Conf(option);

            _contentType = new ContentType(conf);

        }

        [TearDown]
        public void TearDown() {
            //設定ファイルのリストア
            _op.Dispose();
        }

        [TestCase("$$$", "application/octet-stream")]
        [TestCase("txT", "text/plain")]
        [TestCase("txt", "text/plain")]
        [TestCase("jpg", "image/jpeg")]
        public void ContentTypeGetTest(string ext, string typeText) {
            var fileName = string.Format("TEST.{0}",ext);
            var s = _contentType.Get(fileName);
            Assert.AreEqual(s,typeText);
        }




    }
}
