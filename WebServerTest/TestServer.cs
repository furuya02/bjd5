using System;
using BjdTest;
using Bjd;
using WebServer;
using System.Net.Sockets;

namespace WebServerTest {
    class TestServer:TsServerBase {

        public string DocumentRoot { get; private set; }
        
        public TestServer(ProtocolKind protocolKind,int port):base("WebServer",protocolKind,port){

            //通常は、NameTagの初期化は、baseのコンストラクタへのパラメータで行われる
            //Webの場合は、特別
            NameTag = string.Format("Web-{0}:{1}", hostName, port);

            //ドキュメントルートの設定
            //テストコードのフォルダにドキュメントルートを設定する
            DocumentRoot = UtilDir.Src + "\\public_html";
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
