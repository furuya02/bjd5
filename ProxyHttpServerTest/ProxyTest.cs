using Bjd.net;
using Bjd.sock;
using NUnit.Framework;
using Bjd;
using System.Net.Sockets;
using ProxyHttpServer;

namespace ProxyHttpServerTest {
    [TestFixture]
    class ProxyTest : ILife{
       
        Proxy _proxy;


        [SetUp]
        public void SetUp() {
            var kernel = new Kernel(null,null,null,null);
            var ip = new Ip("127.0.0.1");
            const int port = 0;
            Ssl ssl = null;
            var tcpObj = new SockTcp(new Kernel(), ip,port,3,ssl);
            var upperProxy = new UpperProxy(false, "", 0, null,false,"","");//上位プロキシ未使用
            const int timeout = 3;
            _proxy = new Proxy(kernel, null, tcpObj, timeout,upperProxy);
        }
        [TearDown]
        public void TearDown() {
        }
        [TestCase("127.0.0.1",8080)]
        public void Test(string host,int port) {

            //int port = 8080;
            //string host = "127.0.0.1";
            var ip = new Ip(host);
            var listener = new TcpListener(ip.IPAddress, port);
            listener.Start();

            _proxy.Connect(this, host, port, "TEST", ProxyProtocol.Http);

            Assert.AreEqual(_proxy.HostName,ip.ToString());
            Assert.AreEqual(_proxy.Port,port);

        }

        public bool IsLife(){
            return true;
        }
    }
}
