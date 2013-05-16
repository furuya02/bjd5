using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BJD;

namespace ProxyHttpServer {
    class ProxyHttp {

        OpBase opBase;//オプションヘッダの追加のため
        Kanel kanel;//オプションヘッダの追加のため
        
        Proxy proxy;
        public Cache Cache { get; private set; }
        public LimitString LimitString { get; private set; }

        
        //データオブジェクト
        List<OneHttp> ar = new List<OneHttp>();
        int indexServer = 0;//サーバ側にどこまで送信を完了したかのインデックス
        int indexClient = 0;//クライアント側にどこまで送信を完了したかのインデックス
        int indexRecv = 0;//サーバ側からのデータを、どこまで受信完了したかのインデックス

        public ProxyHttp(Kanel kanel,OpBase opBase,Proxy proxy,Cache cache,LimitString limitString) {
            this.kanel = kanel;
            this.opBase = opBase;
            this.proxy = proxy;
            this.Cache = cache;
            this.LimitString = limitString;
        }
        
        //クライアントへの送信がすべて完了しているかどうかの確認
        public bool IsFinish() {
            if(indexClient == ar.Count) 
                return true;
            return false;
        }
        //データオブジェクトの追加
        public void Add(OneObj oneObj) {
            
            //オプション指定によるヘッダの追加処理
            if(!opBase.ValBool("useBrowserHedaer")) {
                if(opBase.ValBool("addHeaderRemoteHost")) {
                    //    oneObj.Header[cs].Append(key,val);
                    oneObj.Header[CS.CLIENT].Append("Remote-Host-Wp",Define.ServerAddress());
                }
                if(opBase.ValBool("addHeaderXForwardedFor")) {
                    oneObj.Header[CS.CLIENT].Append("X-Forwarded-For",Define.ServerAddress());
                }
                if(opBase.ValBool("addHeaderForwarded")) {
                    string str = string.Format("by {0} (Version {1}) for {2}",Define.ApplicationName(),kanel.Ver.Version(),Define.ServerAddress());
                    oneObj.Header[CS.CLIENT].Append("Forwarded",str);
                }
            }

            OneHttp oneHttp = new OneHttp(proxy,this,oneObj);
            //キャッシュの確認
            oneHttp.CacheConform();
            ar.Add(oneHttp);
        }
        

        public List<string> Debug() {
            List<string> list = new List<string>();

            //すべてのプロキシが完了している
            if(indexClient == ar.Count) {
                list.Add(string.Format("[HTTP] SOCK_STATE sv={0} cl={1} Finish/{2} HostName={3}",proxy.Sock(CS.SERVER).State,proxy.Sock(CS.CLIENT).State,ar.Count,proxy.HostName));
            } else {
                list.Add(string.Format("[HTTP] SOCK_STATE sv={0} cl={1} {2}/{3} HostName={4}",proxy.Sock(CS.SERVER).State,proxy.Sock(CS.CLIENT).State,ar.Count,indexClient,proxy.HostName));
                for(int i = 0;i < ar.Count;i++) {
                    List<string> l = ar[i].Debug(i);
                    foreach(string s in l)
                        list.Add(s);
                }
            }
            return list;
        }


        //プロキシ処理
        public bool Pipe(ref bool life) {

            if(!SendServer(ref life))//サーバへの送信
                return false;
            if(!RecvServer(ref life))//サーバからの受信
                return false;
            if(!SendClient(ref life))//クライアントへの送信
                return false;

            if(proxy.Sock(CS.SERVER).State != SOCKET_OBJ_STATE.CONNECT){
                if(indexClient == ar.Count) {
                    return false;
                }
            }

            return true;
        }

        //サーバ側への送信
        bool SendServer(ref bool life) {
            for(int i = indexServer;life && i < ar.Count;i++) {
                //次のオブジェクトの接続先が現在接続中のサーバと違う場合
                if(proxy.Sock(CS.SERVER)!=null && ar[i].HostName != proxy.HostName) {
                    //既存のプロキシ処理が完了するまで、次のサーバ送信（リクエスト送信）は待機となる
                    if(i<indexClient)
                        return true;
                }
                if(!ar[i].SendServer(ref life)) {
                    return false;
                }
                proxy.Logger.Set(LOG_KIND.DEBUG,null,999,string.Format("■indexServer {0}->{1}",indexServer,indexServer + 1));
                indexServer++;
            }
            return true;
        }
        
        //クライアント側への送信
        bool SendClient(ref bool life) {
            for(int i = indexClient;life && i < ar.Count;i++) {
                if(!ar[i].SendClient(ref life)) {
                    return false;
                }
                //クライアントへの送信が完了しているかどうかの確認
                if(ar[i].SideState(CS.CLIENT) != HTTP_SIDE_STATE.CLIENT_SIDE_SEND_BODY) {
                    break;
                }
                //if(indexClient!=0)
                    proxy.Logger.Set(LOG_KIND.DEBUG,null,999,string.Format("■indexClient {0}->{1}",indexClient,indexClient+1));
                //送信が完了している場合は、次のデータオブジェクトの処理に移行する
                indexClient++;

            }
            return true;
        }

        //サーバ側からの受信
        bool RecvServer(ref bool life) {
            for(int i = indexRecv;life && i < ar.Count;i++) {
                if(!ar[i].RecvServer(ref life)) {
                    proxy.Logger.Set(LOG_KIND.DEBUG,null,999,"[HTPP] ■Break RecvServer()");
                    return false;
                }
                //サーバ側からの受信が完了しているかどうかの確認
                if(ar[i].SideState(CS.SERVER) != HTTP_SIDE_STATE.SERVER_SIDE_RECV_BODY)
                    break;
                //送信が完了しているばあは、次のデータオブジェクトの処理に移る
                indexRecv++;
            }
            return true;
        }
           

                //コンテンツ制限
                //if(isText) {
                //    if(charset == CHARSET.UNKNOWN) {
                //        string s = Encoding.ASCII.GetString(b);
                //        int index = s.ToLower().IndexOf("charset");
                //        if(0 <= index) {
                //            s = s.Substring(index + 8);
                //            if(s.ToLower().IndexOf("x-sjis") >= 0) {
                //                charset = CHARSET.SJIS;
                //            } else if(s.ToLower().IndexOf("shift_jis") >= 0) {
                //                charset = CHARSET.SJIS;
                //            } else if(s.ToLower().IndexOf("x-euc-jp") >= 0) {
                //                charset = CHARSET.EUC;
                //            } else if(s.ToLower().IndexOf("euc-jp") >= 0) {
                //                charset = CHARSET.EUC;
                //            } else if(s.ToLower().IndexOf("utf-8") >= 0) {
                //                charset = CHARSET.UTF8;
                //            } else if(s.ToLower().IndexOf("utf-7") >= 0) {
                //                charset = CHARSET.UTF7;
                //            } else if(s.ToLower().IndexOf("iso-2022-jp") >= 0) {
                //                charset = CHARSET.JIS;
                //            } else {
                //                //int k = 0;
                //            }
                //        }
                //    }
                //    string str = "";
                //    switch(charset) {
                //        case CHARSET.ASCII:
                //            str = Encoding.ASCII.GetString(b);
                //            break;
                //        case CHARSET.SJIS:
                //            str = Encoding.GetEncoding("shift-jis").GetString(b);
                //            break;
                //        case CHARSET.EUC:
                //            str = Encoding.GetEncoding("euc-jp").GetString(b);
                //            break;
                //        case CHARSET.JIS:
                //            str = Encoding.GetEncoding(50222).GetString(b);
                //            break;
                //        case CHARSET.UTF8:
                //            str = Encoding.UTF8.GetString(b);
                //            break;
                //        case CHARSET.UTF7:
                //            str = Encoding.UTF7.GetString(b);
                //            break;
                //        case CHARSET.UNKNOWN:
                //            str = Encoding.ASCII.GetString(b);
                //            break;
                //    }
                //    //コンテンツ制限
                //    string hitStr = limitString.IsHit(str);
                //    if(hitStr != null) {
                //        //制限にヒットした場合
                //        this.Logger.Set(LOG_KIND.NOMAL,sock[CS.SERVER],21,hitStr);
                //        return false;
                //    }
                //}


   
    }

    enum HTTP_SIDE_STATE {
        NON,

        CLIENT_SIDE_RECV_REQUEST,//リクエスト受信完了 HTTP(CLIENT_SIDE)
        CLIENT_SIDE_SEND_HEADER,//ヘッダ送信完了      HTTP(CLIENT_SIDE)
        CLIENT_SIDE_SEND_BODY,//本体送信完了          HTTP(CLIENT_SIDE)

        SERVER_SIDE_SEND_HEADER,//リクエスト送信完了  HTTP(SERVER_SIDE)
        SERVER_SIDE_SEND_BODY,//本体送信完了  HTTP(SERVER_SIDE)
        SERVER_SIDE_RECV_HEADER,//ヘッダ受信完了      HTTP(SERVER_SIDE) 
        SERVER_SIDE_RECV_BODY//本体受信完了           HTTP(SERVER_SIDE)
    }

    class OneHttp {

        Response response = new Response();
        Dictionary<CS,HTTP_SIDE_STATE> sideState = new Dictionary<CS,HTTP_SIDE_STATE>(2);

        long lastRecvServer = DateTime.Now.Ticks;

        enum CHARSET {
            UNKNOWN = 0,
            ASCII = 1,
            SJIS = 2,
            EUC = 3,
            UTF8 = 4,
            UTF7 = 5,
            JIS = 6//iso-2022-jp
        }
        bool isText = false;//コンテンツ制限の対象かどうかのフラグ
        CHARSET charset = CHARSET.UNKNOWN;
        byte[] textData = new byte[0];

        public HTTP_SIDE_STATE SideState(CS cs) {
            return sideState[cs];
        }
        
        //リクエストの接続先ホスト名
        //proxyオブジェクが、現在接続中のホストと、このリクエストの接続先が同一かどうかを確認するためのプロパティ
        //proxyオブジェクトは、既存のプロキシ処理をすべて完了していないと接続先を変更できないため、このオブジェクトのサーバへの接続は待機させられることになる
        public string HostName {
            get {
                return oneObj.Request.HostName;
            }
        }

        //キャッシュ対象かどうかのフラグ
        bool IsCacheTarget = false;

        //受信形式
        enum ONE_HTTP_KIND {
            UNKNOWN,//不明
            CONTENT_LENGTH,//Content-Length形式
            CHUNK //chunk形式
        }
        ONE_HTTP_KIND oneHttpKind = ONE_HTTP_KIND.UNKNOWN;
        
        // OneHttpKind == ONE_HTTP_KIND.CONTENT_LENGTHの場合は、下記の変数でサーバからの受信完了を確認する
        int contentLength = -1;
        // OneHttpKind == ONE_HTTP_KIND.CHUNKの場合は、下記の変数でサーバからの受信を行う
        int chunkLen = -1;

        Proxy proxy;
        public Http Http{ get;private set;}
        OneObj oneObj;


        public List<string> Debug(int i) {
            long waitTime = DateTime.Now.Ticks-lastRecvServer;

            List<string> list = new List<string>();
            list.Add(string.Format("[ONE_HTTP][{0}] {1}",i,oneObj.Request.RequestStr));

            if(sideState[CS.CLIENT] == HTTP_SIDE_STATE.CLIENT_SIDE_SEND_BODY) {
                list.Add("[ONE_HTTP] Finish");
            } else {
                if(oneHttpKind == ONE_HTTP_KIND.CHUNK) {
                    list.Add(string.Format("[ONE_HTTP][{0}] code={1} KIND={2} sv={3} cl={4} chunkLen={5} pos sv={6} cl={7}",i,response.Code,oneHttpKind,SideState(CS.SERVER),SideState(CS.CLIENT),chunkLen,oneObj.Pos[CS.SERVER],oneObj.Pos[CS.CLIENT]));
                } else {
                    list.Add(string.Format("[ONE_HTTP][{0}] code={1} KIND={2} sv={3} cl={4} contentLength={5} pos sv={6} cl={7}",i,response.Code,oneHttpKind,SideState(CS.SERVER),SideState(CS.CLIENT),contentLength,oneObj.Pos[CS.SERVER],oneObj.Pos[CS.CLIENT]));
                }
                list.Add(string.Format("[ONE_HTTP][{0}] buf sv={1} cl={2} ■WaitTime={3}",i,oneObj.Buf[CS.SERVER].Length,oneObj.Buf[CS.CLIENT].Length,waitTime));
            }
            return list;
        }
        
        public OneHttp(Proxy proxy,Http http,OneObj oneObj) {
            this.proxy = proxy;
            this.Http = http;
            this.oneObj = oneObj;

            sideState[CS.CLIENT] = HTTP_SIDE_STATE.CLIENT_SIDE_RECV_REQUEST;
            sideState[CS.SERVER] = HTTP_SIDE_STATE.NON;
        }



        //public string RequestStr {
        //    get { return request.RequestStr; }
        //}
        //public PROXY_PROTOCOL Protocol {
        //    get { return request.Protocol; }
        //}

        //キャッシュ確認
        public void CacheConform() {
            //キャッシュ対象のリクエストかどうかの確認
            if(oneObj.Request.Protocol == PROXY_PROTOCOL.HTTP && !oneObj.Request.Cgi) {
                if(Http.Cache.IsTarget(oneObj.Request.HostName,oneObj.Request.Uri,oneObj.Request.Ext)) {
                    // Pragma: no-cache が指定されている場合は、蓄積されたキャッシュを否定する
                    string pragmaStr = oneObj.Header[CS.CLIENT].GetVal("Pragma");
                    if(pragmaStr != null && pragmaStr.ToLower().IndexOf("no-cache") >= 0) {
                        proxy.Logger.Set(LOG_KIND.DETAIL,null,16,oneObj.Request.Uri);
                        Http.Cache.Remove(oneObj.Request.HostName,oneObj.Request.Port,oneObj.Request.Uri);//存在する場合は、無効化する
                    } else {
                        string modifiedStr = oneObj.Header[CS.CLIENT].GetVal("If-Modified-Since");
                        DateTime modified = Util.Str2Time(modifiedStr);
                        byte[] dat = Http.Cache.Get(oneObj.Request,modified);
                        if(dat != null) {//キャッシュが見つかった場合
                            proxy.Logger.Set(LOG_KIND.DETAIL,null,14,oneObj.Request.Uri);

                            sideState[CS.SERVER] = HTTP_SIDE_STATE.SERVER_SIDE_RECV_BODY;//一気に受信完了
                            sideState[CS.CLIENT] = HTTP_SIDE_STATE.CLIENT_SIDE_SEND_HEADER;//ヘッダ送信完了まで進める
                            oneObj.Buf[CS.SERVER] = Bytes.Create("HTTP/1.1 200 OK\r\n",dat);
                        }
                    }
                    string url = string.Format("{0}:{1}{2}",oneObj.Request.HostName,oneObj.Request.Port,oneObj.Request.Uri);
                    //キャッシュ対象の場合だけ、受信用のオブジェクトを生成する
                    IsCacheTarget = true;
                }
            }
        }


        
        public bool SendServer(ref bool life) {
            //サーバ側との接続処理
            if(!proxy.Connect(ref life,oneObj.Request.HostName,oneObj.Request.Port,oneObj.Request.RequestStr,oneObj.Request.Protocol)) {
                proxy.Logger.Set(LOG_KIND.DEBUG,null,999,"□Break http.Connect()==false");
                return false;
            }

            //ヘッダ送信
            byte[] sendBuf = new byte[0];
            if(sideState[CS.SERVER] == HTTP_SIDE_STATE.NON) {
                if(oneObj.Request.Protocol == PROXY_PROTOCOL.SSL) {
                    if(!proxy.UpperProxy.Use) {
                        //取得したリクエストをバッファに格納する
                        //sendBuf = new byte[0];
                        //sendBuf[CS.CLIENT] = Bytes.Create("HTTP/1.0 200 Connection established\r\n\r\n");//CONNECTが成功したことをクライアントに返す
                    } else {
                        //上位プロキシを使用する場合(リクエストラインはそのまま使用される)
                        sendBuf = Bytes.Create(oneObj.Request.SendLine(proxy.UpperProxy.Use),oneObj.Header[CS.CLIENT].GetBytes());
                    }
                } else if(oneObj.Request.Protocol == PROXY_PROTOCOL.HTTP) {//HTTPの場合
                    //header.Remove("Proxy-Connection");//＜＝■これ入れていいのか？

                    //取得したリクエストをバッファに格納する
                    //上位プロキシを使用する場合(リクエストラインはそのまま使用される)
                    sendBuf = Bytes.Create(oneObj.Request.SendLine(proxy.UpperProxy.Use),oneObj.Header[CS.CLIENT].GetBytes());
                }
                if(!Send(proxy.Sock(CS.SERVER),sendBuf))//送信
                    return false;
                sideState[CS.SERVER] = HTTP_SIDE_STATE.SERVER_SIDE_SEND_HEADER;
            }

            if(sideState[CS.SERVER] == HTTP_SIDE_STATE.SERVER_SIDE_SEND_HEADER) {
                //バッファに残っているデータの送信
                if(!SendBuf(CS.CLIENT))
                    return false;
            }

            //サーバへの送信完了を確認する（ステータス変更）
            sideState[CS.SERVER] = HTTP_SIDE_STATE.SERVER_SIDE_SEND_BODY;
            
            return true;
        }

        public bool RecvServer(ref bool life) {
            //int timeout=3;
            //レスポンス・ヘッダの受信
            if(sideState[CS.SERVER] == HTTP_SIDE_STATE.SERVER_SIDE_SEND_BODY) {
                int c = proxy.OptionTimeout; //本当は、OptionTimeout*10　だけど、最初のレスポンスがあまりに遅いとプログラムがロックするので10分の１に設定する
                while(life && proxy.Sock(CS.SERVER).State == SOCKET_OBJ_STATE.CONNECT && proxy.Sock(CS.CLIENT).State == SOCKET_OBJ_STATE.CONNECT && proxy.Sock(CS.SERVER).Length() == 0) {
                    Thread.Sleep(100);
                    c--;
                    if(c < 0)
                        return false;//レスポンスが遅い場合、あまり待ちすぎると処理が止まってしまうので、エラーとする
                }
                //レスポンスの取得
                int len = proxy.Sock(CS.SERVER).Length();
                if(!response.Recv(proxy.Logger,proxy.Sock(CS.SERVER),proxy.OptionTimeout,ref life)) {
                    proxy.Logger.Set(LOG_KIND.ERROR,proxy.Sock(CS.SERVER),6,"");
                    return false;
                }
                //ヘッダの受信
                if(!oneObj.Header[CS.SERVER].Recv(proxy.Sock(CS.SERVER),proxy.OptionTimeout,ref life)) {
                    proxy.Logger.Set(LOG_KIND.ERROR,proxy.Sock(CS.SERVER),7,"");
                    return false;
                }
                
                //データ転送形式の判別
                if(oneObj.Request.Method == HTTP_METHOD.HEAD) {
                        oneHttpKind = ONE_HTTP_KIND.CONTENT_LENGTH;
                        contentLength = 0;
                }
                if(oneHttpKind == ONE_HTTP_KIND.UNKNOWN) {
                    string strTransferEncoding = oneObj.Header[CS.SERVER].GetVal("Transfer-Encoding");
                    if(strTransferEncoding != null) {
                        if(strTransferEncoding == "chunked")
                            oneHttpKind = ONE_HTTP_KIND.CHUNK;
                    }
                }
                if(oneHttpKind == ONE_HTTP_KIND.UNKNOWN) {
                    string strContentLength = oneObj.Header[CS.SERVER].GetVal("Content-Length");
                    if(strContentLength != null) {
                        oneHttpKind = ONE_HTTP_KIND.CONTENT_LENGTH;
                        contentLength = Convert.ToInt32(strContentLength);
                    } else {
                        if(response.Code != 200) {
                            oneHttpKind = ONE_HTTP_KIND.CONTENT_LENGTH;
                            contentLength = 0;
                        }
                    }
                }


                //コンテンツ制限の対象かどうかのフラグを設定する
                if(Http.LimitString != null) {//コンテンツ制限に文字列が設定されている場合
                    string contentType = oneObj.Header[CS.SERVER].GetVal("Content-Type");
                    if(contentType != null) {
                        if(contentType.ToLower().IndexOf("text/h") == 0) {
                            isText = true;
                        }
                        if(contentType.ToLower().IndexOf("text/t") == 0) {
                            isText = true;
                        }
                    }
                    //Content-Encoding:gzipが指定された場合は、テキスト扱いしない
                    if(isText) {
                        string contentEncoding = oneObj.Header[CS.SERVER].GetVal("Content-Encoding");
                        if(contentEncoding != null) {
                            if(contentEncoding.ToUpper().IndexOf("gzip") != -1) {
                                isText = false;
                            }
                        }
                    }

                }

                sideState[CS.SERVER] = HTTP_SIDE_STATE.SERVER_SIDE_RECV_HEADER;//ヘッダ受信完了

                CheckCharset(oneObj.Header[CS.SERVER].GetBytes());//キャラクタセットのチェック

                lastRecvServer = DateTime.Now.Ticks;
            }
            
            //データ本体の受信
            if(oneHttpKind == ONE_HTTP_KIND.CHUNK) {//チャンク形式の場合
                //チャンク形式の受信
                if(!RecvServerChunk(ref life))
                    return false;
            } else {//Content-Length形式の受信
                if(!RecvServerContentLength())
                    return false;
            }

            //受信完了の確認
            if(oneHttpKind == ONE_HTTP_KIND.CONTENT_LENGTH) {
                if(contentLength <= oneObj.Buf[CS.SERVER].Length) {
                    sideState[CS.SERVER] = HTTP_SIDE_STATE.SERVER_SIDE_RECV_BODY;//受信完了
                } else {
                    //データが未到着の場合は、しばらく他のスレッドを優先する
                    //while(life && proxy.Sock(CS.SERVER).Length() == 0)
                    //    Thread.Sleep(100);
                    for(int i = 0;i < 100 && life;i++)
                        Thread.Sleep(10);
                }
            }
            if(proxy.Sock(CS.SERVER).State == SOCKET_OBJ_STATE.DISCONNECT && proxy.Sock(CS.SERVER).Length() == 0) {
                //サーバ側が切断されており、取得できるデータが残っていないときは、常に受信完了とする
                sideState[CS.SERVER] = HTTP_SIDE_STATE.SERVER_SIDE_RECV_BODY;//受信完了
            }
            return true;
        }
        
        //チャンク形式の受信(RecvServer()から使用される)
        bool RecvServerChunk(ref bool life){

            int len = proxy.Sock(CS.SERVER).Length();
            if(len <= 0)
                return true;
            while(life) {
                byte[] b;
                if(chunkLen == -1) {//チャンクサイズの取得
                    //サイズ取得
                    b = proxy.Sock(CS.SERVER).LineRecv(proxy.OptionTimeout,OPERATE_CRLF.NO,ref life);
                    if(b == null || b.Length < 2) {
                        proxy.Logger.Set(LOG_KIND.DEBUG,null,999,string.Format("chunk ERROR b==null or b.Lenght<2"));
                        return false;
                    }
                    oneObj.Buf[CS.SERVER] = Bytes.Create(oneObj.Buf[CS.SERVER],b);
                    lastRecvServer = DateTime.Now.Ticks;

                    //サイズ変換
                    string str = Encoding.ASCII.GetString(b,0,b.Length - 2);
                    if(str == "") {
                        chunkLen = -1;//次回はサイズ取得
                        continue;
                    }

                    try {
                        chunkLen = Convert.ToInt32(str.Trim(),16);
                    } catch {
                        proxy.Logger.Set(LOG_KIND.DEBUG,null,999,string.Format("【例外】Convert.ToInt32(str,16) str=\"{0}\"",str));
                        break;
                    }
                    if(chunkLen == 0) {//受信完了
                        sideState[CS.SERVER] = HTTP_SIDE_STATE.SERVER_SIDE_RECV_BODY;
                        break;
                    }
                }
                len = proxy.Sock(CS.SERVER).Length();
                if(chunkLen > len) {//データ受信が可能かどうかの判断
                    Thread.Sleep(300);//待機
                    break;//受信は、次回に回す
                }
                //データ受信（サイズ分）
                b = proxy.Sock(CS.SERVER).Recv(chunkLen,proxy.OptionTimeout);
                if(b == null || b.Length != chunkLen)
                    return false;

                oneObj.Buf[CS.SERVER] = Bytes.Create(oneObj.Buf[CS.SERVER],b);

                if(isText) {
                    textData = Bytes.Create(textData,b);
                    //コンテンツ制限の確認
                    if(IsHitLimitString(textData))
                        return false;
                }

                lastRecvServer = DateTime.Now.Ticks;
                chunkLen = -1;//次回はサイズ取得
            }
            return true;
        }

        //Content-Length形式の受信(RecvServer()から使用される)
        bool RecvServerContentLength() {
            int len = proxy.Sock(CS.SERVER).Length();
            if(len <= 0) 
                return true;
            byte[] b = proxy.Sock(CS.SERVER).Recv(len,proxy.OptionTimeout);
            if(b == null)
                return false;


            oneObj.Buf[CS.SERVER] = Bytes.Create(oneObj.Buf[CS.SERVER],b);

            if(isText) {
                //コンテンツ制限の確認
                if(IsHitLimitString(oneObj.Buf[CS.SERVER]))
                    return false;
            }

            lastRecvServer = DateTime.Now.Ticks;
            return true;
        }

        public bool SendClient(ref bool life) {
            if(sideState[CS.SERVER] != HTTP_SIDE_STATE.SERVER_SIDE_RECV_HEADER && sideState[CS.SERVER] != HTTP_SIDE_STATE.SERVER_SIDE_RECV_BODY) 
                return true;

            byte[] sendBuf = new byte[0];
            if(sideState[CS.CLIENT] == HTTP_SIDE_STATE.CLIENT_SIDE_RECV_REQUEST) {
                //サーバから受信したレスポンス及びヘッダをバッファに展開する
                sendBuf = Bytes.Create(response.ToString(),"\r\n",oneObj.Header[CS.SERVER].GetBytes());
                if(!Send(proxy.Sock(CS.CLIENT),sendBuf))//送信
                    return false;
                sideState[CS.CLIENT] = HTTP_SIDE_STATE.CLIENT_SIDE_SEND_HEADER;//ヘッダ送信完了
            }
            if(sideState[CS.CLIENT] == HTTP_SIDE_STATE.CLIENT_SIDE_SEND_HEADER) {
                //バッファに残っているデータの送信
                if(!SendBuf(CS.SERVER))
                    return false;
            }
            
            //クライアントへの送信完了を確認する（ステータス変更）
            if(oneHttpKind == ONE_HTTP_KIND.CONTENT_LENGTH) {//Content-Lengthの場合
                //posがContentLengthまで達したら完了
                if(oneObj.Pos[CS.SERVER] >= contentLength)
                    sideState[CS.CLIENT] = HTTP_SIDE_STATE.CLIENT_SIDE_SEND_BODY;
            } else if(oneHttpKind == ONE_HTTP_KIND.CHUNK) {//chunkの場合
                //サーバ側の受信が完了し、バッファをすべて送信したら完了
                if(sideState[CS.SERVER] == HTTP_SIDE_STATE.SERVER_SIDE_RECV_BODY && oneObj.Buf[CS.SERVER].Length <= oneObj.Pos[CS.SERVER])
                    sideState[CS.CLIENT] = HTTP_SIDE_STATE.CLIENT_SIDE_SEND_BODY;
            }
            //上記の条件で送信完了していない場合でも
            if(sideState[CS.CLIENT] != HTTP_SIDE_STATE.CLIENT_SIDE_SEND_BODY) {
                //サーバ側が切断され、受信未処理のデータが存在せず、バッファをすべて送信していたら完了
                if(proxy.Sock(CS.SERVER).State == SOCKET_OBJ_STATE.DISCONNECT && proxy.Sock(CS.SERVER).Length() == 0 && oneObj.Buf[CS.SERVER].Length <= oneObj.Pos[CS.SERVER]) {
                    sideState[CS.CLIENT] = HTTP_SIDE_STATE.CLIENT_SIDE_SEND_BODY;
                }
            }

            return true;
        }
        
        //バッファに残っているデータの送信
        //パラメータ cs CS.SERVER を設定した場合、buf[CS.SERVER]を処理対象とし、クライアント側に送信する
        bool SendBuf(CS cs) {
            TcpObj sock = proxy.Sock(CS.CLIENT);
            if(cs == CS.CLIENT)
                sock = proxy.Sock(CS.SERVER);

            int len = oneObj.Buf[cs].Length - oneObj.Pos[cs];
            if(len > 0) {
                byte[] sendBuf = new byte[len];
                Buffer.BlockCopy(oneObj.Buf[cs],oneObj.Pos[cs],sendBuf,0,len);
                if(!Send(sock,sendBuf))//送信
                    return false;
                oneObj.Pos[cs] += len;
            }
            return true;
        }

        //送信
        bool Send(TcpObj sock,byte[] sendBuf) {
            int c = sock.SendUseEncode(sendBuf);
            if(c == sendBuf.Length) {
                sendBuf = new byte[0];
            } else {
                return false;
            }
            return true;
        }

        //ヘッダ追加
        //public void AddHeader(CS cs,string key,string val) {
        //    oneObj.Header[cs].Append(key,val);
        //}

        //コンテンツ制限の確認
        bool IsHitLimitString(byte [] b) {
            if(isText) {
                CheckCharset(b);//キャラクタセットのチェック
                
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
                string hitStr = Http.LimitString.IsHit(str);
                if(hitStr != null) {
                    //制限にヒットした場合
                    proxy.Logger.Set(LOG_KIND.NOMAL,proxy.Sock(CS.SERVER),21,hitStr);
                    return true;
                }
            }
            return false;
        }
        //キャラクタセットのチェック
        void CheckCharset(byte[] b) {
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
        }
    }
}
