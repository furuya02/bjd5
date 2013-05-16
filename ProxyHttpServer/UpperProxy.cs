using System.Collections.Generic;

namespace ProxyHttpServer {
    //上位プロキシ情報
    class UpperProxy {
        public bool Use{ get;set;}
        public string Server { get; private set; }
        public int Port { get; private set; }
        public List<string> DisableAdderssList { get; private set; }
        //Ver5.6.9
        public string AuthUser { get; private set; }
        public string AuthPass { get; private set; }
        public bool UseAuth { get; set; }

        public UpperProxy(bool use,string server,int port,List<string> disableAddressList,bool useAuth,string authUser,string authPass) {
            Use = use;
            Server = server;
            Port = port;
            DisableAdderssList = disableAddressList;
            UseAuth = useAuth;
            AuthUser = authUser;
            AuthPass = authPass;
        }
    }
}
