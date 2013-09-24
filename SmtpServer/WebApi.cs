using Bjd.option;
using Bjd.server;
using WebApiServer;

namespace SmtpServer {
    class WebApi{
        private Config _config = null;
        public WebApi(OneOption op){
            //「WebApiServerを使用する」場合だけ、Configへのポインタを有効にする
            if (op != null && op.UseServer){
                _config = ((WebApiServer.Option) op).Config;
            }
        }
        
        //サーバの起動
        public bool Service(){
            if (_config != null){
                return _config.Service;
            }
            return true; //デフォルト値
        }
        
    }
}
