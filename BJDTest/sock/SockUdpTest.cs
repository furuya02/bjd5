using System;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.sock;
using NUnit.Framework;

namespace BjdTest.sock{
    //**************************************************
    // Echoサーバを使用したテスト
    //**************************************************

    [TestFixture]
    internal class SockUdpTest{
        private class EchoServer : ThreadBase{
            private readonly SockServer _sockServer;
            private readonly String _addr;
            private readonly int _port;
            private readonly Ssl _ssl = null;

            public EchoServer(String addr, int port) : base(null){
                _sockServer = new SockServer(new Kernel(),ProtocolKind.Udp,_ssl);
                _addr = addr;
                _port = port;
            }

            public override String GetMsg(int no){
                return null;
            }

            protected override bool OnStartThread(){
                return true;
            }


            protected override void OnStopThread(){
                _sockServer.Close();
            }

            protected override void OnRunThread(){
                Ip ip = null;
                try{
                    ip = new Ip(_addr);
                } catch (ValidObjException ex){
                    Assert.Fail(ex.Message);
                }
                if (_sockServer.Bind(ip, _port)){
                    //[C#]
                    ThreadBaseKind = ThreadBaseKind.Running;

                    while (IsLife()){

                        var child = (SockUdp) _sockServer.Select(this);
                        if (child == null){
                            break;
                        }
                        while (IsLife() && child.SockState == SockState.Connect) {
                            var len = child.Length();
                            if (len > 0){
                                byte[] buf = child.RecvBuf;
                                child.Send(buf);
                                //送信が完了したら、この処理は終了
                                break;
                            }
                        }
                    }
                }
            }
        }




        [Test]
        public void Echoサーバにsendしてlength分ずつRecvする(){
            //setUp
            const string addr = "127.0.0.1";
            const int port = 53;
            var echoServer = new EchoServer(addr, port);
            echoServer.Start();

            const int timeout = 3;

            const int max = 1500;
            const int loop = 10;
            var tmp = new byte[max];
            for (int i = 0; i < max; i++){
                tmp[i] = (byte) i;
            }

            var ip = new Ip(addr);
            for (var i = 0; i < loop; i++){
                var sockUdp = new SockUdp(new Kernel(), ip, port, null, tmp);
//                while (sockUdp.Length() == 0){
//                    Thread.Sleep(10);
//                }
                var b = sockUdp.Recv(timeout);

                //verify
                for (var m = 0; m < max; m += 10){
                    Assert.That(b[m], Is.EqualTo(tmp[m])); //送信したデータと受信したデータが同一かどうかのテスト
                }
                sockUdp.Close();
            }

            //TearDown
            echoServer.Stop();
        }
    }
}
