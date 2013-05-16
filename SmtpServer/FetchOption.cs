namespace SmtpServer {
    class FetchOption {
        public int Interval { get; private set; }//受信間隔(分)
        public string Host { get; private set; }//サーバ
        public int Port { get; private set; }//ポート
        public string User { get; private set; }//ユーザ
        public string Pass { get; private set; }//パスワード
        public string LocalUser { get; private set; }//ローカルユーザ
        public int Synchronize { get; private set; }//同期
        public int KeepTime { get; private set; }//サーバに残す時間（分）
        public FetchOption(int interval, string host, int port, string user, string pass, string localUser, int synchronize, int keepTime) {
            Interval = interval;
            Host = host;
            Port = port;
            User = user;
            Pass = pass;
            LocalUser = localUser;
            Synchronize = synchronize;
            KeepTime = keepTime;
        }
    }
}