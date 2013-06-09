using System;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using System.Globalization;
using System.IO;
using WebServer;

namespace WebServerTest {
    [TestFixture]
    class SsiTest : ILife{

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


        string Date2Str(DateTime dt) {
            var culture = new CultureInfo("en-US", true);
            return dt.ToString("ddd M dd hh:mm:ss yyyy", culture);
        }

        [TestCase("ExecCgi.html", "100+200=300")]
        [TestCase("Include.html", "Hello world.(SSL Include)")]
        [TestCase("FSize.html", "179")]
        [TestCase("Echo.html", "DOCUMENT_NAME = Echo.html")]
        [TestCase("Echo.html", "LAST_MODIFIED = $")]
        [TestCase("Echo.html", "DATE_LOCAL = $")]
        [TestCase("Echo.html", "DATE_GMT = $")]
        [TestCase("Echo.html", "DOCUMENT_URI = $")]
        [TestCase("TimeFmt.html", "TIME_FMT = $")]
        [TestCase("Flastmod.html", "FLASTMOD = $")]
        //[TestCase("Echo.html", "QUERY_STRING_UNESCAPED = $")] //未実装
        public void SsiRequestTest(string fileName, string pattern) {

            var path = string.Format("{0}\\SsiTest\\Echo.html", _v4Sv.DocumentRoot);
            if (pattern == "LAST_MODIFIED = $") {
                pattern = string.Format("LAST_MODIFIED = {0}", Date2Str(File.GetLastWriteTime(path)));
            } else if (pattern == "DATE_LOCAL = $") {
                pattern = string.Format("DATE_LOCAL = {0}", Date2Str(DateTime.Now));
                pattern = pattern.Substring(0,25); //秒以降は判定しない
            } else if (pattern == "DATE_GMT = $") {
                pattern = string.Format("DATE_GMT = {0}", Date2Str(TimeZoneInfo.ConvertTimeToUtc(DateTime.Now)));
                pattern = pattern.Substring(0, 25); //秒以降は判定しない
            } else if (pattern == "DOCUMENT_URI = $") {
                pattern = string.Format("DOCUMENT_URI = {0}", path);
            } else if (pattern == "QUERY_STRING_UNESCAPED = $") {
                pattern = string.Format("QUERY_STRING_UNESCAPED = {0}", path);
            } else if (pattern == "TIME_FMT = $") {
                var dt = DateTime.Now;
                pattern = string.Format("TIME_FMT = {0:D2}.{1:D2}.{2:D4}", dt.Day, dt.Month, dt.Year);
            }else  if (pattern == "FLASTMOD = $") {
                pattern = string.Format("FLASTMOD = {0}", Date2Str(File.GetLastWriteTime(path)));
            }

            var cl = Inet.Connect(new Kernel(),new Ip(IpKind.V4Localhost), 88, 10, null);

            cl.Send(Encoding.ASCII.GetBytes(string.Format("GET /SsiTest/{0} HTTP/1.1\nHost: ws00\n\n", fileName)));
            int sec = 10; //CGI処理待ち時間（これで大丈夫?）
            var lines = Inet.RecvLines(cl, sec, this);
            var find = lines.Any(l => l.IndexOf(pattern) != -1);
            Assert.AreEqual(find, true, string.Format("not found {0}", pattern));

            cl.Close();

        }

        [Test]
        public void IncludeしたファイルがCGIファイルでない場合() {
            //SetUp

            var cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 88, 10, null);

            //exercise
            cl.Send(Encoding.ASCII.GetBytes(string.Format("GET /SsiTest/{0} HTTP/1.1\nHost: ws00\n\n", "Include2.html")));
            int sec = 30; //CGI処理待ち時間（これで大丈夫?）
            var lines = Inet.RecvLines(cl, sec, this);
            var expected = "<html>";
            var actual = lines[8];
            //verify
            Assert.That(actual,Is.EqualTo(expected));
            //TearDown
            cl.Close();

        }

        [Test]
        public void IncludeしたファイルがCGIファイルの場合() {
            //SetUp

            var cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 88, 10, null);

            //exercise
            cl.Send(Encoding.ASCII.GetBytes(string.Format("GET /SsiTest/{0} HTTP/1.1\nHost: ws00\n\n", "Include3.html")));
            int sec = 30; //CGI処理待ち時間（これで大丈夫?）
            var lines = Inet.RecvLines(cl, sec, this);
            var expected = "100+200=300";
            var actual = lines[8];
            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //TearDown
            cl.Close();

        }


        public bool IsLife(){
            return true;
        }
    }
}
