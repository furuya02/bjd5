using Bjd.log;
using Bjd.mail;

namespace SmtpServer {
    class MlSender {
        readonly MailSave _mailSave;
        readonly Logger _logger;

        public MlSender(MailSave mailSave, Logger logger) {
            _mailSave = mailSave;
            _logger = logger;
        }

        public bool Send(MlEnvelope mlEnvelope, Mail orgMail) {
            var mail = orgMail.CreateClone();//�w�b�_��ύX���邽�߂Ƀe���|������쐬����

            if (_mailSave.Save(mlEnvelope.From, mlEnvelope.To, mail, mlEnvelope.Host, mlEnvelope.Addr)) {
                _logger.Set(LogKind.Detail, null, 38, string.Format("From:{0} To:{1}", mlEnvelope.From, mlEnvelope.To));
                return true;
            }
            _logger.Set(LogKind.Error, null, 39, string.Format("From:{0} To:{1}", mlEnvelope.From, mlEnvelope.To));
            return true;
        }
    }
}