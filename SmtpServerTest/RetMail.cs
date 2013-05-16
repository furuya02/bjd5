using Bjd;

namespace SmtpServerTest {
    class RetMail {
        public Mail Mail { get; private set; }
        public MailAddress From { get; private set; }
        public MailAddress To { get; private set; }
        public RetMail(MailAddress from, MailAddress to, Mail mail) {
            this.From = from;
            this.To = to;
            this.Mail = mail;
        }
    }
}