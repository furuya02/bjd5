using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;

namespace ProxyHttpServer {

    class ProxyFtp:ProxyObj {
        //データオブジェクト
        OneObj _oneObj;
        readonly Kernel _kernel;
        //readonly OneOption _oneOption;
        readonly Conf _conf;
        readonly Server _server;

        readonly long _lastRecvServer = DateTime.Now.Ticks; 

        //ユーザ名及びパスワード
        string _user;
        string _pass;
        //パス及びファイル名
        string _path;
        string _file;

        public ProxyFtp(Proxy proxy, Kernel kernel, Conf conf, Server server, int dataPort)
            : base(proxy) {
            _kernel = kernel;
            //_oneOption = oneOption;
            _conf = conf;
            _server = server;

            DataPort = dataPort;

            //USER PASSのデフォルト値
            _user = "anonymous";
            _pass = (string)conf.Get("anonymousAddress");
        }

        override public void Dispose() {
            _oneObj.Dispose();
        }

        public int DataPort { get; private set; }

        //クライアントへの送信がすべて完了しているかどうかの確認
        override public bool IsFinish() {
            if(_oneObj.Body[CS.Server].Length == _oneObj.Pos[CS.Server]) {
                if(_oneObj.Body[CS.Client].Length == _oneObj.Pos[CS.Client]) {
                    return true;
                }
            }
            return false;
        }
        override public bool IsTimeout() {
            if(IsFinish()) {
                if(WaitTime > Proxy.OptionTimeout)
                    return true;
            }
            return false;
        }

        //データオブジェクトの追加
        override public void Add(OneObj oneObj) {
            _oneObj = oneObj;
            
            //URLで指定されたユーザ名およびパスワードを使用する
            if(oneObj.Request.User != null)
                _user = oneObj.Request.User;
            if(oneObj.Request.Pass != null)
                _pass = oneObj.Request.Pass;
            
            //URIをパスとファイル名に分解する
            _path = oneObj.Request.Uri;
            _file = "";
            int index = oneObj.Request.Uri.LastIndexOf('/');
            if(index < oneObj.Request.Uri.Length - 1) {
                _path = oneObj.Request.Uri.Substring(0,index);
                _file = oneObj.Request.Uri.Substring(index + 1);
            }
        }

        override public void DebugLog() {
            var list = new List<string>();

            //すべてのプロキシが完了している
            list.Add(string.Format("[SSL] SOCK_STATE sv={0} cl={1} HostName={2}",Proxy.Sock(CS.Server).SockState,Proxy.Sock(CS.Client).SockState,Proxy.HostName));
            list.Add(string.Format("[SSL] {0}",_oneObj.Request.RequestStr));
            list.Add(string.Format("[SSL] buf sv={0} cl={1} pos sv={2} cl={3} ■WaitTime={4}sec",_oneObj.Body[CS.Server].Length,_oneObj.Body[CS.Client].Length,_oneObj.Pos[CS.Server],_oneObj.Pos[CS.Client],WaitTime));

            foreach(string s in list)
                Proxy.Logger.Set(LogKind.Debug,null,999,s);
        }


        //プロキシ処理
        override public bool Pipe(ILife iLife) {

            DataThread dataThread = null;
            var paramStr = "";

            //サーバ側との接続処理
            if(!Proxy.Connect(iLife,_oneObj.Request.HostName,_oneObj.Request.Port,_oneObj.Request.RequestStr,_oneObj.Request.Protocol)) {
                Proxy.Logger.Set(LogKind.Debug,null,999,"□Break proxy.Connect()==false");
                return false;
            }


            //wait 220 welcome
            if(!WaitLine("220",ref paramStr,iLife))
                return false;

            Proxy.Sock(CS.Server).AsciiSend(string.Format("USER {0}",_user));
            if(!WaitLine("331",ref paramStr,iLife))
                return false;
            
            Proxy.Sock(CS.Server).AsciiSend(string.Format("PASS {0}",_pass));
            if (!WaitLine("230", ref paramStr, iLife))
                return false;

            //Ver5.6.6
            if (_path == "/") {
                Proxy.Sock(CS.Server).AsciiSend("PWD");
                if (!WaitLine("257", ref paramStr, iLife))
                    return false;
                var tmp = paramStr.Split(' ');
                if (tmp.Length >= 1) {
                    _path = tmp[0].Trim(new[] { '"' });
                    if (_path[_path.Length - 1] != '/') {
                        _path = _path + "/";
                    }
                }
            }


            //リクエスト
            if(_path != "") {
                Proxy.Sock(CS.Server).AsciiSend(string.Format("CWD {0}",_path));
                if (!WaitLine("250", ref paramStr,iLife))
                    goto end;
            }

            Proxy.Sock(CS.Server).AsciiSend(_file == "" ? "TYPE A" : "TYPE I");
            if (!WaitLine("200", ref paramStr,iLife))
                goto end;

            //PORTコマンド送信
            var bindAddr = Proxy.Sock(CS.Server).LocalIp;
            // 利用可能なデータポートの選定
            while (iLife.IsLife()){
                DataPort++;
                if (DataPort >= 9999) {
                    DataPort = 2000;
                }                
                if (SockServer.IsAvailable(_kernel,bindAddr, DataPort)){
                    break;
                }
            }
            int listenPort = DataPort;

            //データスレッドの生成
            dataThread = new DataThread(_kernel,bindAddr,listenPort);

            // サーバ側に送るPORTコマンドを生成する
            string str = string.Format("PORT {0},{1},{2},{3},{4},{5}", bindAddr.IpV4[0], bindAddr.IpV4[1], bindAddr.IpV4[2], bindAddr.IpV4[3], listenPort / 256, listenPort % 256);
           
            
            
            Proxy.Sock(CS.Server).AsciiSend(str);
            if (!WaitLine("200", ref paramStr, iLife))
                goto end;

            if(_file == "") {
                Proxy.Sock(CS.Server).AsciiSend("LIST");
                if (!WaitLine("150", ref paramStr, iLife))
                    goto end;
            } else {
                Proxy.Sock(CS.Server).AsciiSend("RETR " + _file);
                if (!WaitLine("150", ref paramStr, iLife))
                    goto end;

            }
            
            //Ver5.0.2
            while(iLife.IsLife()) {
                if(!dataThread.IsRecv)
                    break;
            }

            if (!WaitLine("226", ref paramStr,iLife))
                goto end;

            byte[] doc;
            if(_file == "") {
                //受信結果をデータスレッドから取得する
                List<string> lines = Inet.GetLines(dataThread.ToString());
                //ＦＴＰサーバから取得したLISTの情報をHTMLにコンバートする
                doc = ConvFtpList(lines,_path);
            } else {
                doc = dataThread.ToBytes();
            }

            //クライアントへリプライ及びヘッダを送信する
            var header = new Header();
            header.Replace("Server", Util.SwapStr("$v", _kernel.Ver.Version(),(string)_conf.Get("serverHeader")));

            header.Replace("MIME-Version","1.0");
            
            if(_file == "") {
                header.Replace("Date",Util.UtcTime2Str(DateTime.UtcNow));
                header.Replace("Content-Type","text/html");
            } else {
                header.Replace("Content-Type","none/none");
            }
            header.Replace("Content-Length",doc.Length.ToString());

            Proxy.Sock(CS.Client).AsciiSend("HTTP/1.0 200 OK");//リプライ送信
            Proxy.Sock(CS.Client).SendUseEncode(header.GetBytes());//ヘッダ送信
            Proxy.Sock(CS.Client).SendNoEncode(doc);//ボディ送信
        end:
            if(dataThread != null)
                dataThread.Dispose();

            return false;
        }

        //ＦＴＰサーバから取得したLISTの情報をHTMLにコンバートする
        byte[] ConvFtpList(IEnumerable<string> lines,string path) {
            var tmp = new List<string>();
            tmp.Add("<head><title>");
            tmp.Add(string.Format("current directory \"{0}\"",path));
            tmp.Add("</title></head>");
            tmp.Add(string.Format("current directory \"{0}\"",path));
            tmp.Add("<hr>");
            tmp.Add("<pre>");
            tmp.Add(string.Format("<a href=\"{0}\">ParentDirector</a>",path + "../"));
            tmp.Add("");
            foreach(string line in lines) {
                var cols = line.Split(new[] { ' ','\t' },StringSplitOptions.RemoveEmptyEntries);
                
                //Ver5.6.6
                if (cols.Length == 4) {
                    if (cols[2].ToUpper() == "<DIR>") {
                        try {
                            var name = cols[3];

                            if (name == ".")
                                continue;
                            if (name == "..")
                                continue;

                            tmp.Add(string.Format("<a href=\"{0}/\" style=\"text-decoration: none\">{1,-50}</a>\t{2,7}\t-", path + name, name, "&ltDIR&gt"));
                        } catch {
                            tmp.Add(line);
                        }
                    } else {
                        tmp.Add(line);
                    }
                } else {
                    try {
                        string dir = cols[0];
                        string name = cols[8];
                        string size = cols[4];
                        string date = cols[5] + " " + cols[6] + " " + cols[7];

                        if (name == ".")
                            continue;
                        if (name == "..")
                            continue;

                        if (dir[0] == 'd') {
                            tmp.Add(string.Format("<a href=\"{0}/\" style=\"text-decoration: none\">{1,-50}</a>\t{2,7}\t-", path + name, name, "&ltDIR&gt"));
                        } else if (dir[0] == 'l') {
                            tmp.Add(string.Format("<a href=\"{0}/\" style=\"text-decoration: none\">{1,-50}</a>\t{2,7}\t-", path + name, name, "&ltLINK&gt"));
                        } else {
                            tmp.Add(string.Format("<a href=\"{0}\" style=\"text-decoration: none\">{1,-50}</a>\t{2,7}\t{3}", path + name, name, size, date));
                        }
                    } catch {
                        tmp.Add(line);
                    }
                }
            }
            tmp.Add("</pre>");

            //byte[]に変換する
            var doc = new byte[0];
            byte[] result = doc;
            foreach (string s in tmp){
                result = Bytes.Create(result, s + "\r\n");
            }
            return result;
        }
        
        //bool WaitLine(string cmd,ref bool life) {

        //    string cmdStr = "";
        //    string paramStr = "";

        //    string lastStr = "";

        //    while(life) {
        //        if(!server.WaitLine(proxy.Sock(CS.Server),ref cmdStr,ref paramStr)) {
        //            proxy.Sock(CS.Client).AsciiSend(lastStr);
        //            return false;
        //        }

        //        if (cmdStr == cmd)
        //            return true;

        //        lastStr = cmdStr + " " + paramStr;

        //        //Ver5.3.0 レスポンスコードが500番台（エラー）の場合、処理を中断する
        //        if (cmdStr[cmdStr.Length - 1] != '-') {
        //            //Ver5.6.3 最後が-で終わらない場合に例外が発生していた問題に対処
        //            //try {
        //            //    if (Int32.Parse(cmdStr) / 100 == 5) {
        //            //        proxy.Sock(CS.Client).AsciiSend(lastStr);
        //            //        return false;
        //            //    }
        //            //} catch {
        //            //    return false;
        //            //}
        //            int d = 0;
        //            if(Int32.TryParse(cmdStr,out d)){
        //                if(d/100==5){
        //                    proxy.Sock(CS.Client).AsciiSend(lastStr);
        //                    return false;
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}


        bool WaitLine(string cmd, ref string paramStr,ILife iLife) {

            string cmdStr = "";
            //string paramStr = "";

            string lastStr = "";

            while (iLife.IsLife()) {
                if (!_server.WaitLine(Proxy.Sock(CS.Server), ref cmdStr, ref paramStr)) {
                    Proxy.Sock(CS.Client).AsciiSend(lastStr);
                    return false;
                }

                if (cmdStr == cmd)
                    return true;

                lastStr = cmdStr + " " + paramStr;

                //Ver5.3.0 レスポンスコードが500番台（エラー）の場合、処理を中断する
                if (cmdStr[cmdStr.Length - 1] != '-') {
                    //Ver5.6.3 最後が-で終わらない場合に例外が発生していた問題に対処
                    //try {
                    //    if (Int32.Parse(cmdStr) / 100 == 5) {
                    //        proxy.Sock(CS.Client).AsciiSend(lastStr);
                    //        return false;
                    //    }
                    //} catch {
                    //    return false;
                    //}
                    int d;
                    if (Int32.TryParse(cmdStr, out d)) {
                        if (d / 100 == 5) {
                            Proxy.Sock(CS.Client).AsciiSend(lastStr);
                            return false;
                        }
                    }
                }
            }

            return false;
        }


        long WaitTime {
            get {
                return (DateTime.Now.Ticks - _lastRecvServer) / 1000 / 1000 / 10;
            }
        }
        class DataThread:IDisposable,ILife {

            SockTcp _sockTcp;
            Thread _t;
            //readonly Logger logger;
            bool _life = true;
            //ProxyFtp proxyFtp;

            byte[] _buffer = new byte[0];

            //Ver5.0.2
            public bool IsRecv { get; private set; }

            private Kernel _kernel;
            private Ip _ip;
            private int _listenPort;

            //public DataThread(Logger logger,sockTcp listenObj) {
            public DataThread(Kernel kernel,Ip ip,int listenPort){
                _kernel = kernel;
                _ip = ip;
                _listenPort = listenPort;
                
                //this.logger = logger;
                IsRecv = true;
                
                //パイプスレッドの生成
                _t = new Thread(Pipe) { IsBackground = true };
                _t.Start();
            }
            public void Dispose() {
                if(_t == null)
                    return;

                _life = false;
                while(_t.IsAlive) {
                    Thread.Sleep(100);
                }
            }


            void Pipe() {
                _sockTcp = SockServer.CreateConnection(_kernel, _ip, _listenPort, this);
                if (_sockTcp != null){
                    while (_life){
                        var len = _sockTcp.Length();
                        if (len > 0){
                            const int tout = 3; //受信バイト数がわかっているので、ここでのタイムアウト値はあまり意味が無い
                            var b = _sockTcp.Recv(len, tout, this);
                            if (b != null){
                                _buffer = Bytes.Create(_buffer, b);
                            }
                        }
                        if (_sockTcp.Length() == 0 && _sockTcp.SockState != SockState.Connect)
                            break;
                    }
                    _sockTcp.Close();
                }
                IsRecv = false;
            }
            override public string ToString() {
                return Encoding.GetEncoding("shift-jis").GetString(_buffer);
            }

            public bool IsLife(){
                return _life;
            }

            public byte[] ToBytes() {
                return _buffer;
            }

        }
    }
}
