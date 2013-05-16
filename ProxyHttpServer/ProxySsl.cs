using System;
using System.Collections.Generic;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyHttpServer {

    class ProxySsl:ProxyObj {
        //データオブジェクト
        OneObj _oneObj;
        long _lastRecvServer = DateTime.Now.Ticks;

        public ProxySsl(Proxy proxy):base(proxy) {
        }
        
        override public void Dispose(){
            _oneObj.Dispose();
        }

        
        //クライアントへの送信がすべて完了しているかどうかの確認
        override public bool IsFinish() {
            if(_oneObj.Body[CS.Server].Length == _oneObj.Pos[CS.Server]) {
                if(_oneObj.Body[CS.Client].Length == _oneObj.Pos[CS.Client]) {
                    if(Proxy.Sock(CS.Server).Length() == 0) {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }
        override public bool IsTimeout() {
            if(IsFinish()) {
                if(WaitTime > Proxy.OptionTimeout)
                    return true;
            }
            return false;
        }

        //データオブジェクトの追加
        override public void Add(OneObj oneObj) {
            _oneObj = oneObj;

            if(Proxy.UpperProxy.Use) {
                //上位プロキシを使用する場合(リクエストラインはそのまま使用される)
                oneObj.Body[CS.Client].Set(Bytes.Create(oneObj.Request.SendLine(Proxy.UpperProxy.Use),oneObj.Header[CS.Client].GetBytes()));
            } else {
                //取得したリクエストをバッファに格納する
                oneObj.Body[CS.Client].Set(new byte[0]);
                oneObj.Body[CS.Server].Set(Bytes.Create("HTTP/1.0 200 Connection established\r\n\r\n"));//CONNECTが成功したことをクライアントに返す
            }

        }

        override public void DebugLog() {
            var list = new List<string>();

            //すべてのプロキシが完了している
            list.Add(string.Format("[SSL] SOCK_STATE sv={0} cl={1} HostName={2}",Proxy.Sock(CS.Server).SockState,Proxy.Sock(CS.Client).SockState,Proxy.HostName));
            list.Add(string.Format("[SSL] {0}",_oneObj.Request.RequestStr));
            list.Add(string.Format("[SSL] buf sv={0} cl={1} pos sv={2} cl={3} ■WaitTime={4}sec",_oneObj.Body[CS.Server].Length,_oneObj.Body[CS.Client].Length,_oneObj.Pos[CS.Server],_oneObj.Pos[CS.Client],WaitTime));

            foreach(string s in list)
                Proxy.Logger.Set(LogKind.Debug,null,999,s);
        }


        //プロキシ処理
        override public bool Pipe(ILife iLife) {

            if(!RecvClient(iLife))//クライアントからの受信
                return false;
            if(!SendServer(iLife))//サーバへの送信
                return false;
            if(!RecvServer(iLife))//サーバからの受信
                return false;
            if(!SendClient())//クライアントへの送信
                return false;

            if(Proxy.Sock(CS.Server).SockState != SockState.Connect){
                if(IsFinish())
                    return false;
            }

            //クライアントから切断された場合は、常に処理終了
            if(Proxy.Sock(CS.Client).SockState != SockState.Connect) {
                Proxy.Logger.Set(LogKind.Debug,null,999,"□Break ClientSocket!=CONNECT");
                return false;
            }

            return true;
        }

        long WaitTime {
            get {
                return (DateTime.Now.Ticks - _lastRecvServer) / 1000 / 1000 / 10;
            }
        }
        bool RecvClient(ILife iLife) {
            if(!RecvBuf(CS.Client,iLife))
                return false;
            return true;
        }
        bool RecvServer(ILife iLife) {
            if(!RecvBuf(CS.Server,iLife))
                return false;
            return true;
        }

        bool SendServer(ILife iLife) {
            //サーバ側との接続処理
            if(!Proxy.Connect(iLife,_oneObj.Request.HostName,_oneObj.Request.Port,_oneObj.Request.RequestStr,_oneObj.Request.Protocol)) {
                Proxy.Logger.Set(LogKind.Debug,null,999,"□Break http.Connect()==false");
                return false;
            }
            //バッファに残っているデータの送信
            if(!SendBuf(CS.Client))
                return false;
            return true;
        }

        bool SendClient() {
            //バッファに残っているデータの送信
            if(!SendBuf(CS.Server))
                return false;
            return true;
        }

        //データの受信
        //パラメータ cs CS.SERVER を設定した場合、buf[CS.SERVER]を処理対象とし、クライアント側に送信する
        bool RecvBuf(CS cs, ILife iLife) {
            SockTcp sock = Proxy.Sock(cs);
            if(sock == null)//サーバ側未接続
                return true;

            var len = sock.Length();
            if(len == 0)
                return true;
            var b = sock.Recv(len,Proxy.OptionTimeout,iLife);
            if(b == null)
                return false;
            _oneObj.Body[cs].Add(b);
            _lastRecvServer = DateTime.Now.Ticks;
            return true;
        }

        //バッファに残っているデータの送信
        //パラメータ cs CS.SERVER を設定した場合、buf[CS.SERVER]を処理対象とし、クライアント側に送信する
        bool SendBuf(CS cs) {
            var sock = Proxy.Sock(CS.Client);
            if(cs == CS.Client)
                sock = Proxy.Sock(CS.Server);

            var len = _oneObj.Body[cs].Length - _oneObj.Pos[cs];
            if(len > 0) {
                var sendBuf = _oneObj.Body[cs].SendBuf((int)_oneObj.Pos[cs]);
                if(!Send(sock,sendBuf))//送信
                    return false;
                _oneObj.Pos[cs] += len;
                _lastRecvServer = DateTime.Now.Ticks;
            }
            return true;
        }

        //送信
        bool Send(SockTcp sock,byte[] sendBuf) {
            var c = sock.SendUseEncode(sendBuf);
            if(c == sendBuf.Length) {
                sendBuf = new byte[0];
            } else {
                return false;
            }
            return true;
        }

    }
}
