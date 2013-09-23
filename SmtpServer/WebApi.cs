using Bjd.option;
using Bjd.server;
using WebApiServer;

namespace SmtpServer {
    class WebApi{
        private WebApiServer.Option _op = null;
        public WebApi(OneOption op){
            if (op != null && op.UseServer){
                _op = (WebApiServer.Option) op;
            }
        }
        public bool Service(){
            if (_op != null){
                return _op.Config.Service;
            }
            return true;
        }
    }
}
