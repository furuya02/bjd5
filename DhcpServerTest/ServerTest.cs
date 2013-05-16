using System.Net.Sockets;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using BjdTest.test;
using NUnit.Framework;
using DhcpServer;
using Bjd;
using System.Net;

namespace DhcpServerTest {
    
    [TestFixture]
    public class ServerTest {


        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _sv; //サーバ


        [SetUp]
        public void Setup(){
            //設定ファイルの退避と上書き
            _op = new TmpOption("DhcpServerTest","DhcpServerTest.ini");
            OneBind oneBind = new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Udp);
            Kernel kernel = new Kernel();
            var option = kernel.ListOption.Get("Dhcp");
            Conf conf = new Conf(option);

            //サーバ起動
            _sv = new Server(kernel, conf, oneBind);
            _sv.Start();

        }
        [TearDown]
        public void TearDown() {
            //サーバ停止
            _sv.Stop();
            _sv.Dispose();

            //設定ファイルのリストア
            _op.Dispose();
        }

        PacketDhcp Access(byte [] buf) {
            //クライアントソケット生成、及び送信
            var cl = new UdpClient(68);
            cl.Connect((new Ip(IpKind.V4Localhost)).IPAddress, 67); //クライアントのポートが67でないとサーバが応答しない
            cl.Send(buf,buf.Length);
            
            //受信
            var ep = new IPEndPoint(0, 0);
            var recvBuf = cl.Receive(ref ep);
            if (recvBuf.Length == 0) {
                Assert.Fail();//受信データが無い場合
            }
            var rp = new PacketDhcp();
            rp.Read(recvBuf);

            cl.Close();
            return rp;
        }
        
        [Test]
        public void ステータス情報_ToString_の出力確認() {

            var expected = "+ サービス中 \t                Dhcp\t[127.0.0.1\t:UDP 67]\tThread";

            //exercise
            var actual = _sv.ToString().Substring(0, 56);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }


        [Test]
        public void ConnectTest() {
            const ushort id = 100;
            var requestIp = new Ip("127.0.0.1");
            var serverIp = new Ip("127.0.0.1");
            var mac = new Mac("11-22-33-44-55-66");
            var maskIp = new Ip("255.255.255.0");
            var gwIp = new Ip("255.255.255.0");
            var dnsIp0 = new Ip("255.255.255.0");
            var dnsIp1 = new Ip("255.255.255.0");
            var sp = new PacketDhcp(id, requestIp, serverIp, mac, DhcpType.Discover, 3600, maskIp, gwIp, dnsIp0, dnsIp1, "");
            
            var bytes = sp.GetBuffer();
            bytes[0] = 1;//Opecode = 2->1

            var rp = Access(bytes);
            Assert.AreEqual(rp.Type,DhcpType.Offer);
        }

        [Test]
        public void Connect2Test() {
            const ushort id = 100;
            var requestIp = new Ip("0.0.0.0");
            var serverIp = new Ip("127.0.0.1");
            var mac = new Mac("11-22-33-44-55-66");
            var maskIp = new Ip("255.255.255.0");
            var gwIp = new Ip("255.255.255.0");
            var dnsIp0 = new Ip("255.255.255.0");
            var dnsIp1 = new Ip("255.255.255.0");
            var sp = new PacketDhcp(id, requestIp, serverIp, mac, DhcpType.Request, 3600, maskIp, gwIp, dnsIp0, dnsIp1, "");

            var bytes = sp.GetBuffer();
            bytes[0] = 1;//Opecode = 2->1

            var rp = Access(bytes);
            Assert.AreEqual(rp.Type, DhcpType.Nak);
        }

        [TestCase("192.168.2.1", "11-22-33-44-55-66",DhcpType.Offer)]
        [TestCase("0.0.0.0", "ff-ff-ff-ff-ff-ff", DhcpType.Offer)]
        public void RequestTest(string requestIpStr, string macStr, DhcpType ans) {
            const ushort id = 100;
            var requestIp = new Ip(requestIpStr);
            var serverIp = new Ip("127.0.0.1");
            var mac = new Mac(macStr);
            var maskIp = new Ip("255.255.255.0");
            var gwIp = new Ip("0.0.0.0");
            var dnsIp0 = new Ip("0.0.0.0");
            var dnsIp1 = new Ip("0.0.0.0");
            var sp = new PacketDhcp(id, requestIp, serverIp, mac, DhcpType.Discover, 3600, maskIp, gwIp, dnsIp0, dnsIp1, "");

            var bytes = sp.GetBuffer();
            bytes[0] = 1;//Opecode = 2->1

            var rp = Access(bytes);
            Assert.AreEqual(rp.Type, ans);
        }
    }
}
