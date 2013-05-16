using System.Text;
using Bjd;
using SmtpServer;

namespace SmtpServerTest {
    class TestMail {
        public Mail Mail { get; private set; }
        public MlEnvelope MlEnvelope { get; private set; }
        public TestMail(string from, string to, string bodyStr) {
            this.Mail = new Mail(null);
            Mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            var body = Encoding.ASCII.GetBytes(bodyStr);
            Mail.Init(body);
            Mail.AddHeader("from", from);
            Mail.AddHeader("to", to);

            var host = "TEST";
            var addr = new Ip("10.0.0.1");
            MlEnvelope = new MlEnvelope(CreateMailAddress(from), CreateMailAddress(to), host, addr);
        }

        MailAddress CreateMailAddress(string str) {
            var addr = str;
            var s0 = str.IndexOf("<");
            if (s0 != -1) {
                var tmp = str.Substring(s0 + 1);
                var s1 = tmp.IndexOf(">");
                if (s1 != -1) {
                    addr = tmp.Substring(0, s1);
                }
            }
            return new MailAddress(addr);
        }
    }
}
