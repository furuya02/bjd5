using System.Collections.Generic;
using System.Text;
using Bjd;
using Bjd.mail;
using Bjd.util;

namespace SmtpServer {
    class MlCreator {
        //******************************************************************
        //メール生成クラス
        // mailヘッダのFrom及びToは追加さない(MlSenderで最終的に追加する)
        //******************************************************************
        readonly MlAddr _mlAddr;
        readonly List<string> _docs;
        public MlCreator(MlAddr mlAddr,List<string>docs) {
            _mlAddr = mlAddr;
            _docs = docs;
        }
        //メールを添付する
        public Mail Attach(string subject, Mail orgMail) {
            var mail = new Mail();

            mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            //ヘッダ作成
            mail.AddHeader("subject", subject);
            //本文作成
            mail.Init(Encoding.ASCII.GetBytes(subject + "\r\n"));
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));
            mail.Init(Encoding.ASCII.GetBytes("Original mail as follows:\r\n"));
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));
            //オリジナルメールの添付
            var body = Inet.GetLines(orgMail.GetBytes());
            foreach (var buf in body) {
                mail.Init(Encoding.ASCII.GetBytes("  "));//行頭に空白を追加
                mail.Init(buf);
            }
            return mail;
        }
        public Mail Deny() {
            var subject = string.Format("You are not member ({0} ML)", _mlAddr.Name);
            var bodyStr = _mlAddr.Conv(_docs[(int) MlDocKind.Deny]);
            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        public Mail Welcome() {
            return Fixed(MlDocKind.Welcome);
        }
        public Mail Guide() {
            return Fixed(MlDocKind.Guide);
        }
        public Mail Help() {
            return Fixed(MlDocKind.Help);
        }
        public Mail Admin() {
            return Fixed(MlDocKind.Admin);
        }
        public Mail Confirm(string confirmStr) {

            var subject = string.Format("Subscribe confirmation request ({0} ML)", _mlAddr.Name);
            var bodyStr = _mlAddr.Conv(_docs[(int) MlDocKind.Deny]);
            bodyStr = Util.SwapStr("$CONFIRM", confirmStr, bodyStr);

            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        public Mail Append(string appendStr) {

            var subject = string.Format("{0} ({1} ML)", MlDocKind.Append.ToString().ToLower(), _mlAddr.Name);
            var bodyStr = _mlAddr.Conv((_docs[(int) MlDocKind.Append]));
            bodyStr = Util.SwapStr("$APPEND", appendStr, bodyStr);

            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        public Mail Log(string log) {
            var subject = string.Format("log ({0} ML)", _mlAddr.Name);
            return Create(ContentTyep.Sjis, subject, log);

        }
        public Mail Member(string member) {
            var subject = string.Format("member request ({0} ML)", _mlAddr.Name);
            return Create(ContentTyep.Ascii, subject, member);
        }

        public Mail Get(MlMailDb mlDb, int start, int end) {
            const string boundaryStr = "BJD-Boundary";
            var buf = new byte[0];
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

            var subject = string.Format("result for get [{0}-{1} MIME/multipart] ({2} ML)", start, end, _mlAddr.Name);
            var contentType = string.Format("multipart/mixed;\r\n boundary=\"{0}\"\r\n", boundaryStr);

            return Create(subject, contentType, buf);
        }

        public Mail Summary(MlMailDb mlDb, MlSubject mlSubject, int start, int end) {
            //ライブラリからの取得
            Encoding encoding = null;
            var sb = new StringBuilder();
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
            var subject = string.Format("result for summary [{0}-{1}] ({2} ML)", start, end, _mlAddr.Name);
            return Create(ContentTyep.Sjis, subject, sb.ToString());
        }

        Mail Fixed(MlDocKind mlDocKind) {
            var subject = string.Format("{0} ({1} ML)", mlDocKind.ToString().ToLower(), _mlAddr.Name);
            var bodyStr = _mlAddr.Conv((_docs[(int)mlDocKind]));
            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        Mail Create(string subject, string contentType, byte[] body) {
            var mail = new Mail();
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