using Bjd;
using Bjd.mail;
using Bjd.net;

namespace SmtpServer {
    class MlEnvelope {
        public MailAddress From { get;private set; }
        public MailAddress To { get; private set; }
        public string Host { get; private set; }
        public Ip Addr { get; private set; }

        public MlEnvelope(MailAddress from, MailAddress to, string host, Ip addr) {
            From = from;
            To = to;
            Host = host;
            Addr = addr;
        }
        public MlEnvelope ChangeFrom(MailAddress from) {
            return new MlEnvelope(from, To, Host, Addr);
        }
        public MlEnvelope ChangeTo(MailAddress to) {
            return new MlEnvelope(From, to, Host, Addr);
        }
        public MlEnvelope Swap() {
            return new MlEnvelope(To,From,Host, Addr);
        }
    }
}
