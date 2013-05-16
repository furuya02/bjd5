using Bjd;

namespace SmtpServer {
    class MlSender2 {
        readonly MailSave mailSave;
        readonly Logger logger;
        
        public MlSender2(MailSave mailSave,Logger logger) {
            this.mailSave = mailSave;
            this.logger = logger;
        }
        public bool Send(MlEnvelope mlEnvelope,Mail orgMail) {

            var mail = orgMail.CreateClone();//ヘッダを変更するためにテンポラリを作成する
            
            //MlCreatorで追加されない FromとToをここでセットする
            //var from = mail.GetHeader("from");
            //if (from==null || from.IndexOf(mlEnvelope.From.ToString()) == -1) {
            //    mail.ConvertHeader("from", mlEnvelope.From.ToString());
            //}
            //mail.ConvertHeader("from",mlEnvelope.From.ToString());
            //var to = mail.GetHeader("to");
            //if (to == null) {
            //    mail.AddHeader("to", mlEnvelope.To.ToString());
            //}
            
            if (mailSave.Save(mlEnvelope.From, mlEnvelope.To, mail, mlEnvelope.Host, mlEnvelope.Addr)) {
                logger.Set(LogKind.Detail, null, 38, string.Format("From:{0} To:{1}",mlEnvelope.From,mlEnvelope.To));
                return true;
            } else {
                logger.Set(LogKind.Error, null, 39, string.Format("From:{0} To:{1}", mlEnvelope.From, mlEnvelope.To));
                return true;
            }
        }
    }
}
