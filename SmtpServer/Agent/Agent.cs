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
        readonly bool _always;//�L���[�펞����

        //�b��
        private Kernel _kernel;
        private Server _server;


        //public Agent(Server server, Kernel kernel, MailQueue mailQueue, SaveMail saveMail,bool always):base(kernel,"Agent") {
        public Agent(Kernel kernel, Server server,Conf conf, Logger logger,MailQueue mailQueue, bool always)
            : base(kernel.CreateLogger("Agent",true, null)) {
            _conf = conf;
            _logger = logger;
            _mailQueue = mailQueue;

            _always = always;

            //�b��
            _kernel = kernel;
            _server = server;
        }
        override protected bool OnStartThread() { return true; }//�O����
        override protected void OnStopThread() { }//�㏈��
        override protected void OnRunThread() {//�{��

            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;

            
            var ar = new List<OneAgent>();
            var threadMax = (int)_conf.Get("threadMax");//�X���b�h���d����
            var threadSpan = (int)_conf.Get("threadSpan");//�ŏ������Ԋu�i���j

            //�T�[�o�����w�肳��Ă��Ȃ��Ƒ��M�Ɏ��s����\�����L��
            if (_kernel.ServerName == "")
                _logger.Set(LogKind.Error, null, 20, "");

            while (IsLife()) {

                if (!_always) {//�L���[�펞����
                    Thread.Sleep(300);
                    continue;
                }

                //�L���[����ŏ��������Ԃ�o�߂��Ă��郁�[������o���i�擾����̂́A�ő�Łu�X���b�h���d�����v�܂Łj
                List<OneQueue> queueList = _mailQueue.GetList(threadMax, threadSpan);
                if (queueList.Count == 0) {
                    //for (int i = 0; i < 6000 && life; i++) {
                    Thread.Sleep(10);//�����Ώۂ������ꍇ�́A����(1�����x)�x�e
                    //}
                    continue;
                }
                //�擾�������X�g����������i���d�j�Ŏ��s
                foreach (OneQueue oneQueue in queueList) {
                    //Vrt5.3.6
                    //if(oneQueue.MailInfo.From.ToString()==oneQueue.MailInfo.To.ToString())
                    //    continue;//���[�v���[���͏��u���Ȃ�

                    //OneAgent oneAgent = new OneAgent(kernel, server, mailQueue, this, oneQueue);
                    var oneAgent = new OneAgent(_kernel,_server ,_conf,_logger, _mailQueue, oneQueue);
                    oneAgent.Start();
                    ar.Add(oneAgent);
                }

                //�S�����I������̂�҂iOneAgent���Agent����ɍ폜�����Ɩ�肪����j
                //life=false��OneAgent�͂��ꂼ��i���f���āj�I���Ɍ�����
                //�����ł́AOneAgent���S��������I����܂őҋ@����
                while (true) {
                    bool isRun = false;
                    foreach (OneAgent oneAgent in ar) {
                        if(oneAgent.ThreadBaseKind == ThreadBaseKind.Running){
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