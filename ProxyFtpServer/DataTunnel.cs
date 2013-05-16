using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyFtpServer {
    class DataTunnel:ThreadBase {

        private readonly Dictionary<CS, SockTcp> _sock = new Dictionary<CS, SockTcp>(2);
        private readonly Dictionary<CS, byte[]> _buf = new Dictionary<CS, byte []>(2);

        private readonly Kernel _kernel;
        private readonly Ip _listenIp;
        private readonly Ip _connectIp;
        private readonly int _listenPort;
        private readonly int _connectPort;

        private Tunnel _tunnel;

        public DataTunnel(Kernel kernel,Logger logger,Ip listenIp,int listenPort,Ip connectIp,int connectPort,Tunnel tunnel) : base(logger){
            _sock[CS.Client] = null;
            _sock[CS.Server] = null;
            _buf[CS.Client] = new byte[0];
            _buf[CS.Server] = new byte[0];
            _kernel = kernel;
            _listenIp = listenIp;
            _listenPort = listenPort;
            _connectIp = connectIp;
            _connectPort = connectPort;

            _tunnel = tunnel;

        }

        protected override bool OnStartThread(){
            return true;
        }

        protected override void OnStopThread(){
        
        }

        protected override void OnRunThread(){

            //IsRunning = true;
            KindThreadBase = KindThreadBase.Running;

            int timeout = 3;

            _sock[CS.Client] = new SockTcp(_kernel, _connectIp, _connectPort, timeout, null);
            _sock[CS.Server] = SockServer.CreateConnection(_kernel, _listenIp, _listenPort, this);

            while (IsLife()){
                for (var i = 0; i < 2; i++){
                    var cs = (i == 0) ? CS.Server : CS.Client;
                    if (_buf[cs].Length == 0){
                        var len = _sock[cs].Length();
                        if (len != 0) {
                            _tunnel.ResetIdle();//アイドル時間の更新

                            _buf[cs] = _sock[cs].Recv(len, 1, this);
                        }
                    }
                    if (_buf[cs].Length != 0){
                        var s = _sock[((i == 0) ? CS.Client : CS.Server)].Send(_buf[cs]);
                        if (-1 != s) {
                            _buf[cs] = new byte[0];
                        }
                    }
                }
                if (_sock[CS.Server].SockState != SockState.Connect && _sock[CS.Server].Length()==0) {
                    break;
                }
                if (_sock[CS.Client].SockState != SockState.Connect && _sock[CS.Client].Length() == 0) {
                    break;
                }
            }
            _sock[CS.Client].Close();
            _sock[CS.Server].Close();
        }

        public override string GetMsg(int no){
            return "";
        }
    }
}
