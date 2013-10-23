using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using Newtonsoft.Json;

namespace WebApiServer {
    partial class Server : OneServer {

        //コンストラクタ
        //このオブジェクトの生成時の処理（BJD起動・オプション再設定）
        public Server(Kernel kernel, Conf conf, OneBind oneBind) : base(kernel, conf, oneBind){

        }

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

            // レスポンス用のJSON文字列
            var json = JsonConvert.SerializeObject(new Error(500,"Not Implemented",""));

            //１行受信
            var str = sockTcp.AsciiRecv(30,this);
            if (str == null){
                return;
            }
            //GET /mail/cmd?p1=v1&p2=v2 HTTP/1.1
            var tmp = str.Split(' ');
            if (tmp.Length == 3){
                var method = Method.Unknown;
                foreach (Method m in Enum.GetValues(typeof(Method))){
                    if (m.ToString().ToLower() == tmp[0].ToLower()){
                        method = m;
                        break;
                    }
                }


                if (method != Method.Unknown) {
                    // /mail/cmd?p1=v1&p2=v2
                    var p = tmp[1].Split('/');
                    if (p.Length == 3){
                        var server = p[1].ToLower(); //パラメータの値以外は、強制的に小文字に設定する
                        var n = p[2].Split('?');
                        var cmd = n[0].ToLower();//パラメータの値以外は、強制的に小文字に設定する
                        var param = new Dictionary<String, String>();
                        if (n.Length == 2){
                            foreach (var m in n[1].Split('&')){
                                var o = m.Split('=');
                                if (o.Length == 2){
                                    param.Add(o[0].ToLower(), o[1]); //パラメータの値以外は、強制的に小文字に設定する
                                } else{
                                    param.Add(m.ToLower(), ""); //パラメータの値以外は、強制的に小文字に設定する
                                }
                            }
                        }
                        if (server == "mail"){

//                            OneOption.GetValue("sampleText");

                            var mail = new SvMail(Kernel);
                            json = mail.Exec(method,cmd, param);
                        }
                    }
                }
            }

            //１行送信
            sockTcp.Send(Encoding.UTF8.GetBytes(json));

            //このメソッドを抜けると切断される
        }

        //RemoteServerでのみ使用される
        public override void Append(Bjd.log.OneLog oneLog) {

        }


    }

    class Error{
        public int code { get; private set; }
        public String message { get; private set; }
        public Error(int code, string message,string tag){
            this.code = code;
            this.message = string.Format("{0} [{1}]",message,tag);
        }

    }
}

