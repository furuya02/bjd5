namespace WebServer {
    class OneEnv {
        public string Key { get; private set; }
        public string Val { get; private set; }
        public OneEnv(string key, string val) {
            Key = key;
            Val = val;
        }
    }
}
