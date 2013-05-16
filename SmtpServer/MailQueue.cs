using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Bjd;
using Bjd.mail;

namespace SmtpServer {
    class MailQueue {
        //Ver5.4.8
        readonly object _lockObj = new Object();

        public MailQueue(string currentDirectory) {

            Status = true;//‰Šú‰»ó‘Ô false‚Ìê‡‚ÍA‰Šú‰»‚É¸”s‚µ‚Ä‚¢‚é‚Ì‚Åg—p‚Å‚«‚È‚¢

            //Šî’êƒNƒ‰ƒX‚Ìstring dir‚Ì‰Šú‰»
            Dir = string.Format("{0}\\MailQueue", currentDirectory);
            if (Directory.Exists(Dir))
                return;
            try {
                Directory.CreateDirectory(Dir);
            } catch {
                Status = false;//‰Šú‰»¸”s
                Dir = null;
            }
        }
        public bool Status { get; private set; }//‰Šú‰»ó‘Ô false‚Ìê‡‚ÍA‰Šú‰»‚É¸”s‚µ‚Ä‚¢‚é‚Ì‚Åg—p‚Å‚«‚È‚¢
        public string Dir { get; private set; }

        //d•¡‚µ‚È‚¢ƒtƒ@ƒCƒ‹–¼‚ğæ“¾‚·‚é
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

        public List<OneQueue> GetList(int max, int threadSpan) {

            //DateTime now = DateTime.Now;

            var queueList = new List<OneQueue>();

            //Ver5.4.8
            //lock (this) {//”r‘¼§Œä
            lock (_lockObj) {//”r‘¼§Œä
                foreach (var fileName in Directory.GetFiles(Dir, "DF_*")) {
                    if (queueList.Count == max)
                        break;
                    var mailInfo = new MailInfo(fileName);

                    //ˆ—‘ÎÛ‚©‚Ç‚¤‚©‚ÌŠm”F
                    if (mailInfo.IsProcess(threadSpan, fileName)) {
                        var fname = Path.GetFileName(fileName);
                        //          if(Sw || Df.State==1){
                        queueList.Add(new OneQueue(fname.Substring(3), mailInfo));
                    }
                }
                return queueList;
            }
        }
        public void Delete(string fname) {
            //Ver5.4.8
            //lock (this) {//”r‘¼§Œä
            lock (_lockObj) {//”r‘¼§Œä
                var fileName = string.Format("{0}\\MF_{1}", Dir, fname);
                File.Delete(fileName);
                fileName = string.Format("{0}\\DF_{1}", Dir, fname);
                File.Delete(fileName);
            }
        }
        //public bool Save(Mail mail,MailAddress from, MailAddress to, string host, string addr, string date, string uid) {
        public bool Save(Mail mail, MailInfo mailInfo) {

            //Ver5.4.8
            //lock (this) {//”r‘¼§Œä
            lock (_lockObj) {//”r‘¼§Œä
                var fname = CreateName();
                var fileName = string.Format("{0}\\MF_{1}", Dir, fname);
                if (mail.Save(fileName)) {
                    fileName = string.Format("{0}\\DF_{1}", Dir, fname);
                    mailInfo.Save(fileName);
                    return true;
                }
                return false;
            }
        }
        public bool Read(string fname, ref Mail mail) {
            //Ver5.4.8
            //lock (this) {//”r‘¼§Œä
            lock (_lockObj) {//”r‘¼§Œä
                var fileName = string.Format("{0}\\MF_{1}", Dir, fname);
                return mail.Read(fileName);
            }
        }
    }
}

