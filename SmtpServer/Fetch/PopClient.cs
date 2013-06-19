using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.net;

namespace SmtpServer {
    class PopClient{
        private int _port;
        private Ip _addr;
        public PopClient(Ip addr,int port){
            _addr = addr;
            _port = port;

        }
        public void Recv(){
            
        }
    }
}
