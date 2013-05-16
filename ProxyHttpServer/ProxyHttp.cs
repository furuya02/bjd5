using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.option;
using Bjd.sock;

namespace ProxyHttpServer {
    class ProxyHttp : ProxyObj {

        //readonly OneOption _oneOption;//オプションヘッダの追加のため
        Conf _conf;
        readonly Kernel _kernel;//オプションヘッダの追加のため
        readonly Cache _cache;
        public LimitString LimitString { get; private set; }
        public bool KeepAlive { get; private set; }

        //データオブジェクト
        List<OneProxyHttp> _ar = new List<OneProxyHttp>();
        int _indexServer;//サーバ側にどこまで送信を完了したかのインデックス
        int _indexClient;//クライアント側にどこまで送信を完了したかのインデックス
        int _indexRecv;//サーバ側からのデータを、どこまで受信完了したかのインデックス

        public ProxyHttp(Proxy proxy, Kernel kernel, Conf conf, Cache cache, LimitString limitString)
            : base(proxy) {
            _kernel = kernel;
            //_oneOption = oneOption;
            _conf = conf;
            _cache = cache;
            LimitString = limitString;
            KeepAlive = true;//デフォルトで継続型
        }
        override public void Dispose() {
            _ar = null;
        }

        //クライアントへの送信がすべて完了しているかどうかの確認
        override public bool IsFinish() {
            if (_indexClient == _ar.Count)
                return true;
            return false;
        }
        override public bool IsTimeout() {
            if (IsFinish()) {
                var waitTime = _ar.Select(oneProxyHttp => oneProxyHttp.WaitTime).Concat(new long[]{0}).Min();
                if (waitTime > Proxy.OptionTimeout) {
                    return true;
                }

            }
            return false;
        }

        //データオブジェクトの追加
        override public void Add(OneObj oneObj) {

            //オプション指定によるヘッダの追加処理
            if (!(bool)_conf.Get("useBrowserHedaer")) {
                if ((bool)_conf.Get("addHeaderRemoteHost")) {
                    //    oneObj.Header[cs].Append(key,val);
                    oneObj.Header[CS.Client].Append("Remote-Host-Wp", Encoding.ASCII.GetBytes(Define.ServerAddress()));
                }
                if ((bool)_conf.Get("addHeaderXForwardedFor")) {
                    oneObj.Header[CS.Client].Append("X-Forwarded-For", Encoding.ASCII.GetBytes(Define.ServerAddress()));
                }
                if ((bool)_conf.Get("addHeaderForwarded")) {
                    string str = string.Format("by {0} (Version {1}) for {2}", Define.ApplicationName(), _kernel.Ver.Version(), Define.ServerAddress());
                    oneObj.Header[CS.Client].Append("Forwarded", Encoding.ASCII.GetBytes(str));
                }
            }

            if (_ar.Count == 0) {
                if (oneObj.Request.HttpVer != "HTTP/1.1"){
                    KeepAlive = false;//非継続型
                }
            }

            var oneProxyHttp = new OneProxyHttp(Proxy, this, oneObj);
            //キャッシュの確認
            oneProxyHttp.CacheConform(_cache);
            _ar.Add(oneProxyHttp);
        }


        override public void DebugLog() {
            var list = new List<string>();

            //すべてのプロキシが完了している
            if (_indexClient == _ar.Count) {
                list.Add(string.Format("[HTTP] SOCK_STATE sv={0} cl={1} Finish/{2} HostName={3}", Proxy.Sock(CS.Server).SockState, Proxy.Sock(CS.Client).SockState, _ar.Count, Proxy.HostName));
            } else{
                list.Add(string.Format("[HTTP] SOCK_STATE sv={0} cl={1} {2}/{3} HostName={4}", Proxy.Sock(CS.Server).SockState, Proxy.Sock(CS.Client).SockState, _ar.Count, _indexClient, Proxy.HostName));
                list.AddRange(_ar.Select((t, i) => t.DebugLog(i)).SelectMany(l => l));
            }
            foreach (string s in list)
                Proxy.Logger.Set(LogKind.Debug, null, 999, s);
        }


        //プロキシ処理
        override public bool Pipe(ILife iLife) {

            if (!SendServer(iLife))//サーバへの送信
                return false;
            if (!RecvServer(iLife))//サーバからの受信
                return false;
            if (!SendClient(iLife))//クライアントへの送信
                return false;

            if (Proxy.Sock(CS.Server).SockState != SockState.Connect) {
                if (_indexClient == _ar.Count) {
                    return false;
                }
            }
            //クライアントから切断された場合は、常に処理終了
            if (Proxy.Sock(CS.Client).SockState != SockState.Connect) {
                Proxy.Logger.Set(LogKind.Debug, null, 999, "□Break ClientSocket!=CONNECT");
                return false;
            }

            return true;
        }

        //サーバ側への送信
        bool SendServer(ILife iLife) {
            for (int i = _indexServer; iLife.IsLife() && i < _ar.Count; i++) {
                //次のオブジェクトの接続先が現在接続中のサーバと違う場合
                if (Proxy.Sock(CS.Server) != null && _ar[i].HostName != Proxy.HostName) {
                    //既存のプロキシ処理が完了するまで、次のサーバ送信（リクエスト送信）は待機となる
                    if (i < _indexClient)
                        return true;
                }
                if (!_ar[i].SendServer(iLife)) {
                    return false;
                }
                _indexServer++;
            }
            return true;
        }

        //クライアント側への送信
        bool SendClient(ILife iLife) {
            for (int i = _indexClient; iLife.IsLife() && i < _ar.Count; i++) {
                if (!_ar[i].SendClient(iLife)) {
                    return false;
                }
                //クライアントへの送信が完了しているかどうかの確認
                if (_ar[i].SideState(CS.Client) != HttpSideState.ClientSideSendBody) {
                    break;
                }
                //送信が完了している場合は、次のデータオブジェクトの処理に移行する
                //proxy.Logger.Set(LogKind.Debug,null,999,string.Format("■indexClient {0}->{1}",indexClient,indexClient + 1));

                //キャッシュが可能な場合は、ここでキャッシュされる
                _ar[_indexClient].CacheWrite(_cache);
                //ここでオブジェクトは破棄される
                _ar[_indexClient].Dispose();

                _indexClient++;

            }
            return true;
        }

        //サーバ側からの受信
        bool RecvServer(ILife iLife) {
            for (int i = _indexRecv; iLife.IsLife() && i < _ar.Count; i++) {
                if (!_ar[i].RecvServer(iLife)) {
                    Proxy.Logger.Set(LogKind.Debug, null, 999, "[HTTP] Break RecvServer()");
                    return false;
                }
                //サーバ側からの受信が完了しているかどうかの確認
                if (_ar[i].SideState(CS.Server) != HttpSideState.ServerSideRecvBody)
                    break;
                //送信が完了しているばあは、次のデータオブジェクトの処理に移る
                _indexRecv++;
            }
            return true;
        }
        override public bool WaitProcessing() {
            if (_ar.Count > 0)
                return true;
            return base.WaitProcessing();
        }
    }
}

