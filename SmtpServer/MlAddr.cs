using System.Collections.Generic;
using Bjd;
using Bjd.mail;
using Bjd.util;

namespace SmtpServer {
    class MlAddr {
        public List<string> DomainList { get; private set; }
        public string Name{ get; private set; }
        public MlAddr(string name, List<string> domainList) {
            Name = name;
            DomainList = domainList;
        }
        public MailAddress Admin {
            get {
                return new MailAddress(Name + "-admin", DomainList[0]);//管理者アドレス
            }
        }
        public MailAddress Ctrl {
            get {
                return new MailAddress(Name + "-ctl", DomainList[0]);//制御アドレス
            }
        }
        public MailAddress Post {
            get {
                return new MailAddress(Name, DomainList[0]);//投稿アドレス
            }
        }

        //有効なあて先かどうかの確認
        public bool IsUser(MailAddress mailAddress){
            //代表アドレスの種類判定で無効な場合は、有効な宛先ではない
            return GetKind(mailAddress) != MlAddrKind.None;
        }

        //代表アドレスの種類判定
        public MlAddrKind GetKind(MailAddress mailAddress) {
            if (mailAddress.IsLocal(DomainList)) {
                //「投稿アドレス」
                if (mailAddress.User.ToUpper() == Name.ToUpper())
                    return MlAddrKind.Post;
                //「制御アドレス」
                if (mailAddress.User.ToUpper() == (Name + "-ctl").ToUpper())
                    return MlAddrKind.Ctrl;
                //「管理者アドレス」
                if (mailAddress.User.ToUpper() == (Name + "-admin").ToUpper())
                    return MlAddrKind.Admin;
            }
            return MlAddrKind.None;//無効
        }

        public string Conv(string str){
            str = Util.SwapStr("$ML_NAME", Name, str);
            str = Util.SwapStr("$POST_ADDR", Post.ToString(), str);
            str = Util.SwapStr("$CTRL_ADDR", Ctrl.ToString(), str);
            str = Util.SwapStr("$ADMIN_ADDR", Admin.ToString(), str);
            return str;
        }

    }
}

