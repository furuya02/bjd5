using System;
using System.Collections.Generic;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyFtpServer {
    class DataThread : IDisposable, ILife{
        readonly Kernel _kernel;
        readonly Logger _logger;
        Thread _t;
        bool _life = true;
        readonly int _timeout;

        //反対側のサーバの情報
        readonly Ip _ip;
        readonly int _port;

        readonly Dictionary<CS, SockTcp> _sock = new Dictionary<CS, SockTcp>(2);

        //サーバ側若しくはクライアント側どちらかのSockTcpは、Listen状態で生成が終わっている
        //そして、その接続の待ち受け開始は、このクラスの中で行われる
        //接続が完了した後、反対側のサーバ（Ip,port）へ、コネクトする

        public DataThread(Kernel kernel, Logger logger, int clientPort, int serverPort, Ip bindAddr, Ip ip, int port, int timeout) {
            _kernel = kernel;
            _logger = logger;

            _timeout = timeout;

            _ip = ip;
            _port = port;

            _sock[CS.Client] = null;
            _sock[CS.Server] = null;

            if (serverPort != 0) { //サーバ側がListen状態の場合 PASV
                _sock[CS.Server] = SockServer.CreateConnection(kernel,bindAddr,serverPort,null,this);
                if (_sock[CS.Server] == null)
                    return;
            } else if (clientPort != 0) { //クライアント側がListen状態の場合 PORT
                _sock[CS.Client] = SockServer.CreateConnection(kernel,bindAddr, clientPort, null,this);
                if (_sock[CS.Client] == null)
                    return;
            }
            //パイプスレッドの生成
            _t = new Thread(Pipe) {
                IsBackground = true
            };
            _t.Start();
        }

        public void Dispose() {
            if (_t == null)
                return;

            _life = false;
            while (_t.IsAlive) {
                Thread.Sleep(100);
            }
        }

//        public void CallBack(IAsyncResult ar) {
//            //接続完了（Listenソケットを取得）
//            var sockObj = (SockObj)(ar.AsyncState);
//
//            if (_sock[CS.Server] != null) { //サーバ側がListen状態の場合
//                //接続ソケットを保持して
//                _sock[CS.Server] = (SockTcp)sockObj.CreateChildObj(ar);
//                //Listenソケットをクローズする
//                sockObj.Close();
//                //クライアントと接続する
//                Ssl ssl = null;
//                _sock[CS.Client] = Inet.Connect(ref _life, _kernel, _logger, _ip, _port, ssl);
//                if (_sock[CS.Client] == null)
//                    return;
//            } else { //クライアント側がListen状態の場合
//                //接続ソケットを保持して
//                _sock[CS.Client] = (SockTcp)sockObj.CreateChildObj(ar);
//                //Listenソケットをクローズする
//                sockObj.Close();
//                //サーバと接続する
//
//                Ssl ssl = null;
//                _sock[CS.Server] = Inet.Connect(ref _life, _kernel, _logger, _ip, _port, ssl);
//                if (_sock[CS.Server] == null)
//                    return;
//            }
//            //パイプスレッドの生成
//            _t = new Thread(Pipe){
//                IsBackground = true
//            };
//            _t.Start();
//        }

        void Pipe() {
            const int idleTime = 0;
            
            var tunnel = new Tunnel(_logger, idleTime, _timeout);
            tunnel.Pipe(_sock[CS.Server], _sock[CS.Client],this);

            _sock[CS.Client].Close();
            _sock[CS.Server].Close();
        }

        public bool IsLife(){
            return _life;
        }
    }
}
