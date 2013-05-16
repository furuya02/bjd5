using System;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyHttpServer
{
    //********************************************************
    //リクエスト/レスポンス処理クラス
    //********************************************************
    public class Request {
        
        public Request() {
            HostName = "";
            Uri = "";
            Ext = "";
            Cgi = false;
            RequestStr = "";
            Port = 80;
            HttpMethod = HttpMethod.Unknown;
            Protocol = ProxyProtocol.Unknown;
            HttpVer = "";
        }
        

        //****************************************************************
        //プロパティ
        //****************************************************************
        public string HostName { get; private set; }
        public string Uri { get; private set; }
        public string Ext { get; private set; }
        public bool Cgi { get; private set; }
        public string RequestStr { get; private set; }
        public int Port { get; private set; }
        public HttpMethod HttpMethod { get; private set; }
        public ProxyProtocol Protocol { get; private set; }
        public string HttpVer { get; private set; }
        public string User { get; private set; }
        public string Pass { get; private set; }
        private Encoding _urlEncoding = Encoding.ASCII;

        public byte [] SendLine(bool useUpperProxy) {
            var str = string.Format("{0} {1} {2}\r\n", HttpMethod.ToString().ToUpper(), Uri, HttpVer);
            if (useUpperProxy) {
                str = string.Format("{0}\r\n", RequestStr);
            }
            return _urlEncoding.GetBytes(str);//当初のエンコード形式に戻す
        }


        //データ取得（内部データは、初期化される）
        public bool Recv(Logger logger, SockTcp tcpObj,int timeout,ILife iLife) {

            var buf= tcpObj.LineRecv(timeout,iLife);
            if (buf == null)
                return false;
            buf = Inet.TrimCrlf(buf);

            _urlEncoding = MLang.GetEncoding(buf);//URLエンコードの形式を保存する
            var str = _urlEncoding.GetString(buf);
          
            // メソッド・URI・バージョンに分割
            //"GET http://hostname:port@user:pass/path/filename.ext?param HTTP/1.1"
            RequestStr = str;

            //(空白で分離する)　"GET <=> http://hostname:port@user:pass/path/filename.ext?param HTTP/1.1"
            var index = str.IndexOf(' ');
            if (index < 0) //Ver5.0.0-a8
                return false;

            //(前半) "GET"
            var methodStr = str.Substring(0, index);
            foreach (HttpMethod m in Enum.GetValues(typeof(HttpMethod))) {
                if (methodStr.ToUpper() == m.ToString().ToUpper()) {
                    HttpMethod = m;
                    break;
                }
            }
            if (HttpMethod == HttpMethod.Unknown) {
                logger.Set(LogKind.Secure,tcpObj,1,string.Format("{0}",RequestStr));//サポート外のメソッドです（処理を継続できません）
                return false;
            }
            if (HttpMethod == HttpMethod.Connect) {
                Protocol = ProxyProtocol.Ssl;
                Port = 443;//デフォルトのポート番号は443になる
            }

            //(後半) "http://hostname:port@user:pass/path/filename.ext?param HTTP/1.1"
            str = str.Substring(index + 1);


            //(空白で分離する)　"http://hostname:port@user:pass/path/filename.ext?param <=> HTTP/1.1"
            index = str.IndexOf(' ');
            if (index < 0) //Ver5.0.0-a8
                return false;
            //(後半) "HTTP/1.1"
            HttpVer = str.Substring(index + 1);
            
            if(HttpVer != "HTTP/0.9" && HttpVer != "HTTP/1.0" && HttpVer != "HTTP/1.1") {
                logger.Set(LogKind.Secure,tcpObj,2,RequestStr);//サポート外のバージョンです（処理を継続できません）
                return false;
            }

            //(前半) "http://hostname:port@user:pass/path/filename.ext?param"
            str = str.Substring(0, index);

            if (Protocol == ProxyProtocol.Unknown) {//プロトコル取得
                //("://"で分離する)　"http <=> hostname:port@user:pass/path/filename.ext?param <=> HTTP/1.1"
                index = str.IndexOf("://");
                if (index < 0) //Ver5.0.0-a8
                    return false;
                //(前半) "http"
                var protocolStr = str.Substring(0, index);

                if (protocolStr.ToLower() == "ftp") {
                    Protocol = ProxyProtocol.Ftp;//プロトコルをFTPに修正
                    Port = 21;//FTP接続のデフォルトのポート番号は21になる
                } else if(protocolStr.ToLower() != "http") {
                    //Ver5.6.7
                    //Msg.Show(MsgKind.Error,"設計エラー　Request.Recv()");
                    //エラー表示をポップアップからログに変更
                    logger.Set(LogKind.Error, tcpObj, 29, string.Format("protocolStr={0}", protocolStr));
                    return false;
                } else {
                    Protocol = ProxyProtocol.Http;
                }
                //(後半) "hostname:port@user:pass/path/filename.ext?param"
                str = str.Substring(index + 3);
            }
            //(最初の"/"で分離する)　"hostname:port@user:pass <=> /path/filename.ext?param"
            index = str.IndexOf('/');
            HostName = str;
            if (0 <= index) {
                //(前半) ""hostname:port@user:pass"
                HostName = str.Substring(0, index);

                //(後半) "/path/filename.ext?param"
                str = str.Substring(index);
            } else {
                // GET http://hostname HTTP/1.0 のように、ルートディレクトリが指定されていない場合の対処
                str = "/";
            }

            //ホスト名部分にユーザ名：パスワードが入っている場合の処理
            index = HostName.IndexOf("@");
            if (0 <= index) {
                var userpass = HostName.Substring(0,index);

                //ユーザ名：パスワードを破棄する
                HostName = HostName.Substring(index + 1);

                var i = userpass.IndexOf(':');
                if(i == -1) {
                    User = userpass;
                } else {
                    User = userpass.Substring(0,i);
                    Pass = userpass.Substring(i + 1);
                }
            }
            //Ver5.1.2 IPv6アドレス表記のホスト名に対応
            var tmp = HostName.Split(new[] { '[',']' });
            if(tmp.Length == 3) {//IPv6アドレス表記であると判断する
                HostName = string.Format("[{0}]",tmp[1]);
                index = tmp[2].IndexOf(":");
                if(0 <= index) {
                    var s = tmp[2].Substring(index + 1);
                    Port = Convert.ToInt32(s);
                }
            }else{

                //ホスト名部分にポート番号が入っている場合の処理
                index = HostName.IndexOf(":");
                if (0 <= index) {
                    var s = HostName.Substring(index + 1);
                    Port = Convert.ToInt32(s);
                    HostName = HostName.Substring(0, index);
                }
            }
                
            Uri = str;
            
            //CGI検査
            if(-1!=Uri.LastIndexOf('?'))
                Cgi=true;

            //拡張子取得
            if (!Cgi) {
                index = Uri.LastIndexOf('/');
                if (index != -1)
                    str = Uri.Substring(index + 1);
                index = str.LastIndexOf('.');
                if (index != -1) {
                    Ext = str.Substring(index + 1);
                }
            }
            return true;
        }
    }
}
