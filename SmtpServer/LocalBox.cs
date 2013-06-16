using System;
using Bjd.log;
using Bjd.mail;

namespace SmtpServer {
    //メールをファイルに追加する
    class LocalBox{
        private readonly Logger _logger;
        public LocalBox(Logger logger){
            _logger = logger;

        }
        public bool Save(MailAddress to,Mail mail,MailInfo mailInfo){
            if (mail.Append(to.ToString())) {
                if (_logger != null){
                    _logger.Set(LogKind.Normal, null, 21, string.Format("[{0}] {1}", to.User, mailInfo));
                }
            } else {
                _logger.Set(LogKind.Error, null, 9000059, mail.GetLastError());
                _logger.Set(LogKind.Error, null, 22, string.Format("[{0}] {1}", to.User, mailInfo));
            }
            return true;
        }
    }
}
