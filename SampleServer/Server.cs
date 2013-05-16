
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;

namespace SampleServer {

    partial class Server : OneServer {

        //コンストラクタ
        //このオブジェクトの生成時の処理（BJD起動・オプション再設定）
        public Server(Kernel kernel, Conf conf, OneBind oneBind)
            : base(kernel, conf,oneBind) { }

        //リモート操作（データの取得）Toolダイログとのデータ送受
        override public string Cmd(string cmdStr) { return ""; }
        
        //サーバ起動時の処理(falseを返すとサーバを起動しない)
        override protected bool OnStartServer() { return true; }
        
        //サーバ停止時の処理
        override protected void OnStopServer() { }

        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {
            //UDPサーバの場合は、UdpObjで受ける
            var sockTcp = (SockTcp)sockObj;


            //オプションから「sampleText」を取得する
            //var sampleText = (string)OneOption.GetValue("sampleText");

            //１行受信
            var str = sockTcp.AsciiRecv(30,this);//this.lifeをそのまま渡す

            //１行送信
            sockTcp.AsciiSend(str);

            //このメソッドを抜けると切断される
        }

        //RemoteServerでのみ使用される
        public override void Append(Bjd.log.OneLog oneLog) {

        }


    }
}

