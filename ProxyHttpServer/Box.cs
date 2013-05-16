using System;
using System.Collections.Generic;
using System.Text;
using BJD;

namespace ProxyHttpServer {
    class Http {
        Dictionary<CS,TcpObj> sock = new Dictionary<CS,TcpObj>(2);
        public Http(TcpObj clientSocket) {
            sock[CS.CLIENT] = clientSocket;
            sock[CS.SERVER] = null;


        }
    
    }


    class OneHttp {
        Request request = new Request();
        public OneHttp() {
        }
        public void ClientRequest(){

        }
    }
}
