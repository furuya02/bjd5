using System;
using System.Collections.Generic;
using System.Threading;
using Bjd;
using Bjd.option;
using Bjd.util;

namespace SmtpServer{
    class Fetch : ThreadBase {
        readonly List<OneFetch> _ar = new List<OneFetch>();
        public Fetch(Kernel kernel,Server server,Conf conf)
            : base(kernel.CreateLogger("Fetch",true,null)) {
            //this.server = server;
            var timeout = (int)conf.Get("timeOut");
            var sizeLimit = (int)conf.Get("sizeLimit");
            var dat = (Dat)conf.Get("fetchList");
            if (dat != null){
                foreach (var o in dat) {
                    if (o.Enable) {
                        int interval = Convert.ToInt32(o.StrList[0]);//受信間隔
                        string host = o.StrList[1];//サーバ
                        int port = Convert.ToInt32(o.StrList[2]);//ポート
                        string user = o.StrList[3];//ユーザ
                        string pass = Crypt.Decrypt(o.StrList[4]);//パスワード
                        string localUser = o.StrList[5];//ローカルユーザ


                        //Ver5.2.7 旧バージョン(5.2以前)との互換のため
                        int synchronize;
                        try {
                            synchronize = Convert.ToInt32(o.StrList[6]);//同期
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
                        int keepTime = Convert.ToInt32(o.StrList[7]);//サーバに残す時間
                        var fetchOption = new FetchOption(interval, host, port, user, pass, localUser, synchronize, keepTime);
                        //Ver5.6.0
                        //ar.Add(new OneFetch(kernel, server, fetchOption,timeout, sizeLimit));
                        _ar.Add(new OneFetch(kernel, server, fetchOption, timeout, sizeLimit));
                    }
                }
            }
        }
        override protected bool OnStartThread() { return true; }//前処理
        override protected void OnStopThread() { }//後処理
        override protected void OnRunThread() {//本体
            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;

            while (IsLife()) {
                DateTime now = DateTime.Now;
                foreach (OneFetch oneFetch in _ar) {
                    oneFetch.Job(now, this);
                    Thread.Sleep(500);
                }
                for (int i = 0; i < 100 && IsLife(); i++) {
                    Thread.Sleep(100);
                }
            }
        }

        public override string GetMsg(int no){
            throw new NotImplementedException();
        }
    }
}