using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Bjd;
using System.Text.RegularExpressions;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;

namespace ProxyHttpServer {

    public partial class Server:OneServer {
        //enum Charset {
        //    Unknown=0,
        //    Ascii=1,
        //    Sjis=2,
        //    Euc=3,
        //    Utf8=4,
        //    Utf7=5,
        //    Jis=6//iso-2022-jp
        //}

        private const int DataPortMin = 20000;
        private const int DataPortMax = 21000;
        int _dataPort;

        Cache _cache;
        // 上位プロキシを経由しないサーバのリスト
        readonly List<string> _disableAddressList = new List<string>();

        readonly LimitUrl _limitUrl;//URL制限
        readonly LimitString _limitString;//コンテンツ制限

        //リクエストを通常ログで表示する
        readonly bool _useRequestLog;

        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel, conf,oneBind) {

            _cache = new Cache(kernel,this.Logger,conf);

            // 上位プロキシを経由しないサーバのリスト
            foreach (var o in (Dat)Conf.Get("disableAddress")) {
                if (o.Enable) {//有効なデータだけを対象にする
                    _disableAddressList.Add(o.StrList[0]);
                }
            }
            //URL制限
            var allow = (Dat)Conf.Get("limitUrlAllow");
            var deny = (Dat)Conf.Get("limitUrlDeny");
            //Ver5.4.5正規表現の誤りをチェックする
            for (var i = 0; i < 2; i++) {
                foreach (var a in (i == 0) ? allow : deny) {
                    if (a.Enable && a.StrList[1] == "3") {//正規表現
                        try {
                            var regex = new Regex(a.StrList[0]);
                        } catch {
                            Logger.Set(LogKind.Error, null, 28, a.StrList[0]);
                        }
                    }
                }
            }
            _limitUrl = new LimitUrl(allow,deny);

            
            //リクエストを通常ログで表示する
            _useRequestLog = (bool)Conf.Get("useRequestLog");

            //コンテンツ制限
            _limitString = new LimitString((Dat)Conf.Get("limitString"));
            if (_limitString.Length == 0)
                _limitString = null;

            _dataPort = DataPortMin;

        }
        //リモート操作（データの取得）
        override public string Cmd(string cmdStr) {

            if (cmdStr == "Refresh-DiskCache" || cmdStr == "Refresh-MemoryCache") {
                var infoList = new List<CacheInfo>();
                _cache.GetInfo((cmdStr == "Refresh-MemoryCache")?CacheKind.Memory : CacheKind.Disk,ref infoList);
                var sb = new StringBuilder();
                foreach(CacheInfo cacheInfo in infoList){
                    sb.Append(cacheInfo+"\b");
                }
                return sb.ToString();
            }
            if (cmdStr.IndexOf("Cmd-Remove")==0) {

                var tmp = cmdStr.Split('\t');

                if (tmp.Length != 5)
                    return "false";
                var kind = (CacheKind)Enum.Parse(typeof(CacheKind), tmp[1]);
                var hostName = tmp[2];
                var port = Convert.ToInt32(tmp[3]);
                var uri = tmp[4];
                if (_cache.Remove(kind, hostName, port, uri))
                    return "true";
                return "false";
            }
            return "";
        }

        new public void Dispose() {
            if (_cache != null)
                _cache.Dispose();
            _cache = null;
            
            base.Dispose();
        }
        override protected bool OnStartServer() {
            if (_cache != null)
                _cache.Start();
            return true;
        }
        override protected void OnStopServer() {
            if (_cache != null){
                _cache.Stop();
                _cache.Dispose();
            }
        }
        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {

            //Ver5.6.9
            //UpperProxy upperProxy = new UpperProxy((bool)Conf.Get("useUpperProxy"),(string)this.Conf.Get("upperProxyServer"),(int)this.Conf.Get("upperProxyPort"),disableAddressList);
            var upperProxy = new UpperProxy((bool)Conf.Get("useUpperProxy"), (string)Conf.Get("upperProxyServer"), (int)Conf.Get("upperProxyPort"), _disableAddressList,
                (bool)Conf.Get("upperProxyUseAuth"),
                (string)Conf.Get("upperProxyAuthName"),
                (string)Conf.Get("upperProxyAuthPass"));
            var proxy = new Proxy(Kernel,Logger, (SockTcp)sockObj, Timeout, upperProxy);//プロキシ接続情報
            ProxyObj proxyObj = null;

            //最初のリクエスト取得
            for(int i = 0;IsLife() && proxy.Length(CS.Client) == 0;i++) {
                //まだサーバと未接続の段階では、クライアントからのリクエストがない場合、
                //このスレッドはエラーとなる
                Thread.Sleep(50);
                if(i > 100)
                    goto end;//切断
            }
            //新たなHTTPオブジェクトを生成する
            var oneObj = new OneObj(proxy);

            //リクエスト行・ヘッダ・POSTデータの読み込み・URL制限
            if(!oneObj.RecvRequest(_useRequestLog,_limitUrl,this))
                goto end;

            //HTTPの場合
            if (oneObj.Request.Protocol == ProxyProtocol.Http) {

                proxyObj = new ProxyHttp(proxy,Kernel,Conf,_cache,_limitString);//HTTPデータ管理オブジェクト

                //最初のオブジェクトの追加
                proxyObj.Add(oneObj);

                while(IsLife()) {//デフォルトで継続型

                    //*******************************************************
                    //プロキシ処理
                    //*******************************************************
                    if(!proxyObj.Pipe(this))
                        goto end;

                    if(!((ProxyHttp)proxyObj).KeepAlive) {
                        if(proxyObj.IsFinish()) {
                            Logger.Set(LogKind.Debug,null,999,"break keepAlive=false");
                            break;
                        }
                    }

                    //*******************************************************
                    //次のリクエストを取得
                    //*******************************************************
                    //if(((ProxyHttp)proxyObj).KeepAlive) {
                        for(var i = 0;i < 30;i++) {
                            if(proxy.Length(CS.Client) != 0) {

                                //新たなHTTPオブジェクトを生成する
                                oneObj = new OneObj(proxy);

                                //リクエスト行・ヘッダ・POSTデータの読み込み・URL制限
                                if(!oneObj.RecvRequest(_useRequestLog,_limitUrl,this))
                                    goto end;

                                if (oneObj.Request.Protocol != ProxyProtocol.Http) {
                                    goto end;//Ver5.0.2
                                }
                                //HTTPオブジェクトの追加
                                proxyObj.Add(oneObj);

                            } else {
                                if(!proxyObj.IsFinish())
                                    break;
                                
                                //Ver5.6.1 最適化
                                if (!proxyObj.WaitProcessing()) {
                                    Thread.Sleep(5);
                                }
                            }
                        }
                    //}
                    //デバッグログ
                    //proxyObj.DebugLog();

                    if(proxyObj.IsTimeout()) {
                        Logger.Set(LogKind.Debug,null,999,string.Format("break waitTime>{0}sec [Option Timeout]",proxy.OptionTimeout));
                        break;
                    }
                    //Ver5.1.4-b1
                    //Thread.Sleep(500);
                        
                    Thread.Sleep(1);//Ver5.6.1これを0にするとCPU使用率が100%になってしまう
                }
            } else if (oneObj.Request.Protocol == ProxyProtocol.Ssl) {

                proxyObj = new ProxySsl(proxy);//SSLデータ管理オブジェクト

                //オブジェクトの追加
                proxyObj.Add(oneObj);

                while(IsLife()) {//デフォルトで継続型

                    //*******************************************************
                    //プロキシ処理
                    //*******************************************************
                    if(!proxyObj.Pipe(this))
                        goto end;

                    //デバッグログ
                    //proxyObj.DebugLog();

                    if(proxyObj.IsTimeout()) {
                        Logger.Set(LogKind.Debug,null,999,string.Format("break waitTime>{0}sec [Option Timeout]",proxy.OptionTimeout));
                        break;
                    }
                    //Ver5.0.0-b13
                    //Thread.Sleep(500);
                    Thread.Sleep(1);
                }
            } else if (oneObj.Request.Protocol == ProxyProtocol.Ftp) {
                proxyObj = new ProxyFtp(proxy,Kernel,Conf,this,++_dataPort);//FTPデータ管理オブジェクト

                //オブジェクトの追加
                proxyObj.Add(oneObj);

                //*******************************************************
                //プロキシ処理
                //*******************************************************
                proxyObj.Pipe(this);

                _dataPort = ((ProxyFtp)proxyObj).DataPort;
                if(_dataPort>DataPortMax)
                    _dataPort = DataPortMin;

            }
        end:
            //終了処理
            if(proxyObj != null)
                proxyObj.Dispose();
            proxy.Dispose();

        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }
    }
}

