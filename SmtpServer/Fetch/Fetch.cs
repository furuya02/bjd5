using System;
using System.Collections.Generic;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.util;

namespace SmtpServer{
    class Fetch : ThreadBase {
        readonly ListFetchJob _listFetchJob;
        private readonly Logger _logger;
        private Server _server;
        private Kernel _kernel;

          public Fetch(Kernel kernel, Server server, IEnumerable<OneDat> fetchList, int timeout,int sizeLimit)
            : base(kernel.CreateLogger("FetchThread", true, null)){
              _kernel = kernel;
            _server = server;
            _logger = kernel.CreateLogger("Fetch", true, this);
            _listFetchJob = new ListFetchJob(kernel, _logger, fetchList, timeout, sizeLimit);
           
        }
        override protected bool OnStartThread() { return true; }//前処理
        override protected void OnStopThread() { }//後処理
        override protected void OnRunThread() {//本体
            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;

            while (IsLife()) {
                var now = DateTime.Now;
                foreach (OneFetchJob oneFetchJob in _listFetchJob) {
                    oneFetchJob.Job2(_server,now,_logger,this);
                    Thread.Sleep(500);
                }
                for (int i = 0; i < 100 && IsLife(); i++) {
                    Thread.Sleep(100);
                }
            }
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 0: return "Failed in　nslookup";
                case 1: return _kernel.IsJp() ? "自動受信" : "The automatic reception";
                case 2: return _kernel.IsJp() ? "ホスト名に問題あるため処理を中止しました" : "Because host name included a problem, I canceled processing";
                case 3: return _kernel.IsJp() ? "サーバへの接続に失敗しました" : "Connection failure to a server";
                case 4: return _kernel.IsJp() ? "ログインに失敗しました" : "Failed in login";
                case 5: return _kernel.IsJp() ? "QUIT送信に失敗しました" : "Failed in send QUIT";

            }
            return "unknown";
        }


        class ListFetchJob : ListBase<OneFetchJob> {
            public ListFetchJob(Kernel kernel, Logger logger,IEnumerable<OneDat> fetchList, int timeout, int sizeLimit) {
                if (fetchList != null) {
                    foreach (var o in fetchList) {
                        if (o.Enable) {
                            var interval = Convert.ToInt32(o.StrList[0]); //受信間隔
                            var host = o.StrList[1]; //サーバ
                            var port = Convert.ToInt32(o.StrList[2]); //ポート
                            var user = o.StrList[3]; //ユーザ
                            var pass = Crypt.Decrypt(o.StrList[4]); //パスワード
                            var localUser = o.StrList[5]; //ローカルユーザ


                            //Ver5.2.7 旧バージョン(5.2以前)との互換のため
                            int synchronize;
                            try {
                                synchronize = Convert.ToInt32(o.StrList[6]); //同期
                            } catch {
                                var s = o.StrList[6];
                                if (s == "サーバに残す" || s == "An email of a server does not eliminate it") {
                                    synchronize = 0;
                                } else if (s == "メールボックスと同期する" || s == "Synchronize it with a mailbox") {
                                    synchronize = 1;
                                } else if (s == "サーバから削除する" || s == "An email of a server eliminates it") {
                                    synchronize = 2;
                                } else {
                                    continue; //コンバート失敗したので設定データは無効
                                }
                            }
                            var keepTime = Convert.ToInt32(o.StrList[7]); //サーバに残す時間
                            var oneFetch = new OneFetch(interval, host, port, user, pass, localUser, synchronize, keepTime);
                            if (oneFetch.Ip == null){
                                logger.Set(LogKind.Error, null, 0, string.Format("host={0}",host));
                            }
                            Ar.Add(new OneFetchJob(kernel, oneFetch,timeout,sizeLimit));
                        }
                    }
                }
            }
        }
    }
}