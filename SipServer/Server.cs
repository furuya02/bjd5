using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;

namespace SipServer {

    partial class Server : OneServer {

        User _user;//ユーザ情報

        //コンストラクタ
        //このオブジェクトの生成時の処理（BJD起動・オプション再設定）
        public Server(Kernel kernel, Conf conf, OneBind oneBind)
            : base(kernel, conf,oneBind) { }

        //リモート操作（データの取得）Toolダイログとのデータ送受
        override public string Cmd(string cmdStr) { return ""; }
        
        //サーバ起動時の処理(falseを返すとサーバを起動しない)
        override protected bool OnStartServer() {
            _user = new User();
            return true; 
        }
        
        //サーバ停止時の処理
        override protected void OnStopServer() { }

        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {
            var sockUdp = (SockUdp)sockObj;

            //受信データの解析
            var reception = new Reception(sockUdp.RecvBuf);

            //スタートラインの形式に問題がある
            if (reception.StartLine.ReceptionKind == ReceptionKind.Unknown) {
                //Logger
                return;
            }
            //未対応のSIPバージョン
            if (reception.StartLine.SipVer.No != 2.0) {
                //Logger
                return;
            }
            //リクエストの処理
            if (reception.StartLine.ReceptionKind == ReceptionKind.Request) {

                //Logger(詳細) リクエスト受信をプリント
                
                switch (reception.StartLine.SipMethod) {
                    case SipMethod.Register:
                        var jobRegister = new JobRegister(_user);
                        break;
                    case SipMethod.Invite:
                        break;

                }
                if (reception.StartLine.SipMethod == SipMethod.Invite) {
                    var oneCall = new OneCall();
                    //oneCall.Invite(lines);
                }
            
            } else{//ステータスの処理
                //Logger(詳細) ステータス受信をプリント
                            
            }


            

            //このメソッドを抜けると切断される
        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }

}

