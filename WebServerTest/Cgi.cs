using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServerTest {
    [TestFixture]
    class CgiTest {

        Server server;
        readonly string hostName = "localhost";
        readonly int port = 88;
        readonly string nameTag = "Web-localhost:88";

        //Test用Util
        UtilDir utilDir;
        UtilOption utilOption;


        [SetUp]
        public void SetUp() {

            utilDir = new UtilDir();
            utilOption = new UtilOption(utilDir);//オプション設定
            //ListOption ListServerの初期化を成功させるためには、Kernel生成の前にutilDirを生成する必要がある
            Kernel kernel = new Kernel(null, null, null, null, null);

            //サーバ起動
            OneBind oneBind = new OneBind(new Ip("127.0.0.1"), ProtocolKind.Tcp);
            server = new Server(kernel, nameTag, oneBind);
            server.Start();

        }
        [TearDown]
        public void TearDown() {

            server.Stop();//サーバ停止
            utilOption.Dispose();//オプション書き戻し

        }


        [Test]
        public void Status_Test() {

            var s = string.Format("+ サービス中 \t{0}    \t[127.0.0.1\t:TCP {1}]\tThread 0/10", nameTag, port);

            Assert.AreEqual(server.ToString(), s);
        }

        [Test]
        public void Connect_Test() {

            TcpClient tcp = new TcpClient(hostName, port);
            Assert.AreEqual(tcp.Connected, true);
            tcp.Close();
        }


        [Test]
        public void Http10_Test() {

            UtilClient cl = new UtilClient(hostName, port);
            cl.Send("GET / HTTP/1.0\n\n");
            var s = cl.Recv();
            Assert.AreEqual(s, "HTTP/1.0 200 Document follows\r\n");
            cl.Dispose();
        }

        [Test]
        public void Http11_Test() {

            UtilClient cl = new UtilClient(hostName, port);
            cl.Send("GET / HTTP/1.1\n\n");
            var s = cl.Recv();
            Assert.AreEqual(s, "HTTP/1.1 400 Missing Host header or incompatible headers detected.\r\n");
            cl.Dispose();
        }

    }

}
