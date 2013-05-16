using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using ProxyHttpServer;

namespace ProxyHttpServerTest {
    class ProxyFtpTest : ILife{
        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ

        [TestFixtureSetUp]
        public static void BeforeClass() {

            //srcDir = string.Format("{0}\\ProxyHttpServerTest", TestUtil.ProhjectDirectory());

            //設定ファイルの退避と上書き
            _op = new TmpOption("ProxyHttpServerTest", "ProxyHttpServerTest.ini");
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
        public void HTTP経由のFTPサーバへのアクセス() {


            //setUp
            
            var cl = Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), 8080, 10, null);

            //cl.Send(Encoding.ASCII.GetBytes("GET ftp://ftp.iij.ad.jp/ HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n"));
            cl.Send(Encoding.ASCII.GetBytes("GET ftp://ftp.jaist.ac.jp/ HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n"));

            //exercise
            var lines = Inet.RecvLines(cl, 20, this);
            //verify
            Assert.That(lines[0], Is.EqualTo("HTTP/1.0 200 OK"));

        }

        public bool IsLife(){
            return true;
        }
    }
}
