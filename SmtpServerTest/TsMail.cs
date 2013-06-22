using System.Text;
using Bjd;
using Bjd.mail;
using Bjd.net;
using SmtpServer;

namespace SmtpServerTest {
    //******************************************************************
    // テスト用メールオブジェクト
    //******************************************************************
    class TsMail {
        public Mail Mail { get; private set; }
        public MlEnvelope MlEnvelope { get; private set; }
        public TsMail(string from, string to, string bodyStr) {
            Mail = new Mail();
            Mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            var body = Encoding.ASCII.GetBytes(bodyStr);
            Mail.AppendLine(body);
            Mail.AddHeader("from", from);
            Mail.AddHeader("to", to);

            const string host = "TEST";
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
