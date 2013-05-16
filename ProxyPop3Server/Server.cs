using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;

namespace ProxyPop3Server {
    class Server:MailProxyServer {

        //コンストラクタ
        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel,conf,oneBind) {

        }
        protected override string BeforeJob(SockTcp client) {
            Protocol = MailProxyProtocol.Pop3;

            //挨拶文をサーバに変わって送出する
            client.AsciiSend("+OK ");

            //USER コマンドを受け付けるまでループ(最大５回)する
            for(var i=0;i<5;i++) {
                var buf = client.LineRecv(Timeout,this);
                if(buf != null) {
                    var str = Inet.TrimCrlf(Encoding.ASCII.GetString(buf));
                    if(str.IndexOf("USER") == 0) {
                        ClientBuf.Add(buf);
                        var tmp = str.Split(' ');
                        if(tmp.Length >= 2) {
                            return tmp[1];//ユーザ名
                        }
                    } else if(str.IndexOf("QUIT") == 0) {
                        return null;
                    } else {
                        client.AsciiSend("-ERR ");
                    }
                }else{
                    Thread.Sleep(300);
                }
            }
            return null;
        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }
}


