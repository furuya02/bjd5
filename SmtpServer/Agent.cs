using System.Collections.Generic;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.option;

namespace SmtpServer {
    class Agent : ThreadBase {
        readonly Conf _conf;
        readonly Logger _logger;

        readonly MailQueue _mailQueue;
        readonly bool _always;//キュー常時処理

        //暫定
        private Kernel _kernel;
        private Server _server;


        //public Agent(Server server, Kernel kernel, MailQueue mailQueue, SaveMail saveMail,bool always):base(kernel,"Agent") {
        public Agent(Kernel kernel, Server server,Conf conf, Logger logger,MailQueue mailQueue, bool always)
            : base(kernel.CreateLogger("Agent",true, null)) {
            _conf = conf;
            _logger = logger;
            _mailQueue = mailQueue;

            _always = always;

            //暫定
            _kernel = kernel;
            _server = server;
        }
        override protected bool OnStartThread() { return true; }//前処理
        override protected void OnStopThread() { }//後処理
        override protected void OnRunThread() {//本体

            //[C#]
            //IsRunning = true;
            KindThreadBase = KindThreadBase.Running;

            
            var ar = new List<OneAgent>();
            var threadMax = (int)_conf.Get("threadMax");//スレッド多重化数
            var threadSpan = (int)_conf.Get("threadSpan");//最小処理間隔（分）

            //サーバ名が指定されていないと送信に失敗する可能性が有る
            if (_kernel.ServerName == "")
                _logger.Set(LogKind.Error, null, 20, "");

            while (IsLife()) {

                if (!_always) {//キュー常時処理
                    Thread.Sleep(300);
                    continue;
                }

                //キューから最小処理時間を経過しているメールを取り出す（取得するのは、最大で「スレッド多重化数」まで）
                List<OneQueue> queueList = _mailQueue.GetList(threadMax, threadSpan);
                if (queueList.Count == 0) {
                    //for (int i = 0; i < 6000 && life; i++) {
                    Thread.Sleep(10);//処理対象が無い場合は、少し(1分程度)休憩
                    //}
                    continue;
                }
                //取得したリスト分だけ並列（多重）で実行
                foreach (OneQueue oneQueue in queueList) {
                    //Vrt5.3.6
                    //if(oneQueue.MailInfo.From.ToString()==oneQueue.MailInfo.To.ToString())
                    //    continue;//ループメールは処置しない

                    //OneAgent oneAgent = new OneAgent(kernel, server, mailQueue, this, oneQueue);
                    var oneAgent = new OneAgent(_kernel,_server ,_conf,_logger, _mailQueue, oneQueue);
                    oneAgent.Start();
                    ar.Add(oneAgent);
                }

                //全部が終了するのを待つ（OneAgentよりAgentが先に削除されると問題がある）
                //life=falseでOneAgentはそれぞれ（中断して）終了に向かう
                //ここでは、OneAgentが全部処理を終えるまで待機する
                while (true) {
                    bool isRun = false;
                    foreach (OneAgent oneAgent in ar) {
                        //if (oneAgent.IsRunning) {
                        if(oneAgent.KindThreadBase == KindThreadBase.Running){
                            isRun = true;
                            break;
                        }
                    }
                    if (!isRun)
                        break;
                    Thread.Sleep(100);
                }
            }
        }

        public override string GetMsg(int no){
            throw new System.NotImplementedException();
        }
    }
}