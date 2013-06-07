using System.Collections.Generic;
using System.Linq;
using Bjd.mail;
using Bjd.option;
using Bjd.util;

namespace SmtpServer {
    class SmtpAuthUserList {
        //認証情報（どちらかが有効になる）
        readonly MailBox _mailBox = null;
        readonly List<SmtpAuthOneUser> _ar = new List<SmtpAuthOneUser>();
        
        //usePopAcountがfalseの時、内部で強制的にmailBoxが無効化される
        //usePopAcountがtrueの時、内部で強制的にesmtpUserListが無効化される
        public SmtpAuthUserList(bool usePopAccount,MailBox mailBox, IEnumerable<OneDat> esmtpUserList){
            
            _mailBox = mailBox;
            
            if (esmtpUserList != null){
                foreach (var o in esmtpUserList){
                    if (!o.Enable){
                        continue;
                    }
                    var user = o.StrList[0];
                    var pass = o.StrList[1];
                    pass = Crypt.Decrypt(pass);
                    _ar.Add(new SmtpAuthOneUser(user, pass));
                }
            }
            
            if (!usePopAccount){
                //POPアカウントを使用しない場合
                _mailBox = null; //強制的にmailBoxを無効化する
            }
        }
        public string GetPass(string user) {
            if (_mailBox != null) {
                return _mailBox.GetPass(user);
            }
            foreach (var o in _ar){
                if (o.User == user){
                    return o.Pass;
                }
            }
            return null;
        }

        public bool Auth(string user, string pass) {
            if (_mailBox != null) {
                return _mailBox.Auth(user, pass);
            }
            foreach (var o in _ar) {
                if (o.User == user) {
                    if (o.Pass == pass) {
                        return true;
                    }
                    break;
                }
            }
            return false;
        }


        class SmtpAuthOneUser {
            public SmtpAuthOneUser(string user, string pass) {
                User = user;
                Pass = pass;
            }
            public string User { get; private set; }
            public string Pass { get; private set; }

        }
    }
}
