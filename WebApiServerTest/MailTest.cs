using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using WebApiServer;
using System.Windows.Forms;
using Newtonsoft.Json;


namespace WebApiServerTest{
    [TestFixture]
    public class MailTest : ILife{

        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _v6Sv; //サーバ
        private static Server _v4Sv; //サーバ

        [TestFixtureSetUp]
        public static void BeforeClass(){

            //設定ファイルの退避と上書き
            _op = new TmpOption("WebApiServerTest", "WebApiServerTest.ini");

            //MailBoxのみ初期化する特別なテスト用Kernelコンストラクタ
            var kernel = new Kernel("MailBox");
            var option = kernel.ListOption.Get("WebApi");
            var conf = new Conf(option);

            //メールボックスをバックアップする
            var src = string.Format("{0}\\mailbox", TestUtil.ProjectDirectory() + "\\BJD\\out");
            var dst = string.Format("{0}\\mailbox.bak", TestUtil.ProjectDirectory() + "\\WebApiServerTest");
            if (Directory.Exists(dst)){
                Directory.Delete(dst,true);
            }
            Directory.Move(src, dst);
            //メールボックスをコピーする
            src = string.Format("{0}\\mailbox", TestUtil.ProjectDirectory() + "\\WebApiServerTest");
            dst = string.Format("{0}\\mailbox", TestUtil.ProjectDirectory() + "\\BJD\\out");
            if (Directory.Exists(dst)) {
                Directory.Delete(dst, true);
            }
            Directory.Copy(src, dst);
            


          


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
        public void SetUp(){
        }

        [TearDown]
        public void TearDown(){
        }
        
        //クライアントの生成
        SockTcp CreateClient(InetKind inetKind) {
            var port = 5050;
            if (inetKind == InetKind.V4){
                return Inet.Connect(new Kernel(), new Ip(IpKind.V4Localhost), port, 10, null);
            }
            return Inet.Connect(new Kernel(), new Ip(IpKind.V6Localhost), port, 10, null);
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Test(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "TEST";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?fields=subject,to,size HTTP/1.0\n\n"));
            
            var buf = cl.Recv(3000,10, this);
            var actual = Encoding.UTF8.GetString(buf);


            dynamic d = JsonConvert.DeserializeObject(actual);
            dynamic dd = d.data;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();


        }

        public bool IsLife() {
            return true;
        }
    }
}
