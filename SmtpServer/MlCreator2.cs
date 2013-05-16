using System.Collections.Generic;
using System.Text;
using Bjd;

namespace SmtpServer {
    class MlCreator2 {
        //******************************************************************
        //メール生成クラス
        // mailヘッダのFrom及びToは追加さない(MlSenderで最終的に追加する)
        //******************************************************************
        readonly MlAddr mlAddr;
        //readonly MlOption mlOption;
        private List<string> docs;
        public MlCreator2(MlAddr mlAddr,List<string>docs) {
            this.mlAddr = mlAddr;
            this.docs = docs;

            //this.mlOption = mlOption;

        }
        //メールを添付する
        public Mail Attach(string subject, Mail orgMail) {
            var mail = new Mail(orgMail.Logger);
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            //ヘッダ作成
            mail.AddHeader("subject", subject);
            //本文作成
            mail.Init(Encoding.ASCII.GetBytes(subject + "\r\n"));
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));
            mail.Init(Encoding.ASCII.GetBytes("Original mail as follows:\r\n"));
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));
            //オリジナルメールの添付
            List<byte[]> body = Inet.GetLines(orgMail.GetBytes());
            foreach (byte[] buf in body) {
                mail.Init(Encoding.ASCII.GetBytes("  "));//行頭に空白を追加
                mail.Init(buf);
            }
            return mail;
        }
        public Mail Deny() {
            var subject = string.Format("You are not member ({0} ML)", mlAddr.Name);

            //var bodyStr = mlDoc.Get(MLDocKind.Deny);
            var bodyStr = mlAddr.Conv(docs[(int) MLDocKind.Deny]);
            
            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        public Mail Welcome() {
            return Fixed(MLDocKind.Welcome);
        }
        public Mail Guide() {
            return Fixed(MLDocKind.Guide);
        }
        public Mail Help() {
            return Fixed(MLDocKind.Help);
        }
        public Mail Admin() {
            return Fixed(MLDocKind.Admin);
        }
        public Mail Confirm(string confirmStr) {

            var subject = string.Format("Subscribe confirmation request ({0} ML)", mlAddr.Name);
            var bodyStr = mlAddr.Conv(docs[(int) MLDocKind.Deny]);
            bodyStr = Util.SwapStr("$CONFIRM", confirmStr, bodyStr);

            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        public Mail Append(string appendStr) {

            var subject = string.Format("{0} ({1} ML)", MLDocKind.Append.ToString().ToLower(), mlAddr.Name);
            string bodyStr = mlAddr.Conv((docs[(int) MLDocKind.Append]));
            bodyStr = Util.SwapStr("$APPEND", appendStr, bodyStr);

            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        public Mail Log(string log) {
            var subject = string.Format("log ({0} ML)", mlAddr.Name);
            return Create(ContentTyep.Sjis, subject, log);

        }
        public Mail Member(string member) {
            string subject = string.Format("member request ({0} ML)", mlAddr.Name);
            return Create(ContentTyep.Ascii, subject, member);
        }

        public Mail Get(MlMailDb mlDb, int start, int end) {
            string boundaryStr = "BJD-Boundary";
            byte[] buf = new byte[0];
            //ライブラリからの取得
            for (int i = start; i <= end; i++) {
                var mail = mlDb.Read(i);
                if (mail != null) {
                    buf = Bytes.Create(buf, Encoding.ASCII.GetBytes(string.Format("--{0}\r\n", boundaryStr)));
                    buf = Bytes.Create(buf, Encoding.ASCII.GetBytes("Content-Type: message/rfc822\r\n\r\n"));
                    buf = Bytes.Create(buf, mail.GetBytes());
                }
            }
            buf = Bytes.Create(buf, Encoding.ASCII.GetBytes(string.Format("--{0}--\r\n", boundaryStr)));

            var subject = string.Format("result for get [{0}-{1} MIME/multipart] ({2} ML)", start, end, mlAddr.Name);
            var contentType = string.Format("multipart/mixed;\r\n boundary=\"{0}\"\r\n", boundaryStr);

            return Create(subject, contentType, buf);
        }

        public Mail Summary(MlMailDb mlDb, MlSubject mlSubject, int start, int end) {
            //ライブラリからの取得
            Encoding encoding = null;
            StringBuilder sb = new StringBuilder();
            for (int i = start; i <= end; i++) {
                var mail = mlDb.Read(i);
                if (mail == null) {
                    sb.Append(mlSubject.Get(i) + " library no't found.\r\n");
                } else {
                    string str = mail.GetHeader("subject");
                    if (str != null) {
                        str = Subject.Decode(ref encoding, str);
                        sb.Append(string.Format("{0}\r\n", str));
                    } else {
                        sb.Append(mlSubject.Get(i) + " subject no't found.\r\n");
                    }
                }
            }
            string subject = string.Format("result for summary [{0}-{1}] ({2} ML)", start, end, mlAddr.Name);
            return Create(ContentTyep.Sjis, subject, sb.ToString());
        }

        Mail Fixed(MLDocKind mlDocKind) {
            var subject = string.Format("{0} ({1} ML)", mlDocKind.ToString().ToLower(), mlAddr.Name);
            var bodyStr = mlAddr.Conv((docs[(int)mlDocKind]));
            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        Mail Create(string subject, string contentType, byte[] body) {
            Mail mail = new Mail(null);
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            //ヘッダ作成
            mail.AddHeader("subject", subject);
            mail.AddHeader("Content-Type", contentType);
            //本文作成
            mail.Init(body);
            return mail;
        }
        Mail Create(ContentTyep contentType, string subject, string bodyStr) {
            var encoding = Encoding.ASCII;
            var contentTypeStr = "text/plain; charset==us-ascii";
            switch (contentType) {
                case ContentTyep.Ascii:
                    break;
                case ContentTyep.Sjis:
                    encoding = Encoding.GetEncoding("shift-jis");
                    contentTypeStr = string.Format("text/plain; charset={0}", encoding.HeaderName);
                    break;
            }
            return Create(subject, contentTypeStr, encoding.GetBytes(bodyStr));
        }
    }
}