using Bjd;
using Bjd.mail;

namespace SmtpServer {
    //１人の参加者
    public class MlOneUser {
        public bool Enable { get; private set; }//有効無効
        public string Name { get; private set; }//名前
        public MailAddress MailAddress { get; private set; }//メールアドレス
        public bool IsManager { get; private set; }//管理者
        public bool IsReader { get; private set; }//読者
        public bool IsContributor { get; private set; }//投稿者
        public string Psssword { get; private set; }//パスワード

        public MlOneUser(bool enable, string name, MailAddress mailAddress, bool isManager, bool isReader, bool isContributor, string password) {
            Enable = enable;
            Name = name;
            MailAddress = mailAddress;
            IsManager = isManager;
            IsReader = isReader;
            IsContributor = isContributor;
            Psssword = password;
        }
    }
}