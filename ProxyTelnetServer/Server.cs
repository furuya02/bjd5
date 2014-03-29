using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using Bjd.util;

namespace ProxyTelnetServer {

    public partial class Server : OneServer {


        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel, conf,oneBind) {
        }
        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }
        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {

            string hostName;

            var client = (SockTcp)sockObj;
            SockTcp server = null;

            //***************************************************************
            //前処理（接続先・ユーザ名・パスワードの取得)
            //***************************************************************
            //{
            //    //接続先（ホスト）名取得
            //    client.AsciiSend("open>");
            //    var sb = new StringBuilder();
            //    while (IsLife()) {
            //        var b = client.Recv(1,Timeout,this);//timeout=60sec
            //        if (b == null)
            //            break;

            //        var c = Convert.ToChar(b[0]);
            //        if (c == '\r'){
            //            continue;
            //        }
            //        if (c == '\n')
            //            break;
            //        //Ver6.0.6 TeraTerm対応
            //        if (c == 0 && sb.Length != 0)
            //        {
            //            break;
            //        }

            //        if ((c == '.') || ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z') || ('0' <= c && c <= '9'))
            //        {
            //            client.Send(b);//エコー
            //            sb.Append(c);
            //        }

            //    }
            //    hostName = sb.ToString();
            //}
            var buf = new List<byte>();
                
            {
                //接続先（ホスト）名取得
                client.Send(Encoding.ASCII.GetBytes("open>"));

                var iac = false;
                var ego = false;
                 var sb = new StringBuilder();
                while (IsLife()){
                    var b = client.Recv(1, Timeout, this); //timeout=60sec
                    if (b == null)
                        break;
                    var d = Convert.ToChar(b[0]);
                    if (d == 0)
                        continue;
                    if (d == '\xFF'){
                        iac = true;
                        buf.Add(b[0]);
                        continue;
                    }
                    if (iac){
                        if (d == '\xFA'){
                            ego = true;
                            iac = false;
                            buf.Add(b[0]);
                            continue;
                        }
                        if (d == '\xF0'){
                            ego = false;
                            iac = false;
                            buf.Add(b[0]);
                            continue;
                        }
                        if (d == '\xFB' || d == '\xFC' || d == '\xFD' || d == '\xFE'){
                            buf.Add(b[0]);
                            continue;
                        }
                        iac = false;
                        buf.Add(b[0]);
                        continue;
                    }
                    if (ego){
                        buf.Add(b[0]);
                        continue;
                    }
                    client.Send(b); //エコー

                    if (d == '\r' || d == '\n')
                        break;

                    if (d == '\b'){
                        sb.Remove(sb.Length - 1, 1);
                    } else{
                        sb.Append(d);
                    }
                }
                hostName = sb.ToString();
            }
            // ごみの破棄
            while (IsLife() && client.Length()>0)
            {
                var b = client.Recv(1, Timeout, this); //timeout=60sec
            }
           

            //***************************************************************
            // サーバとの接続
            //***************************************************************
            {
                const int port = 23;
                //var ipList = new List<Ip>{new Ip(hostName)};
                //if (ipList[0].ToString() == "0.0.0.0") {
                //    ipList = Kernel.DnsCache.Get(hostName);
                //    if (ipList.Count == 0) {
                //        Logger.Set(LogKind.Normal,null,2,string.Format("open>{0}",hostName));
                //        goto end;
                //    }
                //}
                var ipList = Kernel.GetIpList(hostName);
                if (ipList.Count == 0) {
                    Logger.Set(LogKind.Normal, null, 2, string.Format("open>{0}", hostName));
                    goto end;
                }
                foreach (var ip in ipList) {
                    server = Inet.Connect(Kernel,ip,port,Timeout,null);
                    if (server != null)
                        break;
                }

                if (server == null) {
                    Logger.Set(LogKind.Normal, null, 3, string.Format("open>{0}", hostName));
                    goto end;
                }
                
                //Ver6.0.6
                if (server.SockState != Bjd.sock.SockState.Connect){
                    Logger.Set(LogKind.Normal, null, 3, string.Format("open>{0}", hostName));
                    goto end;
                }
            }
            Logger.Set(LogKind.Normal,server,1,string.Format("open>{0}",hostName));
            
            server.Send(buf.ToArray(),buf.Count);
            //***************************************************************
            // パイプ
            //***************************************************************
            var tunnel = new Tunnel(Logger,(int)Conf.Get("idleTime"),Timeout);
            tunnel.Pipe(server,client,this);
        end:
            client.Close();
            if (server != null)
                server.Close();

        }
        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }
}

