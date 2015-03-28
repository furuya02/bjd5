using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using BjdTest.test;
using NUnit.Framework;
using Bjd.ctrl;

namespace BjdTest.server{

    [TestFixture]
    public class OneServerTest{
        private class MyServer : OneServer{
            public MyServer(Conf conf, OneBind oneBind) : base(new Kernel(), conf, oneBind){

            }

            public override string GetMsg(int messageNo){
                return "";
            }

            protected override void OnStopServer(){
            }

            protected override bool OnStartServer(){
                return true;
            }

            protected override void OnSubThread(SockObj sockObj){
                while (IsLife()){
                    Thread.Sleep(0); //これが無いと、別スレッドでlifeをfalseにできない

                    if (sockObj.SockState != Bjd.sock.SockState.Connect){
                        Console.WriteLine(@">>>>>sockAccept.getSockState()!=SockState.CONNECT");
                        break;
                    }
                }
            }
            //RemoteServerでのみ使用される
            public override void Append(OneLog oneLog) {

            }

            protected override void CheckLang(){}
        }

        internal class MyClient{
            private Socket _s = null;
            private readonly String _addr;
            private readonly int _port;
            private Thread _t;
            private bool _life;

            public MyClient(String addr, int port){
                _addr = addr;
                _port = port;
            }

            public void Connet(){
                
                _life = true;
                _t = new Thread(Loop) { IsBackground = true };
                _t.Start();

                //接続完了まで少し時間が必要
                while (_s==null || !_s.Connected){
                    Thread.Sleep(2);
                }
                
            }
            void Loop() {
                _s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _s.Connect(_addr, _port);
                while (_life){
                    Thread.Sleep(2);
                }

            }

            public void Dispose(){
                //			try {
                //				s.shutdownInput();
                //				s.shutdownOutput();
                //				s.close();
                //			} catch (IOException e1) {
                //				e1.printStackTrace();
                //			}
                _life = false;
                while (_t.IsAlive){
                    Thread.Sleep(0);
                }
                _s.Close();
            }
        }

        [Test]
        public void start_stopの繰り返し_負荷テスト(){

            var ip = new Ip(IpKind.V4Localhost);
            var oneBind = new OneBind(ip, ProtocolKind.Tcp);
            Conf conf = TestUtil.CreateConf("OptionSample");
            conf.Set("port", 9990);
            conf.Set("multiple", 10);
            conf.Set("acl", new Dat(new CtrlType[0]));
            conf.Set("enableAcl", 1);
            conf.Set("timeOut", 3);

            var myServer = new MyServer(conf, oneBind);

            for (var i = 0; i < 5; i++){
                myServer.Start();

                Assert.That(myServer.ThreadBaseKind, Is.EqualTo(ThreadBaseKind.Running));
                Assert.That(myServer.SockState(), Is.EqualTo(SockState.Bind));
                myServer.Stop();
                Assert.That(myServer.ThreadBaseKind, Is.EqualTo(ThreadBaseKind.After));
                Assert.That(myServer.SockState(), Is.EqualTo(SockState.Error));

            }

            myServer.Dispose();
        }

        [Test]
        public void start_stopの繰り返し_負荷テスト_UDP() {

            var ip = new Ip(IpKind.V4Localhost);
            var oneBind = new OneBind(ip, ProtocolKind.Udp);
            Conf conf = TestUtil.CreateConf("OptionSample");
            conf.Set("port", 9990);
            conf.Set("multiple", 10);
            conf.Set("acl", new Dat(new CtrlType[0]));
            conf.Set("enableAcl", 1);
            conf.Set("timeOut", 3);

            var myServer = new MyServer(conf, oneBind);

            for (var i = 0; i < 5; i++) {
                myServer.Start();

                Assert.That(myServer.ThreadBaseKind, Is.EqualTo(ThreadBaseKind.Running));
                Assert.That(myServer.SockState(), Is.EqualTo(SockState.Bind));
                myServer.Stop();
                Assert.That(myServer.ThreadBaseKind, Is.EqualTo(ThreadBaseKind.After));
                Assert.That(myServer.SockState(), Is.EqualTo(SockState.Error));

            }

            myServer.Dispose();
        }


        [Test]
        public void new及びstart_stop_disposeの繰り返し_負荷テスト(){

            var ip = new Ip(IpKind.V4Localhost);
            var oneBind = new OneBind(ip, ProtocolKind.Tcp);
            Conf conf = TestUtil.CreateConf("OptionSample");
            conf.Set("port", 88);
            conf.Set("multiple", 10);
            conf.Set("acl", new Dat(new CtrlType[0]));
            conf.Set("enableAcl", 1);
            conf.Set("timeOut", 3);

            for (var i = 0; i < 5; i++){
                var myServer = new MyServer(conf, oneBind);

                myServer.Start();
                Assert.That(myServer.ThreadBaseKind, Is.EqualTo(ThreadBaseKind.Running));
                Assert.That(myServer.SockState(),  Is.EqualTo(SockState.Bind));

                myServer.Stop();
                Assert.That(myServer.ThreadBaseKind,  Is.EqualTo(ThreadBaseKind.After));
                Assert.That(myServer.SockState(), Is.EqualTo(SockState.Error));

                myServer.Dispose();
            }
        }

        [Test]
        public void new及びstart_stop_disposeの繰り返し_負荷テスト_UDP() {

            var ip = new Ip(IpKind.V4Localhost);
            var oneBind = new OneBind(ip, ProtocolKind.Udp);
            Conf conf = TestUtil.CreateConf("OptionSample");
            conf.Set("port", 88);
            conf.Set("multiple", 10);
            conf.Set("acl", new Dat(new CtrlType[0]));
            conf.Set("enableAcl", 1);
            conf.Set("timeOut", 3);

            for (var i = 0; i < 5; i++) {
                var myServer = new MyServer(conf, oneBind);

                myServer.Start();
                Assert.That(myServer.ThreadBaseKind, Is.EqualTo(ThreadBaseKind.Running));
                Assert.That(myServer.SockState(), Is.EqualTo(SockState.Bind));

                myServer.Stop();
                Assert.That(myServer.ThreadBaseKind, Is.EqualTo(ThreadBaseKind.After));
                Assert.That(myServer.SockState(), Is.EqualTo(SockState.Error));

                myServer.Dispose();
            }
        }


        [Test]
        public void multipleを超えたリクエストは破棄される事をcountで確認する(){

            const int multiple = 5;
            const int port = 8889;
            const string address = "127.0.0.1";
            var ip = new Ip(address);
            var oneBind = new OneBind(ip, ProtocolKind.Tcp);
            Conf conf = TestUtil.CreateConf("OptionSample");
            conf.Set("port", port);
            conf.Set("multiple", multiple);
            conf.Set("acl", new Dat(new CtrlType[0]));
            conf.Set("enableAcl", 1);
            conf.Set("timeOut", 3);

            var myServer = new MyServer(conf, oneBind);
            myServer.Start();

            var ar = new List<MyClient>();

            for (int i = 0; i < 20; i++){
                var myClient = new MyClient(address, port);
                myClient.Connet();
                ar.Add(myClient);
            }
            Thread.Sleep(100);

            //multiple以上は接続できない
            Assert.That(myServer.Count(), Is.EqualTo(multiple));

            myServer.Stop();
            myServer.Dispose();

            foreach (var c in ar){
                c.Dispose();
            }
        }
    }
}
