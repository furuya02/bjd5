using System.Collections.Generic;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;

namespace ProxySmtpServer {
    class Server:MailProxyServer {

        //コンストラクタ
        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel, conf,oneBind) {

        }
        protected override string BeforeJob(SockTcp client,List<byte[]> clientBuf) {

            Protocol = MailProxyProtocolKind.Smtp;

            //挨拶文をサーバに変わって送出する
            client.AsciiSend("220 SMTP-Proxy");
            while(clientBuf.Count<5) {
                var buf = client.LineRecv(Timeout,this);
                if(buf == null)
                    return null;//タイムアウト

                //Ver5.8.6
                //var str = Inet.TrimCrlf(Encoding.ASCII.GetString(buf));
                buf = Inet.TrimCrlf(buf);
                var str = Encoding.ASCII.GetString(buf);

                //Ver5,3,4 RESTコマンドは蓄積がプロトコル上できないのでサーバへは送らない
                if(str.ToUpper().IndexOf("RSET")!=0)
                    clientBuf.Add(buf);
                
                
                if(str.ToUpper().IndexOf("QUIT") != -1) {
                    return null;   
                }
                if(clientBuf.Count > 1) {
                    if(str.ToUpper().IndexOf("MAIL FROM:") != -1) {
                        var mailAddress = str.Substring(str.IndexOf(":") + 1);
                        mailAddress = mailAddress.Trim();
                        mailAddress = mailAddress.Trim(new[] { '<','>' });
                        return mailAddress;//メールアドレス
                    }
                }
                client.AsciiSend("250 OK");
            }
            return null;
        }
        protected override string ConnectJob(SockTcp client, SockTcp server,List<byte[]> clientBuf) {

            //最初のグリーティングメッセージ取得
            var buf = server.LineRecv(Timeout, this);
            if (buf == null)
                return null;//タイムアウト
            
            //EHLO送信
            server.LineSend(clientBuf[0]);
            clientBuf.RemoveAt(0);

            //「250 OK」が返るまで読み飛ばす
            while (IsLife()) {

                buf = server.LineRecv(Timeout, this);
                if (buf == null)
                    return null;//タイムアウト
                var str = Inet.TrimCrlf(Encoding.ASCII.GetString(buf));
                if (str.ToUpper().IndexOf("250 ") == 0) {
                    return str;
                }
            }
            return null;
        }
        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

        protected override void CheckLang() {
        }
    }
}



