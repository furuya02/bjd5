using System.Text;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using System.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using ProxyHttpServer;


namespace ProxyHttpServerTest {
    
    [TestFixture]
    class ServerTest : ILife{


        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ

        private static string srcDir="";


        [TestFixtureSetUp]
        public static void BeforeClass(){

            TestUtil.CopyLangTxt();//BJD.Lang.txt

            srcDir = string.Format("{0}\\ProxyHttpServerTest", TestUtil.ProjectDirectory());

            //設定ファイルの退避と上書き
            _op = new TmpOption("ProxyHttpServerTest","ProxyHttpServerTest.ini");
            Kernel kernel = new Kernel();
            var option = kernel.ListOption.Get("ProxyHttp");
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
            var expected = "+ サービス中 \t           ProxyHttp\t[127.0.0.1\t:TCP 8888]\tThread";

            //exercise
            var actual = sv.ToString().Substring(0, 58);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ステータス情報_ToString_の出力確認_V6() {

            var sv = _v6Sv;
            var expected = "+ サービス中 \t           ProxyHttp\t[::1\t:TCP 8888]\tThread";

            //exercise
            var actual = sv.ToString().Substring(0, 52);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ConnectTest_V4からV4へのプロキシ() {


            //setUp
            //ダミーWebサーバ
            const int webPort = 778;
            var webRoot = string.Format("{0}\\public_html", srcDir);
            var tsWeb = new TsWeb(webPort, webRoot);//Webサーバ起動

            var cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 8888, 10, null);
            cl.Send(Encoding.ASCII.GetBytes("GET http://127.0.0.1:778/index.html HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n"));

            //exercise
            var lines = Inet.RecvLines(cl, 3, this);
            
            //verify
            Assert.That(lines.Count, Is.EqualTo(9));
            Assert.That(lines[0], Is.EqualTo("HTTP/1.1 200 OK"));
            Assert.That(lines[1], Is.EqualTo("Transfer-Encoding: chunked"));
            Assert.That(lines[2], Is.EqualTo("Server: Microsoft-HTTPAPI/2.0"));
            
            Assert.That(lines[4], Is.EqualTo(""));
            Assert.That(lines[5], Is.EqualTo("3"));
            Assert.That(lines[6], Is.EqualTo("123"));
            Assert.That(lines[7], Is.EqualTo("0"));
            Assert.That(lines[8], Is.EqualTo(""));


            //tearDown
            tsWeb.Dispose();//Webサーバ停止

        }

        [Test]
        public void ConnectTest_V6からV4へのプロキシ() {


            //setUp
            //ダミーWebサーバ
            const int webPort = 778;
            var webRoot = string.Format("{0}\\public_html", srcDir);
            var tsWeb = new TsWeb(webPort, webRoot);//Webサーバ起動

            var cl = Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), 8888, 10, null);
            cl.Send(Encoding.ASCII.GetBytes("GET http://127.0.0.1:778/index.html HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n"));

            //exercise
            var lines = Inet.RecvLines(cl, 3, this);

            //verify
            Assert.That(lines.Count, Is.EqualTo(9));
            Assert.That(lines[0], Is.EqualTo("HTTP/1.1 200 OK"));
            Assert.That(lines[1], Is.EqualTo("Transfer-Encoding: chunked"));
            Assert.That(lines[2], Is.EqualTo("Server: Microsoft-HTTPAPI/2.0"));

            Assert.That(lines[4], Is.EqualTo(""));
            Assert.That(lines[5], Is.EqualTo("3"));
            Assert.That(lines[6], Is.EqualTo("123"));
            Assert.That(lines[7], Is.EqualTo("0"));
            Assert.That(lines[8], Is.EqualTo(""));


            //tearDown
            tsWeb.Dispose();//Webサーバ停止

        }


        //外部SSLサーバへの接続試験
        [TestCase("www.facebook.com")]
        [TestCase("mail.google.com")]
        [TestCase("www.google.co.jp")]
        public void SslTest(string hostname) {

            //setUp
            var cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 8888, 10, null);
            cl.Send(Encoding.ASCII.GetBytes(string.Format("CONNECT {0}:443/ HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n", hostname)));

            //exercise
            var lines = Inet.RecvLines(cl, 3, this);

            //verify
            Assert.That(lines[0], Is.EqualTo("HTTP/1.0 200 Connection established"));

            //tearDown
            cl.Close();
        }
        
        //パフォーマンス測定
        [TestCase(5000)]
        [TestCase(1000)]
        [TestCase(30000)]
        //[TestCase(1000000000)]
        public void PerformanceTest(int count) {
            //ダミーWebサーバ
            const int webPort = 777;
            string webRoot = string.Format("{0}\\public_html", srcDir);

            //試験用ファイルの生成
            var fileName = Path.GetRandomFileName();
            var path = string.Format("{0}\\{1}", webRoot, fileName);
            var buf = new List<string>();
            for (int i = 0; i < count; i++) {
                buf.Add("ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ");
            }
            File.WriteAllLines(path,buf);

            var tsWeb = new TsWeb(webPort, webRoot);//Webサーバ起動

            //試験用クライアント

            var cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 8888, 10, null);
            
            //計測
            var sw = new Stopwatch();
            sw.Start();

            cl.Send(Encoding.ASCII.GetBytes(string.Format("GET http://127.0.0.1:777/{0} HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n",fileName)));
            var lines = Inet.RecvLines(cl, 3, this);

            //計測終了
            sw.Stop();
            Console.Write("HTTPProxy Performance : {0}ms LINES:{1}\n",sw.ElapsedMilliseconds,count);

            //作業ファイル削除
            File.Delete(path);
            if (lines != null) {
                Assert.AreEqual(lines[0], "HTTP/1.1 200 OK");
            } else {
                Assert.AreEqual(null, "receive faild");
            }
            cl.Close();//試験用クライアント破棄
            tsWeb.Dispose();//Webサーバ停止


        }


        /*
        試験用Webサーバへのリクエスト試験
        [Test]
        public void Web_Test() {
            byte[] buf = new byte[1024];
            
            //ダミーWebサーバ
            var tsDir = new TsDir();
            int webPort = 777;
            string webRoot = string.Format("{0}\\public_html",tsDir.Src);
            var tsWeb = new TsWeb(webPort, webRoot);//Webサーバ起動


            var tcp = new TcpClient("127.0.0.1", webPort); 
            NetworkStream ns = tcp.GetStream();

            byte[] sendBytes = Encoding.ASCII.GetBytes("GET /index.html HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");
             //リクエスト送信
            ns.Write(sendBytes, 0, sendBytes.Length);
            //受信
            int size = ns.Read(buf, 0, buf.Length);
            
            tcp.Close();

            List<string> lines = Inet.GetLines(Encoding.ASCII.GetString(buf,0,size));
            Assert.AreEqual(lines[0],"HTTP/1.1 200 OK");
            
            tsWeb.Dispose();//Webサーバ停止
            
        }
        */
        public bool IsLife(){
            return true;
        }
    }
}
