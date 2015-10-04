using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Globalization;

using Bjd;
using Bjd.acl;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using Bjd.util;

namespace WebServer {
    partial class Server : OneServer {
        readonly AttackDb _attackDb;//自動拒否


        //通常は各ポートごと１種類のサーバが起動するのでServerTread.option を使用するが、
        //バーチェルホストの場合、１つのポートで複数のサーバが起動するのでオプションリスト（webOptionList）
        //から適切なものを選択し、opBaseにコピーして使用する
        //_subThreadが呼び出されるまでは、ポート番号の代表である ServerThread.option （webOptionList[0]と同じ ）が使用されている
        //Ver5.1.4
        readonly List<WebDavDb> _webDavDbList = new List<WebDavDb>();
        WebDavDb _webDavDb;//WevDAVのDethプロパテイを管理するクラス
        
        protected List<OneOption> WebOptionList = null;

        //通常のServerThreadの子クラスと違い、オプションはリストで受け取る
        //親クラスは、そのリストの0番目のオブジェクトで初期化する

        //コンストラクタ
        public Server(Kernel kernel,Conf conf,OneBind oneBind)
            : base(kernel, conf,oneBind) {

            //同一ポートで待ち受けている仮想サーバのオプションをすべてリストする
            WebOptionList = new List<OneOption>();
            foreach (var o in kernel.ListOption) {
                if (o.NameTag.IndexOf("Web-") == 0) {
                    if((int)o.GetValue("port") == (int)Conf.Get("port")){
                        WebOptionList.Add(o);
                    }
                }
            }
            //WebDAVリストの初期化
            foreach (var o in WebOptionList) {
                if(o.UseServer) {
                    _webDavDbList.Add(new WebDavDb(kernel,NameTag));
                }
            }
            _webDavDb = _webDavDbList[0];
            
            //Ver5.1.2「Cgiパス」「WebDAVパス」「別名」のオプションの修正
            var tagList = new List<string> { "cgiPath", "webDavPath", "aliaseList" };
            foreach(string tag in tagList) {
                var dat = (Dat)Conf.Get(tag);
                var changed = false;
                foreach(var o in dat) {
                    var str = o.StrList[0];
                    if(str[0] != '/') {
                        changed = true;
                        str = '/' + str;
                    }
                    if(str.Length > 1 && str[str.Length - 1] != '/') {
                        changed = true;
                        str = str + '/';
                    }
                    o.StrList[0] = str;
                }
                if(changed)
                    Conf.Set(tag, dat);
            }


            //当初、opBase及びloggerは、weboptionList[0]で暫定的に初期化される 
            var protocol = (int)Conf.Get("protocol");
            if (protocol==1) {//HTTPS
                var op = kernel.ListOption.Get("VirtualHost");
                var privateKeyPassword = (string)op.GetValue("privateKeyPassword");
                var certificate = (string)op.GetValue("certificate");

                //サーバ用SSLの初期化
                ssl = new Ssl(Logger, certificate, privateKeyPassword);
            }

            var useAutoAcl = (bool)Conf.Get("useAutoAcl");// ACL拒否リストへ自動追加する
            if (useAutoAcl) {
                const int max = 1; //発生回数
                const int sec = 120; // 対象期間(秒)
                _attackDb = new AttackDb(sec, max);
            }

        }
        //終了処理
        new public void Dispose() {
            foreach(var db in _webDavDbList) {
                db.Dispose();
            }
            base.Dispose();
        }
        //スレッド開始処理
        override protected bool OnStartServer() {
            return true;
        }
        //スレッド停止処理
        override protected void OnStopServer() {

        }


        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var sockTcp = (SockTcp)sockObj;

            var remoteIp = sockTcp.RemoteIp;

            //opBase 及び loggerはバーチャルホストで変更されるので、
            //このポインタを初期化に使用できない

            bool keepAlive = true;//レスポンスが終了したとき接続を切断しないで継続する 

            //1回目の通信でバーチャルホストの検索を実施する
            var checkVirtual = true;

            var request = new Request(Logger,sockTcp);//リクエストライン処理クラス
            
            //受信ヘッダ
            var recvHeader = new Header();

            //Ver5.1.x
            string urlStr = null;//http://example.com

            //接続が継続している間は、このループの中にいる(継続か否かをkeepAliveで保持する)
            //「continue」は、次のリクエストを待つ　「break」は、接続を切断する事を意味する

            WebStream inputStream = null;
            var outputStream = new WebStream(-1);




            while (keepAlive && IsLife()) {
                int responseCode;

                //***************************************************************
                // ドキュメント生成クラスの初期化
                //***************************************************************
                var contentType = new ContentType(Conf);
                var document = new Document(Kernel, Logger, Conf, sockTcp, contentType);

                var authrization = new Authorization(Conf, Logger);
                var authName = "";
                

                //***************************************************************
                //データ取得
                //***************************************************************
                //リクエスト取得
                //ここのタイムアウト値は、大きすぎるとブラウザの切断を取得できないでブロックしてしまう
                var requestStr = sockTcp.AsciiRecv(Timeout, this);
                if (requestStr == null)
                    break;
                //\r\nの削除
                requestStr = Inet.TrimCrlf(requestStr);
                //Ver5.8.8 リクエストの解釈に失敗した場合に、処理を中断する
                //request.Init(requestStr);
                if (!request.Init(requestStr)){
                    break;
                }

                //ヘッダ取得（内部データは初期化される）
                if (!recvHeader.Recv(sockTcp,(int)Conf.Get("timeOut"),this))
　                   break;

                {
                    //Ver5.1.x
                    var hostStr = recvHeader.GetVal("host");
                    urlStr = hostStr==null ? null : string.Format("{0}://{1}",(ssl != null)?"https":"http",hostStr);
                }

                //入力取得（POST及びPUTの場合）
                var contentLengthStr = recvHeader.GetVal("Content-Length");
                if(contentLengthStr != null) {
                    try{
                        //max,lenはともにlong
                        var max = Convert.ToInt64(contentLengthStr);
                        if(max!=0){//送信データあり
                            inputStream = new WebStream((256000<max)?-1:(int)max);
                            var errorCount = 0;
                            while(inputStream.Length<max && IsLife()){

                                var len = max - inputStream.Length;
                                if (len > 51200000) {
                                    len = 51200000;
                                }
                                var b = sockTcp.Recv((int)len, (int)Conf.Get("timeOut"),this);
                                if (!inputStream.Add(b)) {
                                    errorCount++;//エラー蓄積
                                    Logger.Set(LogKind.Error, null, 41, string.Format("content-Length={0} Recv={1}", max, inputStream.Length));
                                } else {
                                    errorCount = 0;//初期化
                                }
                                Logger.Set(LogKind.Detail, null,38, string.Format("Content-Length={0} {1}bytes Received.", max, inputStream.Length));
                                if (errorCount > 5){//５回連続して受信が無かった場合、サーバエラー
                                    responseCode = 500;
                                    goto SEND;//サーバエラー
                                }
                                Thread.Sleep(10);
                            }
                            Logger.Set(LogKind.Detail, null, 39, string.Format("Content-Length={0} {1}bytes", max, inputStream.Length));
                        }
                    }catch(Exception ex){
                        Logger.Set(LogKind.Error, null, 40, ex.Message);
                    }
                }

                // /によるパラメータ渡しに対応
                //for (int i = 0;i < Option->CgiExt->Count;i++) {
                //    wsprintf(TmpBuf,".%s/",Option->CgiExt->Strings[i]);
                //    strupr(TmpBuf);
                //    strcpy(Buf,Headers->Uri);
                //    strupr(Buf);
                //    if (NULL != (p = strstr(Buf,TmpBuf))) {
                //        i = p - Buf;
                //        i += strlen(TmpBuf) - 1;
                //        p = &Headers->Uri[i];
                //        *p = '\0';
                //        p = &Headers->UriNoConversion[i];
                //        *p = '\0';
                //        wsprintf(TmpBuf,"/%s",p + 1);
                //        Headers->PathInfo = new char[strlen(TmpBuf) + 1];
                //        strcpy(Headers->PathInfo,TmpBuf);
                //        break;
                //    }
                //}



                //***************************************************************
                //バーチャルホストの検索を実施し、opBase、logger及び webDavDb を置き換える
                //***************************************************************
                if (checkVirtual) {//初回のみ
                    ReplaceVirtualHost(recvHeader.GetVal("host"),sockTcp.LocalAddress.Address,sockTcp.LocalAddress.Port);
                    checkVirtual = false;
                }
                //***************************************************************
                //接続を継続するかどうかの判断 keepAliveの初期化
                //***************************************************************
                if (ssl != null) {
                    keepAlive = false;//SSL通信では、１回づつコネクションが必要
                }else{
                    if (request.Ver == "HTTP/1.1") {//HTTP1.1はデフォルトで keepAlive=true
                        keepAlive = true;
                    } else { // HTTP/1.1以外の場合、継続接続は、Connection: Keep-Aliveの有無に従う
                        keepAlive = recvHeader.GetVal("Connection") == "Keep-Alive";
                    }
                }

                //***************************************************************
                // ドキュメント生成クラスの初期化
                //***************************************************************
                //var contentType = new ContentType(OneOption);
                //var document = new Document(kernel,Logger,OneOption,sockTcp,contentType);


                //***************************************************************
                // ログ
                //***************************************************************
                Logger.Set(LogKind.Normal, sockTcp, ssl != null ? 23 : 24, request.LogStr);

                //***************************************************************
                // 認証
                //***************************************************************
                //var authrization = new Authorization(OneOption,Logger);
                //string authName = "";
                if (!authrization.Check(request.Uri, recvHeader.GetVal("authorization"), ref authName)) {
                    responseCode = 401;
                    keepAlive = false;//切断
                    goto SEND;
                }
                //***************************************************************
                // 不正なURIに対するエラー処理
                //***************************************************************
                //URIを点検して不正な場合はエラーコードを返す
                responseCode = CheckUri(sockTcp, request, recvHeader);
                if (responseCode != 200) {
                    keepAlive = false;//切断
                    goto SEND;
                }
                
                //***************************************************************
                //ターゲットオブジェクトの初期化
                //***************************************************************
                var target = new Target(Conf,Logger);
                if (target.DocumentRoot == null) {
                    Logger.Set(LogKind.Error,sockTcp,14,string.Format("documentRoot={0}",Conf.Get("documentRoot")));//ドキュメントルートで指定されたフォルダが存在しません（処理を継続できません）
                    break;//ドキュメントルートが無効な場合は、処理を継続できない
                }
                target.InitFromUri(request.Uri);

                //***************************************************************
                // 送信ヘッダの追加
                //***************************************************************
                // 特別拡張 BlackJumboDog経由のリクエストの場合 送信ヘッダにRemoteHostを追加する
                if ((bool)Conf.Get("useExpansion")) {
                    if (recvHeader.GetVal("Host") != null) {
                        document.AddHeader("RemoteHost",sockTcp.RemoteAddress.Address.ToString());
                    }
                }
                //受信ヘッダに「PathInfo:」が設定されている場合、送信ヘッダに「PathTranslated」を追加する
                var pathInfo = recvHeader.GetVal("PathInfo");
                if (pathInfo != null) {
                    pathInfo = target.DocumentRoot + pathInfo;
                    document.AddHeader("PathTranslated",Util.SwapChar('/','\\',pathInfo));
                }
                //***************************************************************
                //メソッドに応じた処理 OPTIONS 対応 Ver5.1.x
                //***************************************************************
                if(WebDav.IsTarget(request.Method)){
                    var webDav = new WebDav(Logger, _webDavDb, target, document, urlStr, recvHeader.GetVal("Depth"), contentType,(bool)Conf.Get("useEtag"));

                    var inputBuf = new byte[0];
                    if(inputStream!=null){
                        inputBuf = inputStream.GetBytes();
                    }

                    switch(request.Method) {
                        case HttpMethod.Options:
                            responseCode = webDav.Option();
                            break;
                        case HttpMethod.Delete:
                            responseCode = webDav.Delete();
                            break;
                        case HttpMethod.Put:
                            responseCode = webDav.Put(inputBuf);
                            break;
                        case HttpMethod.Proppatch:
                            responseCode = webDav.PropPatch(inputBuf);
                            break;
                        case HttpMethod.Propfind:
                            responseCode = webDav.PropFind();
                            break;
                        case HttpMethod.Mkcol:
                            responseCode = webDav.MkCol();
                            break;
                        case HttpMethod.Copy:
                        case HttpMethod.Move:
                            responseCode = 405;
                            //Destnationで指定されたファイルは書き込み許可されているか？
                            var dstTarget = new Target(Conf,Logger);
                            string destinationStr = recvHeader.GetVal("Destination");
                            if(destinationStr != null) {
                                if(destinationStr.IndexOf("://") == -1) {
                                    destinationStr = urlStr + destinationStr;
                                }
                                var uri = new Uri(destinationStr);
                                dstTarget.InitFromUri(uri.LocalPath);


                                if(dstTarget.WebDavKind == WebDavKind.Write) {
                                    var overwrite = false;
                                    var overwriteStr = recvHeader.GetVal("Overwrite");
                                    if(overwriteStr != null) {
                                        if(overwriteStr == "F") {
                                            overwrite = true;
                                        }
                                    }
                                    responseCode = webDav.MoveCopy(dstTarget,overwrite,request.Method);
                                    document.AddHeader("Location",destinationStr);
                                }
                            }
                            break;
                    }
                    //WebDAVに対するリクエストは、ここで処理完了
                    goto SEND;
                }
                //以下 label SENDまでの間は、GET/POSTに関する処理

                //***************************************************************
                //ターゲットの種類に応じた処理
                //***************************************************************

                if (target.TargetKind == TargetKind.Non) { //見つからない場合
                    responseCode = 404;
                    goto SEND;
                }
                if (target.TargetKind == TargetKind.Move) { //ターゲットはディレクトリの場合
                    responseCode = 301;
                    goto SEND;
                }
                if (target.TargetKind == TargetKind.Dir) { //ディレクトリ一覧表示の場合
                    //インデックスドキュメントを生成する
                    if (!document.CreateFromIndex(request, target.FullPath)) 
                        break;
                    goto SEND;
                }

                //***************************************************************
                //  隠し属性のファイルへのアクセス制御
                //***************************************************************
                if (!(bool)Conf.Get("useHidden")) {
                    if ((target.Attr & FileAttributes.Hidden) == FileAttributes.Hidden) {
                        //エラーキュメントを生成する
                        responseCode = 404;
                        keepAlive = false;//切断
                        goto SEND;
                    }
                }

                if (target.TargetKind == TargetKind.Cgi || target.TargetKind == TargetKind.Ssi) {
                    keepAlive = false;//デフォルトで切断
                    
                    //環境変数作成
                    var env = new Env(Kernel,Conf,request, recvHeader,sockTcp, target.FullPath);
                    
                    // 詳細ログ
                    Logger.Set(LogKind.Detail,sockTcp,18,string.Format("{0} {1}",target.CgiCmd,Path.GetFileName(target.FullPath)));

                    if (target.TargetKind == TargetKind.Cgi) {

                        var cgi = new Cgi();
                        var cgiTimeout = (int)Conf.Get("cgiTimeout");
                        if (!cgi.Exec(target,request.Param,env,inputStream,out outputStream,cgiTimeout)) {
                            // エラー出力
                            var errStr = Encoding.ASCII.GetString(outputStream.GetBytes()); 

                            Logger.Set(LogKind.Error,sockTcp,16,errStr);
                            responseCode = 500;
                            goto SEND;
                        }

                        //***************************************************
                        // NPH (Non-Parsed Header CGI)スクリプト  nph-で始まる場合、サーバ処理（レスポンスコードやヘッダの追加）を経由しない
                        //***************************************************
                        if (Path.GetFileName(target.FullPath).IndexOf("nph-") == 0) {
                            sockTcp.SendUseEncode(outputStream.GetBytes());//CGI出力をそのまま送信する
                            break;
                        }
                        // CGIで得られた出力から、本体とヘッダを分離する
                        if(!document.CreateFromCgi(outputStream.GetBytes()))
                            break;
                        // cgi出力で、Location:が含まれる場合、レスポンスコードを302にする
                        if (document.SearchLocation())//Location:ヘッダを含むかどうか
                            responseCode = 302;
                        goto SEND;
                    } 
                    //SSI
                    var ssi = new Ssi(Kernel, Logger,Conf, sockTcp, request, recvHeader);
                    if (!ssi.Exec(target,env,outputStream)) {
                        // エラー出力
                        Logger.Set(LogKind.Error,sockTcp,22,MLang.GetString(outputStream.GetBytes()));
                        responseCode = 500;
                        goto SEND;
                    }
                    document.CreateFromSsi(outputStream.GetBytes(),target.FullPath);
                    goto SEND;
                }

                //以下は、通常ファイルの処理 TARGET_KIND.FILE

                //********************************************************************
                //Modified処理
                //********************************************************************
                if (recvHeader.GetVal("If_Modified_Since") != null) {
                    var dt = Util.Str2Time(recvHeader.GetVal("If-Modified-Since"));
                    if (target.FileInfo.LastWriteTimeUtc.Ticks / 10000000 <= dt.Ticks / 10000000) {
                        
                        responseCode = 304;
                        goto SEND;
                    }
                }
                if (recvHeader.GetVal("If_Unmodified_Since") != null) {
                    var dt = Util.Str2Time(recvHeader.GetVal("If_Unmodified_Since"));
                    if (target.FileInfo.LastWriteTimeUtc.Ticks / 10000000 > dt.Ticks / 10000000) {
                        responseCode = 412;
                        goto SEND;
                    }
                }
                document.AddHeader("Last-Modified",Util.UtcTime2Str(target.FileInfo.LastWriteTimeUtc));
                //********************************************************************
                //ETag処理
                //********************************************************************
                // (1) useEtagがtrueの場合は、送信時にETagを付加する
                // (2) If-None-Match 若しくはIf-Matchヘッダが指定されている場合は、排除対象かどうかの判断が必要になる
                if ((bool)Conf.Get("useEtag") || recvHeader.GetVal("If-Match") != null || recvHeader.GetVal("If-None-Match") != null) {
                    //Ver5.1.5
                    //string etagStr = string.Format("\"{0:x}-{1:x}\"", target.FileInfo.Length, (target.FileInfo.LastWriteTimeUtc.Ticks / 10000000));
                    var etagStr = WebServerUtil.Etag(target.FileInfo);
                    string str;
                    if (null != (str = recvHeader.GetVal("If-Match"))) {
                        if (str != "*" && str != etagStr) {
                            responseCode = 412;
                            goto SEND;
                        }

                    }
                    if (null != (str = recvHeader.GetVal("If-None-Match"))) {
                        if (str != "*" && str == etagStr) {
                            responseCode = 304;
                            goto SEND;
                        }
                    }
                    if ((bool)Conf.Get("useEtag"))
                        document.AddHeader("ETag",etagStr);
                }
                //********************************************************************
                //Range処理
                //********************************************************************
                document.AddHeader("Accept-Range","bytes");
                var rangeFrom = 0L;//デフォルトは最初から
                var rangeTo = target.FileInfo.Length;//デフォルトは最後まで（ファイルサイズ）
                if (recvHeader.GetVal("Range") != null) {//レンジ指定のあるリクエストの場合
                    var range = recvHeader.GetVal("Range");
                    //指定範囲を取得する（マルチ指定には未対応）
                    if (range.IndexOf("bytes=") == 0) {
                        range = range.Substring(6);
                        var tmp = range.Split('-');


                        //Ver5.3.5 ApacheKiller対処
                        if (tmp.Length > 20) {
                            Logger.Set(LogKind.Secure, sockTcp,9000054, string.Format("[ Apache Killer ]Range:{0}", range));

                            AutoDeny(false, remoteIp);
                            responseCode = 503;
                            keepAlive = false;//切断
                            goto SEND;
                        }
                        
                        if(tmp.Length == 2) {

                            //Ver5.3.6 のデバッグ用
                            //tmp[1] = "499";

                            if(tmp[0] != "") {
                                if(tmp[1] != "") {// bytes=0-10 0～10の11バイト
                                    
                                    //Ver5.5.9
                                    rangeFrom = Convert.ToInt64(tmp[0]);
                                    if (tmp[1] != "") {
                                        //Ver5.5.9
                                        rangeTo = Convert.ToInt64(tmp[1]);
                                        if (target.FileInfo.Length <= rangeTo) {
                                            rangeTo = target.FileInfo.Length - 1;
                                        } else {
                                            document.SetRangeTo = true;//Ver5.4.0
                                        }
                                    }
                                } else {// bytes=3- 3～最後まで
                                    rangeTo = target.FileInfo.Length - 1;
                                    rangeFrom = Convert.ToInt64(tmp[0]); 
                                }
                            } else {
                                if(tmp[1] != "") {// bytes=-3 最後から3バイト
                                    var len = Convert.ToInt64(tmp[1]);
                                    rangeTo = target.FileInfo.Length - 1;
                                    rangeFrom = rangeTo-len+1;
                                    if(rangeFrom<0)
                                        rangeFrom=0;
                                    document.SetRangeTo = true;//Ver5.4.0
                                }

                            }
                            if(rangeFrom <= rangeTo) {
                                //正常に範囲を取得できた場合、事後Rangeモードで動作する
                                document.AddHeader("Content-Range",string.Format("bytes {0}-{1}/{2}",rangeFrom,rangeTo,target.FileInfo.Length));
                                responseCode = 206;
                            }
                        }
                    }
                }
                //通常ファイルのドキュメント
                if (request.Method != HttpMethod.Head) {
                    if (!document.CreateFromFile(target.FullPath,rangeFrom,rangeTo))
                        break;
                }

            SEND:
                //レスポンスコードが200以外の場合は、ドキュメント（及び送信ヘッダ）をエラー用に変更する
                if(responseCode != 200 && responseCode != 302 && responseCode != 206 && responseCode != 207 && responseCode != 204 && responseCode != 201) {

                    //ResponceCodeの応じてエラードキュメントを生成する
                    if (!document.CreateFromErrorCode(request,responseCode))
                        break;

                    if (responseCode == 301) {//ターゲットがファイルではなくディレクトの間違いの場合
                        if(urlStr != null) {
                            var str = string.Format("{0}{1}/",urlStr,request.Uri);
                            document.AddHeader("Location",Encoding.UTF8.GetBytes(str));
                        }
                    }

                    if (responseCode == 304 || responseCode == 301) {//304 or 301 の場合は、ヘッダのみになる
                        document.Clear();
                    } else {
                        if (responseCode == 401) {
                            document.AddHeader("WWW-Authenticate", string.Format("Basic realm=\"{0}\"", authName));
                        }
                    }
                }
                
                //Ver5.6.2 request.Send()廃止
                var responseStr = request.CreateResponse(responseCode);
                sockTcp.AsciiSend(responseStr);//レスポンス送信
                Logger.Set(LogKind.Detail, sockTcp, 4, responseStr);//ログ

                
                document.Send(keepAlive,this);//ドキュメント本体送信
            }

            if(inputStream!=null)
                inputStream.Dispose();
            if (outputStream != null)
                outputStream.Dispose();

            //end://このソケット接続の終了
            if (sockTcp != null) {
                sockTcp.Close();
            }
        }
        
        //********************************************************
        // Host:ヘッダを見て、バーチャルホストの設定にヒットした場合は
        // オプション等を置き換える
        //********************************************************
        void ReplaceVirtualHost(string host, IPAddress ip,int port) {
            
            //Ver5.0.0-b12
            if(host == null) {
                return;
            }

            //Ver5.0.0-a6 仮想Webの検索をホスト名（アドレス）＋ポート番号に修正
            for (int n = 0; n < 2; n++) {
                if (n == 0) {//１回目はホスト名で検索する
                    //Ver5.0.0-a6 「ホスト名:ポート番号」の形式で検索する
                    if (host.IndexOf(':') < 0) {
                        host = string.Format("{0}:{1}",host,port);
                    }
                    host = host.ToUpper();//ホスト名は、大文字・小文字を区別しない
                } else {//２回目はアドレスで検索する
                    host = string.Format("{0}:{1}",ip,port);
                }

                //バーチャルホスト指定の場合オプションを変更する
                foreach (var op in WebOptionList) {
                    //先頭のWeb-を削除する
                    string name = op.NameTag.Substring(4).ToUpper();
                    if (name == host) {
                        if (op.NameTag != Conf.NameTag) {
                            //Ver5.1.4 webDavDbを置き換える
                            foreach(var db in _webDavDbList) {
                                if(db.NameTag == op.NameTag) {
                                    _webDavDb = db;
                                }
                            }
                            //オプション及びロガーを再初期化する
                            //OneOption = op;
                            Conf = new Conf(op);
                            Logger = Kernel.CreateLogger(op.NameTag, (bool)Conf.Get("useDetailsLog"), this);
                        }
                        return;
                    }
                }
            }
        }


        //********************************************************
        //URIを点検して不正な場合はエラーコードを返す
        //return 200 エラーなし
        //********************************************************
        int CheckUri(SockTcp sockTcp, Request request, Header recvHeader) {
            var responseCode = 200;

            // v2.3.1 Uri の１文字目が/で無い場合
            if (request.Uri[0] != '/') {
                responseCode = 400;

                //Uriの最後に空白が入っている場合
            } else if (request.Uri[request.Uri.Length - 1] == (' ') || request.Uri[request.Uri.Length - 1] == ('.')) {
                responseCode = 404;

                // ./の含まれるリクエストは404で落とす
                // %20/の含まれるリクエストは404で落とす
            } else if ((0 <= request.Uri.IndexOf("./")) || (0 <= request.Uri.IndexOf(" /"))) {
                responseCode = 404;

                // HTTP1.1でhostヘッダのないものはエラー
            } else if (request.Ver == "HTTP/1.1" && recvHeader.GetVal("Host") == null) {
                responseCode = 400;

                // ..を参照するパスの排除
            } else if (!(bool)Conf.Get("useDot") && 0 <= request.Uri.IndexOf("..")) {
                Logger.Set(LogKind.Secure,sockTcp,13,"URI=" + request.Uri);//.. が含まれるリクエストは許可されていません。
                responseCode = 403;
            }
            return responseCode;
        }




        //bool CheckAuthList(string requestUri) {
        //    // 【注意 ショートファイル名でアクセスした場合の、認証の回避を考慮する必要がある】
        //    //AnsiString S = ExtractShortPathName(ShortNamePath);
        //    var authList = (Dat)this.Conf.Get("authList");
        //    foreach (var o in authList) {
        //        if (!o.Enable)
        //            continue;
        //        string uri = o.StrList[0];
                
        //        if (requestUri.IndexOf(uri) == 0) {
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        void AutoDeny(bool success, Ip remoteIp) {
            if (_attackDb == null)
                return;
            //データベースへの登録
            if (!_attackDb.IsInjustice(success, remoteIp))
                return;

            //ブルートフォースアタック
            if (AclList.Append(remoteIp)) {//ACL自動拒否設定(「許可する」に設定されている場合、機能しない)
                //追加に成功した場合、オプションを書き換える
                var d = (Dat)Conf.Get("acl");
                var name = string.Format("AutoDeny-{0}", DateTime.Now);
                var ipStr = remoteIp.ToString();
                d.Add(true, string.Format("{0}\t{1}", name, ipStr));
                Conf.Set("acl", d);
                Conf.Save(Kernel.IniDb);

                Logger.Set(LogKind.Secure, null, 9000055, string.Format("{0},{1}", name, ipStr));
            } else {
                Logger.Set(LogKind.Secure, null, 9000056, remoteIp.ToString());
            }
        }

        //テスト用
        public String DocumentRoot{
            get{
                return (string) Conf.Get("documentRoot");
            }
        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }


    }
}
