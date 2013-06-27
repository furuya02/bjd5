using Bjd;
using Bjd.net;

namespace SmtpServer {
    //接続先サーバの情報を表現するクラス
    class OneSmtpServer {
        public OneSmtpServer(string targetServer, Ip ip, int port, bool useSmtp, string user, string pass, bool ssl) {
            TargetServer = targetServer;
            Ip = ip;
            Port = port;
            UseSmtp = useSmtp;
            User = user;
            Pass = pass;
            Ssl = ssl;
        }

        //****************************************************************
        //プロパティ
        //****************************************************************
        public string TargetServer { get; private set; }
        public Ip Ip { get; private set; }
        public int Port { get; private set; }
        public bool UseSmtp { get; private set; }
        public string User { get; private set; }
        public string Pass { get; private set; }
        public bool Ssl { get; private set; }
    }
}