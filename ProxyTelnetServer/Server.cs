using System;
using System.Text;

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
            {
                //接続先（ホスト）名取得
                client.AsciiSend("open>");
                var sb = new StringBuilder();
                while (IsLife()) {
                    var b = client.Recv(1,Timeout,this);//timeout=60sec
                    if (b == null)
                        break;
                    client.SendUseEncode(b);//エコー
                    var c = Convert.ToChar(b[0]);
                    if (c == '\r')
                        continue;
                    if (c == '\n')
                        break;
                    sb.Append(c);
                }
                hostName = sb.ToString();
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
                    Logger.Set(LogKind.Normal,null,3,string.Format("open>{0}",hostName));
                    goto end;
                }
            }
            Logger.Set(LogKind.Normal,server,1,string.Format("open>{0}",hostName));


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

