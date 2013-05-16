using System;
using Bjd.sock;

namespace FtpServer{

    //セッションごとの情報
    public class Session{
        public string UserName { get; set; }
        public string RnfrName { get; set; }
        public int Port { get; set; }
        public CurrentDir CurrentDir { get; set; }
        public OneUser OneUser { get; set; }
        public FtpType FtpType { get; set; }
        public SockTcp SockData { get; set; }
        public SockTcp SockCtrl { get; private set; }

        public Session(SockTcp sockCtrl){
            SockCtrl = sockCtrl;

            //PASV接続用ポート番号の初期化 (開始番号は2000～9900)
            var rnd = new Random();
            Port = (rnd.Next(79) + 20)*100;

        }

        //１行送信
        public void StringSend(string str){
            SockCtrl.StringSend(str,"ascii");
        }
    }
}