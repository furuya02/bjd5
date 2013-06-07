using System;
using Bjd;
using Bjd.mail;
using Bjd.util;

namespace SmtpServer {
    class OneSubscribe {
        public string Name { get; private set; }//ユーザ名
        public MailAddress MailAddress { get; private set; }//申請メールアドレス
        public string ConfirmStr { get; private set; }//認証ワード
        public DateTime Dt { get; private set; }
        public OneSubscribe(MailAddress mailAddress, string name, string confirmStr) {
            Dt = DateTime.Now;
            MailAddress = mailAddress;
            Name = name;
            ConfirmStr = confirmStr;
        }

        public bool FromString(string str) {
            str = Inet.TrimCrlf(str);//\r\nの排除
            var tmp = str.Split('\t');
            if (tmp.Length == 4) {
                try {
                    var ticks = Convert.ToInt64(tmp[0]);
                    Dt = new DateTime(ticks);
                    MailAddress = new MailAddress(tmp[1]);
                    Name = tmp[2];
                    ConfirmStr = tmp[3];
                    return true;//初期化成功
                } catch (Exception){
                }
            }
            return false;
        }

        override public string ToString() {
            return string.Format("{0}\t{1}\t{2}\t{3}", Dt.Ticks, MailAddress, Name, ConfirmStr);
        }
    }
}
