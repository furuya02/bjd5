using System.IO;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using NUnit.Framework;
using Newtonsoft.Json;
using WebApiServer;


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

            //メールボックスの初期化
            MailBoxBackup();
            MailBoxSetup();

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

            //メールボックスの終了処理
            MailBoxRestore();
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
        public void 全件取得(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = 7;

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = data.Count;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void owner指定で特定のユーザのメールを取得する(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = 3;

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?owner=user1 HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = data.Count;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void 複数owner指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = 5;

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?owner=user1,mqueue HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = data.Count;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void limitで取得が制限される(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = 4;

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?limit=4 HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = data.Count;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Fieldsでdateを指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "Fri, 20 Sep 2013 04:50:43 +0900";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?Fields=date HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = (string)data[0].date;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Fieldsでsizeを指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = 593;

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?Fields=size HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = (int)data[0].size;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Fieldsでallを指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "Received: from 127.0.0.1 ([127";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?fields=all&limit=1 HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = ((string)data[0].all).Substring(0, 30);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Fieldsでbodyを指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "$BK\\J8!J$=$N#1!K(B";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?fields=body&limit=1 HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = ((string)data[0].body).Substring(1,19);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Fieldsでuidを指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "bjd.00635152494430501309.000";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?fields=uid&limit=1 HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = (string)data[0].uid;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Fieldsでfilenameを指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "00635152494430601419";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?fields=filename&limit=1 HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = (string)data[0].filename;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }
        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Fieldsでsubjectを指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "テストメール（その１）";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message?Fields=subject HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = (string)data[0].subject;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Deleteによる全件メール削除(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = 0;

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("DELETE /mail/message HTTP/1.1\n\n"));
            var res = cl.Recv(3000, 10, this);
            cl.Close();
            cl = CreateClient(inetKind);

            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);

            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = data.Count;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();

            //メールボックスの初期化
            MailBoxSetup();

        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Deleteによるメール削除_owner指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = 5;

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("DELETE /mail/message?owner=user2 HTTP/1.1\n\n"));
            var res = cl.Recv(3000, 10, this);
            cl.Close();
            cl = CreateClient(inetKind);

            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);

            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = data.Count;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();

            //メールボックスの初期化
            MailBoxSetup();

        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void Deleteによるメール削除_limit指定(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = 4;

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("DELETE /mail/message?limit=3 HTTP/1.1\n\n"));
            var res = cl.Recv(3000, 10, this);
            cl.Close();
            cl = CreateClient(inetKind);

            cl.Send(Encoding.ASCII.GetBytes("GET /mail/message HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            
            
            dynamic d = JsonConvert.DeserializeObject(json);
            dynamic data = d.data;
            var actual = data.Count;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();

            //メールボックスの初期化
            MailBoxSetup();

        }

        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void serviceコマンドによる起動停止(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "{\"code\":200,\"message\":\"start service [control]\"}";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("PUT /mail/control?service=start HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);

            dynamic d = JsonConvert.DeserializeObject(json);
            //dynamic data = d.data;
            //var actual = data.Count;
            var actual = (string)json;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();

            //メールボックスの初期化
            MailBoxSetup();

        }


        [TestCase(InetKind.V4)]
        [TestCase(InetKind.V6)]
        public void responseによるレスポンス制御(InetKind inetKind) {

            //setUp
            var cl = CreateClient(inetKind);
            var expected = "{\"code\":200,\"message\":\"set 2 param [response]\"}";

            //exercise
            cl.Send(Encoding.ASCII.GetBytes("PUT /mail/response?mail=450&rcpt=452 HTTP/1.1\n\n"));
            var str = Encoding.UTF8.GetString(cl.Recv(3000, 10, this));
            var json = str.Substring(str.IndexOf("\r\n\r\n") + 4);
            dynamic d = JsonConvert.DeserializeObject(json);
            //dynamic data = d.data;
            //var actual = data.Count;
            var actual = (string)json;
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();

            //メールボックスの初期化
            MailBoxSetup();

        }


        public bool IsLife() {
            return true;
        }

        static void MailBoxSetup(){
            var testDir = TestUtil.ProjectDirectory() + "\\WebApiServerTest\\";
            var exeDir = TestUtil.ProjectDirectory() + "\\BJD\\out\\";
            //テスト用メールボックスをコピーする
            Copy(testDir + "mailbox", exeDir + "mailbox");
            Copy(testDir + "MailQueue", exeDir + "MailQueue");
        }

        static void MailBoxBackup() {
            var testDir = TestUtil.ProjectDirectory() + "\\WebApiServerTest\\";
            var exeDir = TestUtil.ProjectDirectory() + "\\BJD\\out\\";
            //メールボックスをバックアップする
            Move(exeDir + "mailbox", testDir + "mailbox.bak");
            Move(exeDir + "MailQueue", testDir + "MailQueue.bak");
        }

        private static void MailBoxRestore() {
            var testDir = TestUtil.ProjectDirectory() + "\\WebApiServerTest\\";
            var exeDir = TestUtil.ProjectDirectory() + "\\BJD\\out\\";
            //復旧
            Move(testDir + "mailbox.bak", exeDir + "mailbox");
            Move(testDir + "MailQueue.bak", exeDir + "MailQueue");
        }



        static void Copy(string src,string dst) {
            if (Directory.Exists(dst)) {
                Directory.Delete(dst, true);
            }
            Util.CopyDirectory(src, dst);
        }
        static void Move(string src, string dst) {
            if (Directory.Exists(dst)) {
                Directory.Delete(dst, true);
            }
            Directory.Move(src, dst);
        }

    }
}
