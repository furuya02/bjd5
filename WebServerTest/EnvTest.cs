using System.Text;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using BjdTest.test;
using NUnit.Framework;
using WebServer;
using Bjd;
using System.Net;

namespace WebServerTest {
    [TestFixture]
    public class EnvTest {


        Kernel _kernel = new Kernel();
        private static TmpOption _op; //設定ファイルの上書きと退避
        private static OneOption option;


        [TestFixtureSetUp]
        public static void BeforeClass() {
            //設定ファイルの退避と上書き
            _op = new TmpOption("WebServerTest","WebServerTest.ini");
            var _kernel = new Kernel();
            option = _kernel.ListOption.Get("Web-localhost:88");
        }

        [TestFixtureTearDown]
        public static void AfterClass() {
            //設定ファイルのリストア
            _op.Dispose();
        }


        [TestCase("PATHEXT", ".COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC;.CPL")]
        [TestCase("WINDIR", "C:\\Windows")]
        [TestCase("COMSPEC", "C:\\Windows\\system32\\cmd.exe")]
        [TestCase("SERVER_SOFTWARE", "BlackJumboDog/8.0.2000.2660 (Windows)")]
        [TestCase("SystemRoot", "C:\\Windows")]
        public void OtherTest(string key, string val) {
            var request = new Request(null,null);
            var header = new Header();
            var tcpObj = new SockTcp(new Kernel(), new Ip(IpKind.V4_0), 88, 3,null);
            const string fileName = "";
            var env = new Env(_kernel,new Conf(option),request,header,tcpObj,fileName);
            foreach(var e in env){
                if(e.Key == key){
                    if (e.Key == "SERVER_SOFTWARE" && e.Val.IndexOf(".1478") > 0){
                        Assert.AreEqual(e.Val.ToLower(), "BlackJumboDog/7.1.2000.1478 (Windows)".ToLower());
                    } else{
                        Assert.AreEqual(e.Val.ToLower(), val.ToLower());
                    }
                    return;
                }
            }
            Assert.AreEqual(key,"");
        }

        [TestCase("DOCUMENT_ROOT", "D:\\work\\web")]
        [TestCase("SERVER_ADMIN", "root@localhost")]
        public void OptionTest(string key, string val) {
            var request = new Request(null,null);
            
            var conf = new Conf(option);
            conf.Set("documentRoot", val);

            var header = new Header();
            var tcpObj = new SockTcp(new Kernel(), new Ip("0.0.0.0"), 88, 1, null);
            const string fileName = "";
            var env = new Env(_kernel,conf, request, header,tcpObj, fileName);
            foreach (var e in env) {
                if (e.Key == key) {
                    Assert.AreEqual(e.Val, val);
                    return;
                }
            }
            Assert.AreEqual(key, "");

        }


        [TestCase("REMOTE_ADDR", "10.0.0.100")]
        [TestCase("REMOTE_PORT", "5000")]
        [TestCase("SERVER_ADDR", "127.0.0.1")]
        [TestCase("SERVER_PORT", "80")]
        public void TcpObjTest(string key, string val) {

            var conf = new Conf(option);
            var request = new Request(null,null);
            var header = new Header();
            var tcpObj = new SockTcp(new Kernel(), new Ip("0.0.0.0"), 88, 1,null);
            tcpObj.LocalAddress = new IPEndPoint((new Ip("127.0.0.1")).IPAddress,80);
            tcpObj.RemoteAddress = new IPEndPoint((new Ip("10.0.0.100")).IPAddress, 5000);
            const string fileName = "";
            var env = new Env(_kernel,conf, request, header, tcpObj, fileName);

            foreach (var e in env) {
                if (e.Key == key) {
                    Assert.AreEqual(e.Val, val);
                    return;
                }
            }
            Assert.AreEqual(key, "");
        }
        
        [TestCase("HTTP_ACCEPT_ENCODING", "gzip,deflate,sdch")]
        [TestCase("HTTP_ACCEPT_LANGUAGE", "ja,en-US;q=0.8,en;q=0.6")]
        [TestCase("HTTP_ACCEPT", "text/html,application/xhtml")]
        [TestCase("HTTP_USER_AGENT", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)")]
        [TestCase("HTTP_CONNECTION", "keep-alive")]
        public void HeaderTest(string key, string val) {

            
            var request = new Request(null,null);
            var header = new Header();
            header.Append("Connection", Encoding.ASCII.GetBytes("keep-alive"));
            header.Append("User-Agent", Encoding.ASCII.GetBytes("Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)"));
            header.Append("Accept", Encoding.ASCII.GetBytes("text/html,application/xhtml"));
            header.Append("Accept-Encoding", Encoding.ASCII.GetBytes("gzip,deflate,sdch"));
            header.Append("Accept-Language", Encoding.ASCII.GetBytes("ja,en-US;q=0.8,en;q=0.6"));
            header.Append("Accept-Charset", Encoding.ASCII.GetBytes("Shift_JIS,utf-8;q=0.7,*;q=0.3"));
            header.Append("Cache-Control", Encoding.ASCII.GetBytes("max-age=0"));
            
            var tcpObj = new SockTcp(new Kernel(), new Ip("0.0.0.0"), 88, 3,null);
            const string fileName = "";
            var env = new Env(_kernel,new Conf(option), request, header, tcpObj, fileName);
            foreach (var e in env) {
                if (e.Key == key) {
                    Assert.AreEqual(e.Val, val);
                    return;
                }
            }
            Assert.AreEqual(key, "");
        }


    }

}
