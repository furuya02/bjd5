using System.Collections.Generic;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.util;

namespace SmtpServer {
    class MlDelivery {
        //******************************************************************
        //メール配達クラス　生成して送信する
        //******************************************************************
        readonly MlUserList _mlUserList;
        readonly MlAddr _mlAddr;
        readonly Logger _logger;
        readonly MlSender _mlSender;
        readonly MlMailDb _mlDb;
        readonly MlSubject _mlSubject;
        readonly List<string> _docs;
        readonly int _maxGet;
        
        public MlDelivery(MailSave mailSave, Logger logger, MlUserList mlUserList, MlAddr mlAddr, MlMailDb mlDb, MlSubject mlSubject,List<string>docs,int maxGet) {
            _mlUserList = mlUserList;
            _mlAddr = mlAddr;
            _logger = logger;
            _mlDb = mlDb;
            _mlSubject = mlSubject;
            _docs = docs;
            _maxGet = maxGet;
            _mlSender = new MlSender(mailSave, logger);
        }

        //各種のドキュメントをname-admin@domainから送信者へ送る
        public bool Doc(MlDocKind mlDocKind, Mail orgMail, MlEnvelope mlEnvelope) {
            var mail = Fixed(mlDocKind);
            mail.ConvertHeader("from", _mlAddr.Admin.ToString());
            mail.ConvertHeader("to", ReturnTo(orgMail, mlEnvelope));//送信者をそのまま受信者にする
            return _mlSender.Send(mlEnvelope.Swap().ChangeFrom(_mlAddr.Admin), mail);
        }
        //エラーメールを返信する
        public bool Error(MlEnvelope mlEnvelope,string subject) {
            var mail = Create(ContentTyep.Ascii, subject,"");
            return _mlSender.Send(mlEnvelope.Swap().ChangeFrom(_mlAddr.Admin), mail);
        }
        //メンバー以外は投稿できません
        public bool Deny(Mail orgMail, MlEnvelope mlEnvelope) {
            //メール生成
            var subject = string.Format("You are not member ({0} ML)", _mlAddr.Name);
            var bodyStr = _mlAddr.Conv(_docs[(int)MlDocKind.Deny]);
            var mail = Create(ContentTyep.Sjis, subject, bodyStr);

            //宛先設定 from<->To from = mailDaemon
            mail.ConvertHeader("from", _mlAddr.Admin.ToString());
            mail.ConvertHeader("to", ReturnTo(orgMail, mlEnvelope));//送信者をそのまま受信者にする
            //配送
            return _mlSender.Send(mlEnvelope.Swap().ChangeFrom(_mlAddr.Admin), mail);
        }
        public bool Get(Mail mail, MlEnvelope mlEnvelope, MlParamSpan mlParamSpan) {
            return SpanFunc(Get1, mail, mlEnvelope, mlParamSpan);
        }
        public bool Summary(Mail mail, MlEnvelope mlEnvelope, MlParamSpan mlParamSpan) {
            return SpanFunc(Summary1, mail, mlEnvelope, mlParamSpan);
        }
        delegate Mail DelegateFunc(int s, int e);
        bool SpanFunc(DelegateFunc func, Mail orgMail, MlEnvelope mlEnvelope, MlParamSpan mlParamSpan) {
            var max = _maxGet;
            var s = mlParamSpan.Start;
            var e = mlParamSpan.Start + max - 1;
            bool finish = false;
            while (!finish) {
                if (e >= mlParamSpan.End) {
                    e = mlParamSpan.End;
                    finish = true;
                }
                var mail = func(s, e);
                mail.AddHeader("from", _mlAddr.Admin.ToString());
                mail.AddHeader("to", ReturnTo(orgMail, mlEnvelope));//送信者をそのまま受信者にする
                if (!_mlSender.Send(mlEnvelope.Swap().ChangeFrom(_mlAddr.Admin), mail)) {
                    return false;
                }
                s = e + 1;
                e = s + max - 1;
            }
            return true;
        }
        Mail Summary1(int start, int end) {
            //実際に取得したライブラリ番号
            var s = -1;
            var e = -1;

            Encoding encoding = null;
            var sb = new StringBuilder();

            //ライブラリからの取得
            for (int i = start; i <= end; i++) {
                var mail = _mlDb.Read(i);
                if (mail == null)
                    continue;
                if (s == -1) {
                    s = i;
                    e = i;
                } else {
                    e = i;
                }
                string str = mail.GetHeader("subject");
                if (str != null) {
                    str = Subject.Decode(ref encoding, str);
                    sb.Append(string.Format("{0}\r\n", str));
                }
            }
            var subject = string.Format("result for summary [{0}-{1}] ({2} ML)", s, e, _mlAddr.Name);
            if (s == -1) {//１件も取得できなかった場合
                subject = string.Format("not found No.{0} ({1} ML)", start, _mlAddr.Name);
            }
            return Create(ContentTyep.Sjis, subject, sb.ToString());
        }
        Mail Get1(int start, int end) {
            const string boundaryStr = "BJD-Boundary";
            var buf = new byte[0];
            //実際に取得したライブラリ番号
            var s = -1;
            var e = -1;

            //ライブラリからの取得
            for (var i = start; i <= end; i++) {
                var mail = _mlDb.Read(i);
                if (mail == null)
                    continue;
                if (s == -1) {
                    s = i;
                    e = i;
                } else {
                    e = i;
                }
                buf = Bytes.Create(buf, Encoding.ASCII.GetBytes(string.Format("--{0}\r\n", boundaryStr)));
                buf = Bytes.Create(buf, Encoding.ASCII.GetBytes("Content-Type: message/rfc822\r\n\r\n"));
                buf = Bytes.Create(buf, mail.GetBytes());
            }
            buf = Bytes.Create(buf, Encoding.ASCII.GetBytes(string.Format("--{0}--\r\n", boundaryStr)));
            var subject = string.Format("result for get [{0}-{1} MIME/multipart] ({2} ML)", s, e, _mlAddr.Name);
            if (s == -1) {//１件も取得できなかった場合
                subject = string.Format("not found No.{0} ({1} ML)", start, _mlAddr.Name);
            }
            var contentType = string.Format("multipart/mixed;\r\n boundary=\"{0}\"", boundaryStr);
            return Create(subject, contentType, buf);
        }

        //投稿
        public bool Post(Mail mail, MlEnvelope mlEnvelope) {

            //var no = mlDb.IncNo(mlAddr.Name);//インクリメントした記事番号の取得
            var incNo = _mlDb.Count() + 1;//インクリメントした記事番号の取得

            //記事メールの編集
            //Subject:の変更
            mail.ConvertHeader("subject", _mlSubject.Get(mail.GetHeader("subject"), incNo));
            //Reply-To:の追加　
            mail.ConvertHeader("Reply-To", string.Format("\"{0}\"<{1}>", _mlAddr.Name, _mlAddr.Post));
            //List-Id:の追加　
            mail.ConvertHeader("List-Id", string.Format("{0}.{1}", _mlAddr.Name, _mlAddr.DomainList[0]));
            //List-Software:の追加　
            mail.ConvertHeader("List-Software", string.Format("{0}", Define.ApplicationName()));
            //List-Post:の追加　
            mail.ConvertHeader("List-Post", string.Format("<mailto:{0}>", _mlAddr.Post));
            //List-Owner:の追加　
            mail.ConvertHeader("List-Owner", string.Format("<mailto:{0}>", _mlAddr.Admin));
            //List-Help:の追加　
            mail.ConvertHeader("List-Help", string.Format("<mailto:{0}?body=help>", _mlAddr.Ctrl));
            //List-Unsubscribe:の追加　
            mail.ConvertHeader("List-Unsubscribe", string.Format("<mailto:{0}?body=unsubscribe>", _mlAddr.Ctrl));

            //ライブラリへの保存
            _mlDb.Save(mail);

            //各メンバーへの配信
            foreach (MlOneUser to in _mlUserList) {
                if (to.Enable && to.IsReader) {//「配信する」のみが対象となる
                    if (!_mlSender.Send(mlEnvelope.ChangeTo(to.MailAddress), mail)) {
                        //配信に失敗したメールを管理者に転送する
                        var subject = string.Format("DELIVERY ERROR article to {0} ({1} ML)", to, _mlAddr.Name);
                        return AttachToAmdin(mail, subject, mlEnvelope);
                    }
                }
            }
            return true;
        }
        //元メールを添付して管理者へ送る
        public bool AttachToAmdin(Mail orgMail, string subject, MlEnvelope mlEnvelope) {
            //メール生成
            var mail = new Mail(_logger);
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            mail.AddHeader("subject", subject);
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

            //宛先設定 from<->To from = mailDaemon
            mail.ConvertHeader("from", _mlAddr.Admin.ToString());
            //配送
            return SendAllAdmin(mlEnvelope.ChangeFrom(_mlAddr.Admin), mail);
        }
        //管理者すべてに送信する
        public bool SendAllAdmin(MlEnvelope mlEnvelope, Mail mail) {
            foreach (MlOneUser to in _mlUserList) {
                if (!to.Enable || !to.IsManager)
                    continue; //管理者アドレス
                var mlenv = mlEnvelope.ChangeTo(to.MailAddress);//受信者を管理者に変更する
                var tmpMail = mail.CreateClone();
                tmpMail.ConvertHeader("to", to.MailAddress.ToString());
                if (!_mlSender.Send(mlenv, tmpMail)) {
                    return false;//失敗した場合は、全宛先に送信されない
                }
            }
            return true;
        }
        Mail Fixed(MlDocKind mlDocKind) {
            var subject = string.Format("{0} ({1} ML)", mlDocKind.ToString().ToLower(), _mlAddr.Name);
            var bodyStr = _mlAddr.Conv(_docs[(int)mlDocKind]);
            return Create(ContentTyep.Sjis, subject, bodyStr);
        }
        Mail Create(string subject, string contentType, byte[] body) {
            var mail = new Mail(_logger);
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            //ヘッダ作成
            mail.AddHeader("subject", subject);
            mail.AddHeader("Content-Type", contentType);
            //本文作成
            mail.Init(body);
            return mail;
        }
        Mail Create(ContentTyep type, string subject, string bodyStr) {
            var encoding = Encoding.ASCII;
            var contentType = "text/plain; charset==us-ascii";
            switch (type) {
                case ContentTyep.Ascii:
                    break;
                case ContentTyep.Sjis:
                    encoding = Encoding.GetEncoding("shift-jis");
                    contentType = string.Format("text/plain; charset={0}", encoding.HeaderName);
                    break;
            }
            return Create(subject, contentType, encoding.GetBytes(bodyStr));
        }
        //折り返しメールのためのToヘッダの作成
        string ReturnTo(Mail mail, MlEnvelope mlEnvelope) {
            //元メールのFromヘッダを取得する
            //無い場合は、mlEmvelope.Fromからセットする
            var to = mail.GetHeader("from") ?? mlEnvelope.From.ToString();
            return to;
        }
    }
}
