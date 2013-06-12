using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Bjd;
using Bjd.log;
using Bjd.mail;

namespace SmtpServer {
    class MailQueue {
        readonly object _lockObj = new Object();
 
        public bool Status { get; private set; }//��������� false�̏ꍇ�́A�������Ɏ��s���Ă���̂Ŏg�p�ł��Ȃ�
        public string Dir { get; private set; }

        public MailQueue(string currentDirectory) {

            Status = true;//��������� false�̏ꍇ�́A�������Ɏ��s���Ă���̂Ŏg�p�ł��Ȃ�

            //���N���X��string dir�̏�����
            Dir = string.Format("{0}\\MailQueue", currentDirectory);
            if (Directory.Exists(Dir))
                return;
            try {
                Directory.CreateDirectory(Dir);
            } catch {
                Status = false;//���������s
                Dir = null;
            }
        }

        //�d�����Ȃ��t�@�C������擾����
        protected string CreateName() {
            while (true) {
                var str = string.Format("{0:D20}", DateTime.Now.Ticks);
                Thread.Sleep(1);//Ver5.0.0-b18
                var fileName = string.Format("{0}\\MF_{1}", Dir, str);
                if (!Directory.Exists(fileName)) {
                    return str;
                }
            }
        }

        //sec:�Ō��GetList���Ă���sec���Ԍo�߂��Ȃ���͎̂擾�̑ΏۊO�Ƃ���
        public List<OneQueue> GetList(int max, int sec) {

            var queueList = new List<OneQueue>();

            lock (_lockObj) {//�r������
                foreach (var fileName in Directory.GetFiles(Dir, "DF_*")) {
                    if (queueList.Count == max)
                        break;
                    var mailInfo = new MailInfo(fileName);

                    //�����Ώۂ��ǂ����̊m�F
                    if (mailInfo.IsProcess(sec, fileName)) {
                        var fname = Path.GetFileName(fileName);
                        queueList.Add(new OneQueue(fname.Substring(3), mailInfo));
                    }
                }
                return queueList;
            }
        }
        public void Delete(string fname) {
            lock (_lockObj) {//�r������
                var fileName = string.Format("{0}\\MF_{1}", Dir, fname);
                File.Delete(fileName);
                fileName = string.Format("{0}\\DF_{1}", Dir, fname);
                File.Delete(fileName);
            }
        }
        public bool Save(Mail mail, MailInfo mailInfo) {

            lock (_lockObj) {//�r������
                var fname = CreateName();
                var fileName = string.Format("{0}\\MF_{1}", Dir, fname);
                if (mail.Save(fileName)){
                    fileName = string.Format("{0}\\DF_{1}", Dir, fname);
                    mailInfo.Save(fileName);
                    return true;
                }
                return false;
            }
        }
        public bool Read(string fname, ref Mail mail) {
            lock (_lockObj) {//�r������
                var fileName = string.Format("{0}\\MF_{1}", Dir, fname);
                return mail.Read(fileName);
            }
        }
    }
}

