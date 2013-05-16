using BjdTest;
using Bjd;
using WebServer;

namespace WebServerTest {
    class TsServer:TsServerBase {

        public string DocumentRoot { get; private set; }
        
        public TsServer():base("WebServer",ProtocolKind.Tcp,88){
            
            //通常は、NameTagの初期化は、baseのコンストラクタへのパラメータで行われる
            //Webの場合は、特別
            NameTag = string.Format("Web-{0}:{1}", HostName, Port);

            //ドキュメントルートの設定
            //テストコードのフォルダにドキュメントルートを設定する
            DocumentRoot = TsDir.Src + "\\public_html";
            SetOption("FOLDER", "documentRoot", DocumentRoot);
        
        }

        protected override OneServer CreateServer(Kernel kernel,OneBind oneBind) {
            return new Server(kernel, NameTag, oneBind);
        }
        protected override OneOption CreateOption(Kernel kernel) {
            return new Option(kernel, "", NameTag);
        }
    }
}
