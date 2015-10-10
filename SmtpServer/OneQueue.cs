using Bjd;
using Bjd.mail;

namespace SmtpServer {
    class OneQueue {
        readonly string _fname;
        public OneQueue(string fname, MailInfo mailInfo) {
            _fname = fname;
            MailInfo = mailInfo;
        }

        public MailInfo MailInfo { get; private set; }
        public Mail Mail(MailQueue mailQueue) {
            var mail = new Mail();
            return mailQueue.Read(_fname, ref mail) ? mail : null;
        }

        public void Delete(MailQueue mailQueue) {
            mailQueue.Delete(_fname);
        }
    }

}
