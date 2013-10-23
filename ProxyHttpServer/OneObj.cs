using System;
using System.Collections.Generic;
using Bjd;
using Bjd.log;
using Bjd.util;

namespace ProxyHttpServer {
    class OneObj:IDisposable {
        protected Proxy Proxy;


        public OneObj(Proxy proxy) {
            Proxy = proxy;
            
            Request = new Request();
            Header = new Dictionary<CS,Header>(2);
            Header[CS.Client] = new Header();
            Header[CS.Server] = new Header();

            //データバッファ
            //Body = new Dictionary<CS,byte[]>(2);
            //Body[CS.Client] = new byte[0];
            //Body[CS.Server] = new byte[0];
            
            Body = new Dictionary<CS, BodyBuf>(2);
            Body[CS.Client] = new BodyBuf(640000);
            Body[CS.Server] = new BodyBuf(640000);

            //送信完了サイズ
            Pos = new Dictionary<CS,long>(2);
            Pos[CS.Client] = new long();
            Pos[CS.Server] = new long();


        }
        public Request Request { get; private set; }
        public Dictionary<CS,Header> Header { get; private set; }
        public Dictionary<CS,BodyBuf> Body { get; private set; }
        public Dictionary<CS,long> Pos { get; private set; }
        
        //Ver5.9.0
//        public void Dispose() {
//            foreach(CS cs in Enum.GetValues(typeof(CS))) {
//                Body[cs] = null;
//                Header[cs] = null;
//                Pos[cs] = 0;
//            }
//            Body = null;
//            Header = null;
//            Pos = null;
//        }
        public void Dispose() {
            if (Body != null) {
                foreach (CS cs in Enum.GetValues(typeof(CS))) {
                    Body[cs] = null;
                }
                Body = null;
            }
            if (Header != null) {
                foreach (CS cs in Enum.GetValues(typeof(CS))) {
                    Header[cs] = null;
                }
                Header = null;
            }
            if (Pos != null) {
                foreach (CS cs in Enum.GetValues(typeof(CS))) {
                    Pos[cs] = 0;
                }
                Pos = null;
            }
        }


        //リクエスト行・ヘッダ・POSTデータ
        public bool RecvRequest(bool useRequestLog,LimitUrl limitUrl,ILife iLife) {

            //リクエスト取得（内部データは初期化される）ここのタイムアウト値は、大きすぎるとブラウザの切断を取得できないでブロックしてしまう
            if(!Request.Recv(Proxy.Logger,Proxy.Sock(CS.Client),/*timeout*/3,iLife)) {
                return false;
            }
            //ヘッダの取得
            if(!Header[CS.Client].Recv(Proxy.Sock(CS.Client),Proxy.OptionTimeout,iLife)) {
                return false;
            }
            //POSTの場合は、更にクライアントからのデータを読み込む
            if (Request.Protocol == ProxyProtocol.Http && Request.HttpMethod == HttpMethod.Post) {//POSTの場合
                string strContentLength = Header[CS.Client].GetVal("Content-Length");
                if(strContentLength != null) {
                    try {
                        var len = Convert.ToInt32(strContentLength);
                        //Ver5.9.7
//                        if(0 < len) {
//                            Body[CS.Client].Set(Proxy.Sock(CS.Client).Recv(len,Proxy.OptionTimeout,iLife));
//                        }
                        if (0 < len) {
                            var buf = new byte[0];
                            while (iLife.IsLife()) {
                                var size = len - buf.Length;
                                var b = Proxy.Sock(CS.Client).Recv(size, Proxy.OptionTimeout, iLife);
                                buf = Bytes.Create(buf, b);
                                if (len <= buf.Length) {
                                    break;
                                }
                            }
                            Body[CS.Client].Set(buf);
                        }

                    } catch {
                        Proxy.Logger.Set(LogKind.Error,null,22,Request.Uri);
                        return false;
                    }
                }
            }
                
            //bool useRequestLog リクエストを通常ログで表示する
            //proxy.Logger.Set(useRequestLog ? LogKind.Normal : LogKind.Detail,null,0,string.Format("{0}",Request.RequestStr));
            Proxy.Logger.Set(useRequestLog ? LogKind.Normal : LogKind.Detail,Proxy.Sock(CS.Client),0,string.Format("{0}",Request.RequestStr));

            //URL制限
            string[] tmp = Request.RequestStr.Split(' ');
            if (tmp.Length != 3) {
                Proxy.Logger.Set(LogKind.Normal, null, 10, "a parameter includes a problem");
                return false;
            }
            string errorStr = "";
            if (!limitUrl.IsAllow(tmp[1], ref errorStr)) {
                Proxy.Logger.Set(LogKind.Normal, null, 10, errorStr);
                return false;
            }
            return true;
        }

    }
}
