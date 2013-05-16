using System;
using System.Collections.Generic;
using System.Globalization;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace WebServer
{
    //********************************************************
    //リクエスト/レスポンス処理クラス
    //********************************************************
    internal class Request {

        
        public Request(Logger logger,SockTcp sockTcp) {

            //Logger出力用(void Log()の中でのみ使用される)
            _logger = logger;
            _sockObj = sockTcp;

            Method = HttpMethod.Unknown;
            Uri = "";
            Param = "";
            Ver = "";
            LogStr = "";

        }
        
        readonly Logger _logger;
        readonly SockTcp _sockObj;//Logger出力用
        
        void Log(LogKind logKind, int messageNo, string msg) {
            if(_logger != null){
                _logger.Set(logKind, _sockObj, messageNo, msg);
            }
        }

        public HttpMethod Method { get; private set; }
        public string Uri { get; private set; }
        public string Param { get; private set; }
        public string Ver { get; private set; }
        public string LogStr { get; private set; }

        //データ取得（内部データは、初期化される）
        //public bool Recv(int timeout,sockTcp sockTcp,ref bool life) {
        public bool Init(string requestStr) {

            //既存のデータが残っている場合は削除してから受信にはいる
            Uri = "";
            Param = "";
            Ver = "";
            Method = HttpMethod.Unknown;//Ver5.1.x

            //string str = sockTcp.AsciiRecv(timeout,OperateCrlf.Yes,ref life);
            //if (str == null)
            //    return false;

            // メソッド・URI・バージョンに分割

            //リクエスト行がURLエンコードされている場合は、その文字コードを取得する
            try{
                LogStr = System.Uri.UnescapeDataString(requestStr);//リクエスト文字列をそのまま保存する（ログ表示用）
            }catch{
                LogStr = UrlDecode(requestStr);
            }

            var tmp = requestStr.Split(' ');
            if (tmp.Length != 3) {
                Log(LogKind.Secure, 0, string.Format("Length={0} {1}", tmp.Length, requestStr));//リクエストの解釈に失敗しました（不正なリクエストの可能性があるため切断しました
                return false;
            }
            if (tmp[0] == "" || tmp[1] == "" || tmp[1] == "") {
                Log(LogKind.Secure, 0, string.Format("{0}", requestStr));//リクエストの解釈に失敗しました（不正なリクエストの可能性があるため切断しました
                return false;
            }


            // メソッドの取得
            foreach (HttpMethod m in Enum.GetValues(typeof(HttpMethod))) {
                if (tmp[0].ToUpper() == m.ToString().ToUpper()) {
                    Method = m;
                    break;
                }
            }
            if (Method == HttpMethod.Unknown) {
                Log(LogKind.Secure, 1, string.Format("{0}", requestStr));//サポート外のメソッドです（処理を継続できません）
                return false;
            }
            //バージョンの取得
            if (tmp[2] == "HTTP/0.9" || tmp[2] == "HTTP/1.0" || tmp[2] == "HTTP/1.1") {
                Ver = tmp[2];
            } else {
                Log(LogKind.Secure, 2, string.Format("{0}", requestStr));//サポート外のバージョンです（処理を継続できません）
                return false;
            }
            //パラメータの取得
            var tmp2 = tmp[1].Split('?');
            if (2 <= tmp2.Length)
                Param = tmp2[1];
            // Uri の中の%xx をデコード
            try {
                Uri = System.Uri.UnescapeDataString(tmp2[0]);
                Uri = UrlDecode(tmp2[0]);
            }catch{
                Uri = UrlDecode(tmp2[0]);
            }
            
            //Ver5.1.3-b5 制御文字が含まれる場合、デコードに失敗している
            for(var i = 0;i < Uri.Length;i++) {
                if(18 >= Uri[i]) {
                    Uri = tmp2[0];
                    break;
                }
            }

            
            //Uriに/が続く場合の対処
            Uri = Util.SwapStr("//", "/", Uri);

            //Ver5.8.8
            if (Uri == ""  || Uri[0]!='/'){
                Log(LogKind.Secure, 5, LogStr);
                return false;
            }

            return true;
        }
        
        string UrlDecode(string s) {
            var enc = Inet.GetUrlEncoding(s);
            var b = new List<byte>();
            for (var i = 0; i < s.Length; i++) {
                switch (s[i]) {
                    case '%':
                        b.Add((byte)int.Parse(s[++i].ToString() + s[++i].ToString(), NumberStyles.HexNumber));
                        break;
                    case '+':
                        b.Add(0x20);
                        break;
                    default:
                        b.Add((byte)s[i]);
                        break;
                }
            }
            return enc.GetString(b.ToArray(),0,b.Count);
        }



        

        public string StatusMessage(int code) {
            var statusMessage = "";
            switch (code) {
                case 102:
                    statusMessage = "Processiong"; //RFC2518(10.1)
                    break;
                case 200:
                    statusMessage = "Document follows";
                    break;
                case 201:
                    statusMessage = "Created";
                    break;
                case 204:
                    statusMessage = "No Content";
                    break;
                case 206:
                    statusMessage = "Partial Content";
                    break;
                case 207:
                    statusMessage = "Multi-Status"; //RFC2518(10.2)
                    break;
                case 301:
                    statusMessage = "Moved Permanently";
                    break;
                case 302:
                    statusMessage = "Moved Temporarily";
                    break;
                case 304:
                    statusMessage = "Not Modified";
                    break;
                case 400:
                    statusMessage = "Missing Host header or incompatible headers detected.";
                    break;
                case 401:
                    statusMessage = "Unauthorized";
                    break;
                case 402:
                    statusMessage = "Payment Required";
                    break;
                case 403:
                    statusMessage = "Forbidden";
                    break;
                case 404:
                    statusMessage = "Not Found";
                    break;
                case 405:
                    statusMessage = "Method Not Allowed";
                    break;
                case 412:
                    statusMessage = "Precondition Failed";
                    break;
                case 422:
                    statusMessage = "Unprocessable"; //RFC2518(10.3)
                    break;
                case 423:
                    statusMessage = "Locked"; //RFC2518(10.4)
                    break;
                case 424:
                    statusMessage = "Failed Dependency"; //RFC2518(10.5)
                    break;
                case 500:
                    statusMessage = "Internal Server Error";
                    break;
                case 501:
                    statusMessage = "Request method not implemented";
                    break;
                case 507:
                    statusMessage = "Insufficient Storage"; //RFC2518(10.6)
                    break;
            }
            return statusMessage;
        }

        //レスポンスの送信
        //public void Send(sockTcp sockTcp,int code) {
        //    string str = string.Format("{0} {1} {2}", Ver, code,StatusMessage(code));
        //    sockTcp.AsciiSend(str,OperateCrlf.Yes);//レスポンス送信
        //    logger.Set(LogKind.Detail,sockTcp,4,str);//ログ

        //}
        //レスポンス行の作成
        public string CreateResponse(int code) {
            return string.Format("{0} {1} {2}", Ver, code, StatusMessage(code));
        }
    }
}
