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

        //fetchList = (Dat) conf.Get("fetchList");
        //_timeout = (int) conf.Get("timeOut");
        //_sizeLimit = (int) conf.Get("sizeLimit");
        public Fetch(Kernel kernel, Server server, IEnumerable<OneDat> fetchList, int timeout,int sizeLimit)
            : base(kernel.CreateLogger("Fetch", true, null)){
            _server = server;
            _listFetchJob = new ListFetchJob(kernel,fetchList,timeout,sizeLimit);
            _logger = kernel.CreateLogger("Fetch", true, this);
           
        }
        override protected bool OnStartThread() { return true; }//前処理
        override protected void OnStopThread() { }//後処理
        override protected void OnRunThread() {//本体
            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;

            while (IsLife()) {
                DateTime now = DateTime.Now;
                foreach (OneFetchJob oneFetchJob in _listFetchJob) {
                    oneFetchJob.Job(_server,now,_logger,this);
                    Thread.Sleep(500);
                }
                for (int i = 0; i < 100 && IsLife(); i++) {
                    Thread.Sleep(100);
                }
            }
        }

        public override string GetMsg(int no){
            return null;
        }

        class ListFetchJob : ListBase<OneFetchJob> {
            public ListFetchJob(Kernel kernel, IEnumerable<OneDat> fetchList, int timeout, int sizeLimit) {
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
                            Ar.Add(new OneFetchJob(kernel, oneFetch,timeout,sizeLimit));
                        }
                    }
                }
            }
        }
    }
}