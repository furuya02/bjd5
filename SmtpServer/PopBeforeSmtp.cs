using System;
using Bjd.mail;
using Bjd.net;

namespace SmtpServer {
    class PopBeforeSmtp{
        private readonly bool _usePopBeforeSmtp;
        private readonly int _timePopBeforeSmtp;
        private readonly MailBox _mailBox;

        public PopBeforeSmtp(bool usePopBeforeSmtp, int timePopBeforeSmtp,MailBox mailBox) {
            _usePopBeforeSmtp = usePopBeforeSmtp;
            _timePopBeforeSmtp = timePopBeforeSmtp;
            _mailBox = mailBox;

        }

        //認証されているかどうかのチェック
        public bool Auth(Ip addr) {
            if (_usePopBeforeSmtp) {
                var span = DateTime.Now - _mailBox.LastLogin(addr);//最終ログイン時刻からの経過時間を取得
                //var sec = (int)span.TotalSeconds;//経過秒
                var sec = span.TotalSeconds;//経過秒
                if (0 < sec && sec < _timePopBeforeSmtp) {
                    return true;//認証されている
                }
            }
            return false;
        }
    }
}
