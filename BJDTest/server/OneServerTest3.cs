using System;
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
    internal class OneServerTest3 : ILife{
        private class EchoServer : OneServer{


            public EchoServer(Conf conf, OneBind oneBind) : base(new Kernel(), conf, oneBind){

            }

            public override string GetMsg(int no){
                return null;
            }

            protected override void OnStopServer(){
            }

            protected override bool OnStartServer(){
                return true;
            }

            protected override void OnSubThread(SockObj sockObj){
                //サーバ終了までキープする
                while (IsLife()){
                    Thread.Sleep(100); 
                }
            }
            //RemoteServerでのみ使用される
            public override void Append(OneLog oneLog) {

            }

        }

        EchoServer StartServer(int port,int enableAcl,Dat acl){
            var ip = TestUtil.CreateIp("127.0.0.1");
            const int timeout = 300;
            var oneBind = new OneBind(ip, ProtocolKind.Tcp);
            var conf = TestUtil.CreateConf("OptionSample");
            conf.Set("port", port);
            conf.Set("multiple", 10);
            conf.Set("acl", acl);
            conf.Set("enableAcl", enableAcl);
            conf.Set("timeOut", timeout);

            var sv = new EchoServer(conf, oneBind);
            sv.Start();
            return sv;
        }

        SockTcp StartClient(int port) {

            var ip = TestUtil.CreateIp("127.0.0.1");
            var cl = new SockTcp(new Kernel(), ip, port, 300, null);
            Thread.Sleep(300);
            return cl;
        }

        [Test]
        public void 許可リスト無し_のみ許可する_Deny() {

            //setUp
            const int port = 9987;
            const int enableAcl = 0; //指定したアドレスからのアクセスのみを許可する
            var acl = new Dat(new CtrlType[0]); //許可リストなし

            var sut = StartServer(port, enableAcl, acl);
            var cl = StartClient(port);
            var expected = 0; //　Deny

            //exercise
            var actual = sut.Count();

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
            sut.Stop();
            sut.Dispose();

        }

        
        [Test]
        public void 許可リスト無し_のみ禁止する_Allow(){
            //setUp
            const int port = 9988;
            const int enableAcl = 1; //指定したアドレスからのアクセスのみを禁止する
            var acl = new Dat(new CtrlType[0]); //許可リストなし

            var sut = StartServer(port, enableAcl, acl);
            var cl = StartClient(port);
            var expected = 1; //　Allow

            //exercise
            var actual = sut.Count();

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
            sut.Stop();
            sut.Dispose();
        }

        [Test]
        public void 許可リスト有り_のみ許可する_Allow() {

            //setUp
            const int port = 9987;
            const int enableAcl = 0; //指定したアドレスからのアクセスのみを許可する
            var acl = new Dat(new[]{CtrlType.TextBox, CtrlType.TextBox}); //許可リストあり
            acl.Add(true, "NAME\t127.0.0.1");


            var sut = StartServer(port, enableAcl, acl);
            var cl = StartClient(port);
            var expected = 1; //　Allow

            //exercise
            var actual = sut.Count();

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
            sut.Stop();
            sut.Dispose();

        }

        [Test]
        public void 許可リスト有り_のみ禁止する_Deny() {

            //setUp
            const int port = 9987;
            const int enableAcl = 1; //指定したアドレスからのアクセスのみを禁止する
            var acl = new Dat(new[]{CtrlType.TextBox, CtrlType.TextBox}); //許可リストあり
            acl.Add(true, "NAME\t127.0.0.1");

            var sut = StartServer(port, enableAcl, acl);
            var cl = StartClient(port);
            var expected = 0; //　Deny

            //exercise
            var actual = sut.Count();

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            cl.Close();
            sut.Stop();
            sut.Dispose();

        }

        public bool IsLife(){
            throw new NotImplementedException();
        }
    }
}