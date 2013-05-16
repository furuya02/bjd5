using System.Threading;
using Bjd;
using Bjd.ctrl;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using BjdTest.test;
using NUnit.Framework;

namespace BjdTest.server{
    [TestFixture]
    internal class ServerTest{

        //サーバ動作確認用
        private class MyServer : OneServer{
            public MyServer(Conf conf, OneBind oneBind) : base(new Kernel(), conf, oneBind){
            }

            protected override bool OnStartServer(){
                return true;
            }

            protected override void OnStopServer(){
            }

            public override string GetMsg(int no){
                return "";
            }

            protected override void OnSubThread(SockObj sockObj){
                for (var i = 3; i >= 0 && IsLife(); i--){
                    if (sockObj.SockState != Bjd.sock.SockState.Connect){
                        //TestUtil.prompt(String.format("接続中...sockAccept.getSockState!=Connect"));
                        break;
                    }

                    //TestUtil.prompt(String.format("接続中...あと%d回待機", i));
                    Thread.Sleep(1000);
                }
            }
            //RemoteServerでのみ使用される
            public override void Append(OneLog oneLog) {

            }

        }

        [Test]
        public void A001(){
            var ip = new Ip(IpKind.V4Localhost);
            var oneBind = new OneBind(ip, ProtocolKind.Tcp);
            var conf = TestUtil.CreateConf("OptionSample");
            conf.Set("protocolKind", (int) ProtocolKind.Tcp);
            conf.Set("port", 8888);
            conf.Set("multiple", 10);
            conf.Set("acl", new Dat(new CtrlType[0]));
            conf.Set("enableAcl", 1);
            conf.Set("timeOut", 3);

            var myServer = new MyServer(conf, oneBind);
            myServer.Start();
            for (var i = 10; i > 0; i--){
                Thread.Sleep(1);
            }
            myServer.Dispose();
        }
    }
}
