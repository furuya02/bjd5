using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;



using BJD;

namespace ProxyHttpServer {



    //*************************************************************************
    //サーバクラスでアセンブリ外に公開されているのは、ServerMain()及びOption()のみ
    //*************************************************************************
    public partial class Server:SvBase {
        enum CHARSET {
            UNKNOWN=0,
            ASCII=1,
            SJIS=2,
            EUC=3,
            UTF8=4,
            UTF7=5,
            JIS=6//iso-2022-jp
        }

        int dataPortMin = 20000;
        int dataPortMax = 21000;
        int dataPort;

        Cache cache = null;
        // 上位プロキシを経由しないサーバのリスト
        List<string> disableAddressList = new List<string>();

        LimitUrl limitUrl;//URL制限
        LimitString limitString;//コンテンツ制限

        //リクエストを通常ログで表示する
        bool useRequestLog;

        public Server(Kanel kanel,OpBase opBase)
            : base(kanel, opBase, PROTOCOL_KIND.TCP, USE_ACL.ON) {

            cache = new Cache(this.Logger,this.OpBase);

            // 上位プロキシを経由しないサーバのリスト
            Dat dat = opBase.ValDat("disableAddress");
            foreach (OneLine oneLine in dat.Lines) {
                if (oneLine.Enabled) {//有効なデータだけを対象にする
                    disableAddressList.Add((string)oneLine.ValList[0].Obj);
                }
            }
            //URL制限
            limitUrl = new LimitUrl(opBase.ValDat("limitUrl"),(opBase.ValRadio("enableUrl")==0)?true:false);

            
            //リクエストを通常ログで表示する
            useRequestLog = opBase.ValBool("useRequestLog");

            //コンテンツ制限
            limitString = new LimitString(opBase.ValDat("limitString"));
            if (limitString.Length == 0)
                limitString = null;

            dataPort = dataPortMin;

            //初期化成功(isInitSuccess==trueでないとStart()を実行してもスレッド開始できない)
            isInitSuccess = true;

        }
        //終了処理
        override public void _dispose() {
            cache.Dispose();
        }

        //スレッド開始処理
        override public void _start() {
            cache.Start();
        }
        //スレッド停止処理
        override public void _stop() {
            cache.Stop();
        }

        //リモート操作（データの取得）
        override public string Cmd(string cmdStr) {

            if (cmdStr == "Refresh-DiskCache" || cmdStr == "Refresh-MemoryCache") {
                List<CacheInfo> infoList = new List<CacheInfo>();
                CACHE_KIND cacheKind = CACHE_KIND.DISK;
                if (cmdStr == "Refresh-MemoryCache")
                    cacheKind = CACHE_KIND.MEMORY;
                long size = cache.GetInfo(cacheKind,ref infoList);
                StringBuilder sb = new StringBuilder();
                foreach(CacheInfo cacheInfo in infoList){
                    sb.Append(cacheInfo.ToString()+"\b");
                }
                return sb.ToString();
            }else if (cmdStr.IndexOf("Cmd-Remove")==0) {

                string[] tmp = cmdStr.Split('\t');

                if (tmp.Length != 5)
                    return "false";
                CACHE_KIND kind = (CACHE_KIND)Enum.Parse(typeof(CACHE_KIND), tmp[1]);
                string hostName = tmp[2];
                int port = Convert.ToInt32(tmp[3]);
                string uri = tmp[4];
                if (cache.Remove(kind, hostName, port, uri))
                    return "true";
                return "false";
            }
            return "";
        }


        //接続単位の処理
        override public void _subThread(SockObj sockObj) {

            // 上位プロキシを使用するかどうかのフラグ
            bool useUpperProxy = this.OpBase.ValBool("useUpperProxy");

            Dictionary<CS,TcpObj> sock = new Dictionary<CS,TcpObj>(2);
            sock[CS.CLIENT] = (TcpObj)sockObj;
            sock[CS.SERVER] = null;

            sock[CS.CLIENT].Timeout = timeout;

            //クライアント及びサーバ双方のヘッダを処理するクラス
            Dictionary<CS,Header> header = new Dictionary<CS,Header>(2);
            header[CS.CLIENT] = new Header();
            header[CS.SERVER] = new Header();

            //クライアント及びサーバ双方のバッファ
            Dictionary<CS,byte[]> buf = new Dictionary<CS,byte[]>(2);
            buf[CS.CLIENT] = new byte[0];
            buf[CS.SERVER] = new byte[0];

            Request request = new Request();//クライアントからのリクエストを処理するクラス
            //Response response = new Response();//サーバからのレスポンスを処理するクラス
            OneCache oneCache = null;

            while(true) {
                //***************************************************************
                //クライアントからのデータを読み込む
                //***************************************************************
                {


                    //while (life && sock[CS.CLIENT].Length() == 0) {
                    //    Thread.Sleep(30);
                    //}
                    //接続されたが、クライアントからのリクエストが5秒間来ない場合は、スレッドを破棄する
                    for(int i = 0;life && sock[CS.CLIENT].Length() == 0;i++) {
                        Thread.Sleep(50);
                        if(i > 100) {
                            Logger.Set(LOG_KIND.DEBUG,sock[CS.CLIENT],999,"デバッグ中 クライアントから5秒以上データが来ないので切断する");
                            goto end;//Ver5.0.0-a21
                        }
                    }
                    //リクエスト取得（内部データは初期化される）ここのタイムアウト値は、大きすぎるとブラウザの切断を取得できないでブロックしてしまう
                    if(!request.Recv(this.Logger,sock[CS.CLIENT],timeout,ref life)) {
                        //Logger
                        goto end;
                    }
                    //bool useRequestLog リクエストを通常ログで表示する
                    this.Logger.Set(useRequestLog ? LOG_KIND.NOMAL : LOG_KIND.DETAIL,null,0,string.Format("{0}",request.RequestStr));


                    //***************************************************************
                    //URL制限
                    //***************************************************************
                    if(limitUrl.IsHit(request.RequestStr)) {
                        this.Logger.Set(LOG_KIND.NOMAL,null,10,request.RequestStr);
                        goto end;
                    }

                    //***************************************************************
                    //上位プロキシのチェック
                    //***************************************************************
                    if(useUpperProxy) {
                        // 上位プロキシを経由しないサーバの確認
                        foreach(string hostName in disableAddressList) {
                            if(request.Protocol == PROXY_PROTOCOL.SSL) {
                                if(request.HostName.IndexOf(hostName) == 0) {
                                    useUpperProxy = false;
                                    break;
                                }
                            } else {
                                string str = request.RequestStr.Substring(11);
                                if(str.IndexOf(hostName) == 0) {
                                    useUpperProxy = false;
                                    break;
                                }
                            }

                        }
                    }

                    //ヘッダの取得
                    if(!header[CS.CLIENT].Recv(sock[CS.CLIENT],timeout,ref life)) {
                        //Logger
                        goto end;
                    }

                    //ヘッダの追加処理
                    if(request.Protocol == PROXY_PROTOCOL.HTTP) {
                        if(!this.OpBase.ValBool("useBrowserHedaer")) {
                            if(this.OpBase.ValBool("addHeaderRemoteHost")) {
                                header[CS.CLIENT].Append("Remote-Host-Wp",Define.ServerAddress());
                            }
                            if(this.OpBase.ValBool("addHeaderXForwardedFor")) {
                                header[CS.CLIENT].Append("X-Forwarded-For",Define.ServerAddress());
                            }
                            if(this.OpBase.ValBool("addHeaderForwarded")) {
                                string str = string.Format("by {0} (Version {1}) for {2}",Define.ApplicationName(),kanel.Ver.Version(),Define.ServerAddress());
                                header[CS.CLIENT].Append("Forwarded",str);
                            }
                        }
                    }



                    if(request.Protocol == PROXY_PROTOCOL.SSL) {
                        if(!useUpperProxy) {
                            //取得したリクエストをバッファに格納する
                            buf[CS.CLIENT] = new byte[0];
                            buf[CS.SERVER] = Bytes.Create("HTTP/1.0 200 Connection established\r\n\r\n");//CONNECTが成功したことをクライアントに返す
                        } else {
                            //上位プロキシを使用する場合(リクエストラインはそのまま使用される)
                            buf[CS.CLIENT] = Bytes.Create(request.SendLine(useUpperProxy),header[CS.CLIENT].GetBytes());
                        }
                    } else if(request.Protocol == PROXY_PROTOCOL.HTTP) {//HTTPの場合
                        //Ver5.0.0-b3 削除
                        //header[CS.CLIENT].Remove("Proxy-Connection");

                        //取得したリクエストをバッファに格納する
                        //上位プロキシを使用する場合(リクエストラインはそのまま使用される)
                        buf[CS.CLIENT] = Bytes.Create(request.SendLine(useUpperProxy),header[CS.CLIENT].GetBytes());

                        //Ver5.0.0-a5
                        //POSTの場合は、更にクライアントからのデータを読み込む
                        if(request.Method == HTTP_METHOD.POST) {
                            //int len = 0;
                            string strContentLength = header[CS.CLIENT].GetVal("Content-Length");
                            if(strContentLength != null) {
                                try {
                                    int len = Convert.ToInt32(strContentLength);
                                    if(0 < len) {
                                        byte[] data = sock[CS.CLIENT].Recv(len,timeout);
                                        buf[CS.CLIENT] = Bytes.Create(buf[CS.CLIENT],data);
                                    }

                                } catch {
                                    this.Logger.Set(LOG_KIND.ERROR,null,22,request.Uri);
                                    goto end;
                                }
                            }
                        }
                    }

                }

                //キャッシュ対象のリクエストかどうかの確認
                if(request.Protocol == PROXY_PROTOCOL.HTTP && !request.Cgi) {
                    if(cache.IsTarget(request.HostName,request.Uri,request.Ext)) {

                        string headerStr = header[CS.CLIENT].ToString();
                        bool noCache = false;
                        if(headerStr.ToLower().IndexOf("no-cache") >= 0) {
                            noCache = true;//キャッシュしない
                            this.Logger.Set(LOG_KIND.DETAIL,null,16,request.Uri);
                            cache.Remove(request.HostName,request.Port,request.Uri);//存在する場合は、無効化する
                        } else {
                            string modifiedStr = header[CS.CLIENT].GetVal("If-Modified-Since");
                            DateTime modified = Util.Str2Time(modifiedStr);
                            byte[] dat = cache.Get(request,modified);
                            if(dat != null) {//キャッシュが見つかった場合
                                this.Logger.Set(LOG_KIND.DETAIL,null,14,request.Uri);
                                sock[CS.CLIENT].AsciiSend("HTTP/1.0 200 OK",OPERATE_CRLF.YES);
                                int c = sock[CS.CLIENT].Send(dat);
                                goto end;
                            }
                        }
                        if(!noCache) {
                            string url = string.Format("{0}:{1}{2}",request.HostName,request.Port,request.Uri);
                            //キャッシュ対象の場合だけ、受信用のオブジェクトを生成する
                            oneCache = new OneCache(request.HostName,request.Port,request.Uri);
                        }
                    }
                }

                //***************************************************************
                // サーバとの接続
                //***************************************************************
                {
                    string hostName = request.HostName;
                    int port = request.Port;

                    if(useUpperProxy) {//上位プロキシを使用する場合
                        hostName = this.OpBase.ValString("upperProxyServer");
                        port = this.OpBase.ValInt("upperProxyPort");
                    }


                    List<Ip> ipList = new List<Ip>();
                    ipList.Add(new Ip(hostName));
                    if(ipList[0].ToString() == "0.0.0.0") {
                        ipList = kanel.dnsCache.Get(hostName);
                        if(ipList == null || ipList.Count == 0) {
                            this.Logger.Set(LOG_KIND.ERROR,null,11,hostName);
                            goto end;
                        }
                    }
                    SSL ssl = null;
                    foreach(Ip ip in ipList) {
                        sock[CS.SERVER] = Inet.Connect(ref life,kanel,this.Logger,ip,port,ssl);
                        if(sock[CS.SERVER] != null)
                            break;
                    }
                    if(sock[CS.SERVER] == null) {
                        Logger.Set(LOG_KIND.DETAIL,sock[CS.CLIENT],26,string.Format("{0}:{1}",ipList[0].ToString(),port));
                        return;
                    }
                    sock[CS.SERVER].Timeout = timeout;
                }


                //***************************************************************
                // パイプ処理
                //***************************************************************
                if(request.Protocol == PROXY_PROTOCOL.HTTP || request.Protocol == PROXY_PROTOCOL.SSL) {
                    // パイプ(HTTP/SSL)

                    PipeHttp(sock,buf,request,header,oneCache);//パイプ処理
                } else if(request.Protocol == PROXY_PROTOCOL.FTP) {
                    // パイプ(FTP)

                    dataPort = PipeFtp(sock,request,dataPort);//パイプ処理
                    if(dataPort > dataPortMax)
                        dataPort = dataPortMin;

                }
                continue;
            end:
                break;
                //***************************************************************
                // 終了処理
                //***************************************************************
                //if(sock[CS.CLIENT] != null)
                //    sock[CS.CLIENT].Close();
                //if(sock[CS.SERVER] != null)
                //    sock[CS.SERVER].Close();

            }
            //***************************************************************
            // 終了処理
            //***************************************************************
            if(sock[CS.CLIENT] != null)
                sock[CS.CLIENT].Close();
            if(sock[CS.SERVER] != null)
                sock[CS.SERVER].Close();
        }

        //***************************************************************
        // パイプ(HTTP/SSL)
        //***************************************************************
        void PipeHttp(Dictionary<CS, TcpObj> sock, Dictionary<CS, byte[]> buf, Request request, Dictionary<CS, Header> header, OneCache oneCache) {
            Response response = new Response();//サーバからのレスポンスを処理するクラス

            //***************************************************************
            // パイプ
            //***************************************************************
            bool serverHeda = false;
            if (request.Protocol == PROXY_PROTOCOL.HTTP)
                serverHeda = true;//ヘッダ受信するのはHTTPの時だけ

            bool isText = false;//コンテンツ制限の対象かどうかのフラグ
            CHARSET charset = CHARSET.UNKNOWN;
            
            long contentLength = 0;//サーバのヘッダで示される受信データのサイズ
            long sendLength = 0;//クライアント側へ送信完了したデータのサイズ
            
            long timeoutCounter = 0;//タイムアウトカウンタ
            CS cs = CS.SERVER;

          
            while (life) {

                Thread.Sleep(0);

                cs = Reverse(cs);//サーバ側とクライアント側を交互に処理する

                //Thread.Sleep(0);//Ver5.0.0-a19

                // クライアントの切断の確認
                if (sock[CS.CLIENT].State != SOCKET_OBJ_STATE.CONNECT) {

                    if (buf[CS.SERVER].Length == 0) {
                        cache.Add(oneCache);
                    }

                    //クライアントが切断された場合は、処理を継続する意味が無い
                    this.Logger.Set(LOG_KIND.DETAIL,null,8,"close client");

                    break;
                }


                //*******************************************************
                //処理するデータが到着していない場合の処理
                //*******************************************************
                if (sock[CS.CLIENT].Length() == 0 && sock[CS.SERVER].Length() == 0 && buf[CS.CLIENT].Length == 0 && buf[CS.SERVER].Length == 0) {

                    // サーバの切断の確認
                    if (sock[CS.SERVER].State != SOCKET_OBJ_STATE.CONNECT) {

                        cache.Add(oneCache);

                        //送信するべきデータがなく、サーバが切断された場合は、処理終了
                        this.Logger.Set(LOG_KIND.DETAIL,null,8,"close server");
                        break;
                    }
                    //Ver5.0.0-a3 HTTP/1.1対応（次のリクエスト待ちへ進む）
                    if(!serverHeda){
                        int x = 10;
                    }

                    //Ver5.0.0-a11 
                    //if (response.Code != 0 && <response.Code != 200) {
                    if (response.Code != 0 && (response.Code<200 && 300<=response.Code)) {
                        //見切り切断
                        this.Logger.Set(LOG_KIND.DETAIL,sock[CS.SERVER],8,string.Format("Response Code = {0}",response.Code));
                        break;
                    }

                    //Thread.Sleep(50);
                    Thread.Sleep(1);//Ver5.0.0-a19

                    //タイムアウトカウンタのインクリメント
                    timeoutCounter++;
                    //if(timeoutCounter*50>timeout*1000){
                    if(timeoutCounter > timeout*1000){
                        //タイムアウト
                        this.Logger.Set(LOG_KIND.NOMAL,sock[CS.SERVER],3,string.Format("option TIMEOUT={0}sec",timeout));
                        break;
                    }

                } else {
                    timeoutCounter = 0;//タイムアウトカウンタのリセット

                }


                //*******************************************************
                // 受信処理 
                //*******************************************************
                if(buf[cs].Length==0){ //バッファが空の時だけ処理する

                    if(!serverHeda && cs == CS.CLIENT && buf[CS.SERVER].Length == 0) {//次のリクエスト
                        int x = 10;
                    }

                    if (cs == CS.SERVER && serverHeda) {// HTTPのヘッダ受信

                        while (life && sock[CS.SERVER].State == SOCKET_OBJ_STATE.CONNECT && sock[CS.CLIENT].State == SOCKET_OBJ_STATE.CONNECT && sock[CS.SERVER].Length() == 0) {
                            Thread.Sleep(30);
                        }
                        //レスポンスの取得
                        if (!response.Recv(this.Logger, sock[CS.SERVER],timeout,ref life)) {
                            this.Logger.Set(LOG_KIND.ERROR,sock[CS.SERVER],6,"");
                            break;
                        }
                        if (oneCache != null) {
                            if (response.Code != 200) {//200以外は、キャッシュ保存の対象にならないので無効化する
                                //this.Logger.Set(LOG_KIND.DEBUG, 222, string.Format("response code [{0}]", response.Code));
                                oneCache = null;
                            }
                        }
                        //ヘッダの受信    v2.1.6 すべてのレスポンスをConnectint: close にして、リクエストの抜けを排除する　HTTP/1.1でも擬似的に使用できるようになる
                        if (!header[cs].Recv(sock[cs], timeout, ref life)) {
                            this.Logger.Set(LOG_KIND.ERROR,sock[CS.SERVER],7,"");
                            break;
                        }
                        
                        //Ver5.0.0-a22
                        //サーバからのヘッダにno-cacheが指定されている場合も、キャッシュ対象からはずす
                        string headerStr = header[cs].ToString();
                        if(headerStr.ToLower().IndexOf("no-cache") >= 0) {
                            this.Logger.Set(LOG_KIND.DETAIL,null,16,request.Uri);
                            cache.Remove(request.HostName,request.Port,request.Uri);//存在する場合は、無効化する
                            oneCache = null;
                        }


                        if (oneCache != null)
                            oneCache.Add(header[CS.SERVER]);
                        
                        //サーバから受信したレスポンス及びヘッダをバッファに展開する
                        buf[cs] = Bytes.Create(response.ToString(),"\r\n",header[cs].GetBytes());
                        serverHeda = false;//ヘッダ処理終了
                        string str = header[CS.SERVER].GetVal("Content-Length");
                        if (str != null) {
                            contentLength = Convert.ToInt32(str);
                            //Ver5.0.0-a19
                            //クライアントに送信するのは、本体＋ヘッダとなるので
                            contentLength += buf[cs].Length;
                        }

                        //コンテンツ制限の対象かどうかのフラグを設定する
                        if(limitString!=null){//コンテンツ制限に文字列が設定されている場合
                            string contentType = header[CS.SERVER].GetVal("Content-Type");
                            if (contentType != null) {
                                if (contentType.ToLower().IndexOf("text/h") == 0) {
                                    isText = true;
                                }
                                if (contentType.ToLower().IndexOf("text/t") == 0) {
                                    isText = true;
                                }
                            }
                        }

                    }else{
                        //処理すべきデータ数の取得
                        int len = sock[cs].Length();
                        byte[] b = sock[cs].Recv(len,timeout);
                        if(b != null) {
                            if(cs == CS.CLIENT) {
                                string s = Encoding.ASCII.GetString(b);
                                int x = 10;
                            }
                            buf[cs] = Bytes.Create(buf[cs],b);
                            //logger.Set(LOG_KIND.DEBUG, 0, string.Format("cs={0} Recv()={1}", cs, b.Length));

                            if(cs == CS.SERVER) {
                                if(oneCache != null)
                                    oneCache.Add(b);//キャッシュ

                                //コンテンツ制限
                                if(isText) {
                                    if(charset == CHARSET.UNKNOWN) {
                                        string s = Encoding.ASCII.GetString(b);
                                        int index = s.ToLower().IndexOf("charset");
                                        if(0 <= index) {
                                            s = s.Substring(index + 8);
                                            if(s.ToLower().IndexOf("x-sjis") >= 0) {
                                                charset = CHARSET.SJIS;
                                            } else if(s.ToLower().IndexOf("shift_jis") >= 0) {
                                                charset = CHARSET.SJIS;
                                            } else if(s.ToLower().IndexOf("x-euc-jp") >= 0) {
                                                charset = CHARSET.EUC;
                                            } else if(s.ToLower().IndexOf("euc-jp") >= 0) {
                                                charset = CHARSET.EUC;
                                            } else if(s.ToLower().IndexOf("utf-8") >= 0) {
                                                charset = CHARSET.UTF8;
                                            } else if(s.ToLower().IndexOf("utf-7") >= 0) {
                                                charset = CHARSET.UTF7;
                                            } else if(s.ToLower().IndexOf("iso-2022-jp") >= 0) {
                                                charset = CHARSET.JIS;
                                            } else {
                                                //int k = 0;
                                            }
                                        }
                                    }
                                    string str = "";
                                    switch(charset) {
                                        case CHARSET.ASCII:
                                            str = Encoding.ASCII.GetString(b);
                                            break;
                                        case CHARSET.SJIS:
                                            str = Encoding.GetEncoding("shift-jis").GetString(b);
                                            break;
                                        case CHARSET.EUC:
                                            str = Encoding.GetEncoding("euc-jp").GetString(b);
                                            break;
                                        case CHARSET.JIS:
                                            str = Encoding.GetEncoding(50222).GetString(b);
                                            break;
                                        case CHARSET.UTF8:
                                            str = Encoding.UTF8.GetString(b);
                                            break;
                                        case CHARSET.UTF7:
                                            str = Encoding.UTF7.GetString(b);
                                            break;
                                        case CHARSET.UNKNOWN:
                                            str = Encoding.ASCII.GetString(b);
                                            break;
                                    }
                                    //コンテンツ制限
                                    string hitStr = limitString.IsHit(str);
                                    if(hitStr != null) {
                                        //制限にヒットした場合
                                        this.Logger.Set(LOG_KIND.NOMAL,sock[CS.SERVER],21,hitStr);
                                        break;
                                    }
                                }
                            }
                        }

                    }

                }


                 //*******************************************************
                // 送信処理
                //*******************************************************
                if (buf[cs].Length != 0) { //バッファにデータが入っている場合だけ処理する
                    int c = 0;
                    c = sock[Reverse(cs)].Send(buf[cs]);

                    if (cs == CS.SERVER) {//サーバ側から受信したデータをクライアント側に送信した分だけカウントする
                        if(contentLength!=0){
                            sendLength += c;
                            //クライアント側への送信完了時に確認する                        
                            //受信データサイズのすべてがクライアントへ送信完了したとき切断する
                            if (contentLength <= sendLength) {
                                cache.Add(oneCache);
                                this.Logger.Set(LOG_KIND.DETAIL,sock[CS.SERVER],8,"compleate");
                                break;
                            }
                        }
                    
                    }

                    //logger.Set(LOG_KIND.DEBUG, 0, string.Format("cs={0} Send()={1}", Reverse(cs), c));
                    if (c == buf[cs].Length) {
                        buf[cs] = new byte[0];
                        
                        //クライアント側への送信完了時に確認する                        
                        //if(cs==CS.SERVER){
                        //    //受信データサイズのすべてがクライアントへ送信完了したとき切断する
                        //    if (contentLength != 0 && contentLength <= sendLength) {
                        //        cache.Add(oneCache);
                        //        this.Logger.Set(LOG_KIND.DETAIL,sock[CS.SERVER],8,"compleate");
                        //        break;
                        //    }
                        //}


                    } else {
                        this.Logger.Set(LOG_KIND.ERROR,sock[CS.SERVER],9,string.Format("sock.Send() return {0}",c));
                        break;
                    }

                }
 
            }
        }
        //***************************************************************
        // パイプ(FTP)
        //***************************************************************
        int PipeFtp(Dictionary<CS, TcpObj> sock, Request request,int dataPort) {
            DataThread dataThread = null;

            //ユーザ名及びパスワード
            string user = "anonymous";
            string pass = this.OpBase.ValString("anonymousAddress");

            //Ver5.0.0-a23 URLで指定されたユーザ名およびパスワードを使用する
            if(request.User != null)
                user = request.User;
            if(request.Pass != null)
                pass = request.Pass;

            //wait 220 welcome
            if(!WaitLine(sock,"220"))
                goto end;

            sock[CS.SERVER].AsciiSend(string.Format("USER {0}",user),OPERATE_CRLF.YES);
            if(!WaitLine(sock,"331"))
                goto end;

            sock[CS.SERVER].AsciiSend(string.Format("PASS {0}",pass),OPERATE_CRLF.YES);
            if(!WaitLine(sock,"230"))
                goto end;

            //URIをパスとファイル名に分解する
            string path = request.Uri;
            string file = "";
            int index = request.Uri.LastIndexOf('/');
            if (index < request.Uri.Length - 1) {
                path = request.Uri.Substring(0, index);
                file = request.Uri.Substring(index + 1);
            }

            //リクエスト
            if (path != "") {
                sock[CS.SERVER].AsciiSend(string.Format("CWD {0}",path),OPERATE_CRLF.YES);
                if (!WaitLine(sock, "250"))
                    goto end;
            }

            if (file == "") {
                sock[CS.SERVER].AsciiSend("TYPE A",OPERATE_CRLF.YES);
            } else {
                sock[CS.SERVER].AsciiSend("TYPE I",OPERATE_CRLF.YES);
            }
            if(!WaitLine(sock,"200"))
                goto end;

            //PORTコマンド送信
            Ip bindAddr = new Ip(sock[CS.SERVER].LocalEndPoint.Address.ToString());
            // 利用可能なデータポートの選定
            int listenMax = 1;
            SSL ssl = null;
            TcpObj listenObj=null;
            while(life){
                listenObj = new TcpObj(kanel, this.Logger,bindAddr, dataPort++, listenMax, ssl);
                if (listenObj.State != SOCKET_OBJ_STATE.ERROR)
                    break;
            }
            if(listenObj==null)
                goto end;

            //データスレッドの生成
            dataThread = new DataThread(this.Logger, listenObj);


            // サーバ側に送るPORTコマンドを生成する
            string str = string.Format("PORT {0},{1},{2},{3},{4},{5}",bindAddr.IpV4[0],bindAddr.IpV4[1],bindAddr.IpV4[2],bindAddr.IpV4[3],(listenObj.LocalEndPoint.Port) / 256,(listenObj.LocalEndPoint.Port) % 256);
            sock[CS.SERVER].AsciiSend(str,OPERATE_CRLF.YES);
            if(!WaitLine(sock,"200"))
                goto end;

            if(file==""){
                sock[CS.SERVER].AsciiSend("LIST",OPERATE_CRLF.YES);
                if(!WaitLine(sock,"150"))
                    goto end;
            }else{
                sock[CS.SERVER].AsciiSend("RETR " + file,OPERATE_CRLF.YES);
                if(!WaitLine(sock,"150"))
                    goto end;
            
            }
            if(!WaitLine(sock,"226"))
                goto end;
            
            byte[] doc = new byte[0];
            if (file == "") {
                //受信結果をデータスレッドから取得する
                List<string> lines = Inet.GetLines(dataThread.ToString());
                //ＦＴＰサーバから取得したLISTの情報をHTMLにコンバートする
                doc = ConvFtpList(lines, path);
            } else {
                doc = dataThread.ToBytes();
            }
            
            //クライアントへリプライ及びヘッダを送信する
            Header header = new Header();
            header.Replace("Server",Util.SwapStr("$v",kanel.Ver.Version(),this.OpBase.ValString("serverHeader")));
            header.Replace("MIME-Version","1.0");
            if (file == "") {
                header.Replace("Date",Util.UtcTime2Str(DateTime.UtcNow));
                header.Replace("Content-Type","text/html");
            } else {
                header.Replace("Content-Type","none/none");
            }
            header.Replace("Content-Length",doc.Length.ToString());

            sock[CS.CLIENT].AsciiSend("HTTP/1.0 200 OK",OPERATE_CRLF.YES);//リプライ送信
            sock[CS.CLIENT].Send(header.GetBytes());//ヘッダ送信
            sock[CS.CLIENT].Send(doc);//ボディ送信
        end:
            if (dataThread != null)
                dataThread.Dispose();

            return dataPort;
        }

        bool WaitLine(Dictionary<CS, TcpObj> sock, string cmd) {
            
            string cmdStr = "";
            string paramStr = "";
            
            string lastStr = "";

            while (life) {
                if (!WaitLine(sock[CS.SERVER], ref cmdStr, ref paramStr)) {
                    sock[CS.CLIENT].AsciiSend(lastStr,OPERATE_CRLF.YES);
                    return false;
                }
                if (cmdStr == cmd)
                    return true;
                lastStr = cmdStr + " " + paramStr;
            }
            return false;
        }
        //ＦＴＰサーバから取得したLISTの情報をHTMLにコンバートする
        byte [] ConvFtpList(List<string>lines,string path) {
            List<string> tmp = new List<string>();
            tmp.Add("<head><title>");
            tmp.Add(string.Format("current directory \"{0}\"", path));
            tmp.Add("</title></head>");
            tmp.Add(string.Format("current directory \"{0}\"", path));
            tmp.Add("<hr>");
            tmp.Add("<pre>");
            tmp.Add(string.Format("<a href=\"{0}\">ParentDirector</a>", path + "../"));
            tmp.Add("");
            foreach (string line in lines) {
                string[] cols = line.Split(new char[]{' ', '\t'},StringSplitOptions.RemoveEmptyEntries);
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
                    }else if (dir[0] == 'l') {
                        tmp.Add(string.Format("<a href=\"{0}/\" style=\"text-decoration: none\">{1,-50}</a>\t{2,7}\t-", path+name, name,"&ltLINK&gt"));
                    } else {
                        tmp.Add(string.Format("<a href=\"{0}\" style=\"text-decoration: none\">{1,-50}</a>\t{2,7}\t{3}", path + name, name, size, date));
                    }
                } catch {
                    tmp.Add(line);
                }
            }
            tmp.Add("</pre>");
            
            //byte[]に変換する
            byte[] doc = new byte[0];
            foreach(string str in tmp){
                doc = Bytes.Create(doc,str+"\r\n");
            }
            return doc;

        }
        class DataThread:IDisposable {

            TcpObj tcpObj = null;
            Thread t;
            Logger logger;
            bool life = true;

            byte[] buffer = new byte[0];

            public DataThread(Logger logger, TcpObj listenObj) {
                this.logger = logger;
                listenObj.StartServer(new AsyncCallback(callBack));
            }
            public void Dispose() {
                if (t == null)
                    return;

                life = false;
                while (t.IsAlive) {
                    Thread.Sleep(100);
                }
            }

            void callBack(IAsyncResult ar) {
                //接続完了（Listenソケットを取得）
                SockObj sockObj = (SockObj)(ar.AsyncState);
                //接続ソケットを保持して
                tcpObj = (TcpObj)sockObj.CreateChildObj(ar);
                //Listenソケットをクローズする
                sockObj.Close();

                //パイプスレッドの生成
                t = new Thread(new ThreadStart(Pipe));
                t.IsBackground = true;
                t.Start();
            }

            void Pipe() {
                //int idleTime = 0;
                while (life) {
                    int len = tcpObj.Length();
                    if (len > 0) {
                        int tout = 3;//受信バイト数がわかっているので、ここでのタイムアウト値はあまり意味が無い
                        byte[] b = tcpObj.Recv(len, tout);
                        if (b != null) {
                            buffer = Bytes.Create(buffer, b);
                        }
                    }
                    if (tcpObj.Length()==0 && tcpObj.State != SOCKET_OBJ_STATE.CONNECT)
                        break;
                }
                tcpObj.Close();
            }
            override public string ToString(){
                return Encoding.GetEncoding("shift-jis").GetString(buffer);
            }
            public byte [] ToBytes() {
                return buffer;
            }

        }
        CS Reverse(CS cs){
            if (cs == CS.CLIENT)
                return CS.SERVER;
            return CS.CLIENT;
        }

    }
}

