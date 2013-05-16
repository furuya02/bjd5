using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bjd.mail {
    //**********************************************************************************
    //メールアドレスを表現（保持）するクラス
    // /で始まる場合は、ローカルのファイル名を表現する
    //**********************************************************************************
    public class MailAddress {
        public MailAddress(string user, string domain) {
            User = user;
            Domain = domain;
        }

        //****************************************************************
        //プロパティ
        //****************************************************************
        public string User { get; private set; }
        public string Domain { get; private set; }
        //「ユーザ@ドメイン」の形式で初期化する
        public MailAddress(string str) {

            User = "";
            Domain = "";
            if (str == null)
                return;

            //Ver5.6.0 \b対応
            if (str.IndexOf('\b') != -1) {
                var sb = new StringBuilder();
                foreach (char t in str){
                    if (t == '\b') {
                        sb.Remove(sb.Length - 1, 1);
                    } else {
                        sb.Append(t);
                    }
                }
                str = sb.ToString();
            }

            var buf = Extraction(str);

            var tmp = buf.Split('@');
            switch (tmp.Length){
                case 1:
                    User = tmp[0];
                    break;
                case 2:
                    User = tmp[0];
                    Domain = tmp[1];
                    break;
            }
        }

        override public string ToString() {
            //Ver5.0.2 User==""の時、例外が発生するバグを修正
            //if (User[0] == '/')
            if (User.Length > 0 && User[0] == '/')
                return User.Substring(1);
            return User + "@" + Domain;
        }

        public bool IsLocal(List<string> domainList){
            return User[0] == '/' || domainList.Any(s => s.ToUpper() == Domain.ToUpper());
        }

        public bool IsFile(){
            //ローカルファイルへの出力かどうかを判断する
            return User[0] == '/';
        }

        public bool Compare(MailAddress mailAddress) {
            if (Domain.ToUpper() != mailAddress.Domain.ToUpper())
                return false;
            return User == mailAddress.User;
        }

        //抽出 <>で括られたり、""でコメントが入っている文字列からメールアドレスを抜き出す
        string Extraction(string str) {
            var index = str.IndexOf('<');
            if (0 <= index)// < > で表記されている場合
                str = str.Substring(index + 1);
            index = str.IndexOf('<');
            if (0 <= index) //<がネストされている場合
                str = str.Substring(index + 1);

            var buf = str;

            var esc = false;
            for (var i = 0; i < str.Length; i++) {
                var c = str[i];
                if (esc) {
                    if (c == '"') {
                        esc = false;
                        continue;
                    }
                } else {
                    if (c == '"') {
                        esc = true;
                        continue;
                    }
                }
                if (c == '>' || c == '(' || c == ' ' || c == '\t') {
                    buf = str.Substring(0, i);
                    //Ver5.0.2
                    break;
                }
            }
            return buf;
        }
    }
}
