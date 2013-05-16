namespace ProxyPop3Server {
    class OneSpecialUser {
        public string Before { get; private set; }
        public string Server { get; private set; }
        public int Port { get; private set; }
        public string After { get; private set; }
        public OneSpecialUser(string before, string server, int port, string after) {
            Before = before;
            Server = server;
            Port = port;
            After = after;
        }
    }
}
