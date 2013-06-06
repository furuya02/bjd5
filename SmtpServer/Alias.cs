using System;
using System.Collections.Generic;
using System.Text;
using Bjd.log;
using Bjd.mail;

namespace SmtpServer{
    internal class Alias{

        private readonly Dictionary<String, String> _ar = new Dictionary<string, string>();
        private readonly List<string> _domainList;
        private readonly MailBox _mailBox;

        public Alias(List<string> domainList, MailBox mailBox){
            _domainList = domainList;
            _mailBox = mailBox;
            if (domainList == null || domainList.Count < 1){
                throw new Exception("Alias.cs Alias() domainList.Count<1");
            }
        }

        //テスト用 loggerはnullでも可
        public void Add(String name, String alias, Logger logger){
            //aliasの文字列に矛盾がないかどうかを確認する
            var tmp = alias.Split(',');
            var sb = new StringBuilder();
            foreach (var str in tmp){
                if (str.IndexOf('@') != -1){
                    //グローバルアドレスの追加
                    sb.Append(str);
                    sb.Append(',');
                }else if (str.IndexOf('/') == 0){
                    //ローカルファイルの場合
                    sb.Append(str);
                    sb.Append(',');
                }else if (str.IndexOf('$') == 0){
                    //定義の場合
                    if (str == "$ALL"){
                        if (_mailBox != null){
                            foreach (string user in _mailBox.UserList) {
                                sb.Append(string.Format("{0}@{1}", user, _domainList[0]));
                                sb.Append(',');
                            }
                        }
                    }else if (str == "$USER"){
                        //Ver5.4.3 $USER追加
                        sb.Append(string.Format("{0}@{1}", name, _domainList[0]));
                        sb.Append(',');
                    }else{
                        if (logger != null){
                            logger.Set(LogKind.Error, null, 45, string.Format("name:{0} alias:{1}", name, alias));
                        }
                    }
                }else{
                    if (_mailBox==null || !_mailBox.IsUser(str)){
                        //ユーザ名は有効か？
                        if (logger != null){
                            logger.Set(LogKind.Error, null, 19, string.Format("name:{0} alias:{1}", name, alias));
                        }
                    }else{
                        sb.Append(string.Format("{0}@{1}", str, _domainList[0]));
                        sb.Append(',');
                    }
                }
            }
            string buffer;
            if (_ar.TryGetValue(name, out buffer)){
                if (logger != null){
                    logger.Set(LogKind.Error, null, 30, string.Format("user:{0} alias:{1}", name, alias));
                }
            }else{
                _ar.Add(name, sb.ToString());
            }
        }

        //設定されているユーザ名かどうか
        public bool IsUser(string user) {
            string buffer;
            return _ar.TryGetValue(user, out buffer);
        }


        //宛先リストの変換
        //テスト用 loggerはnullでも可
        public RcptList Reflection(RcptList rcptList, Logger logger) {
            var ret = new RcptList();
            foreach(var mailAddress in rcptList){

                string buffer;
                if (mailAddress.IsLocal(_domainList) && _ar.TryGetValue(mailAddress.User, out buffer)) {
                    var lines = buffer.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(var line in lines) {
                        if (logger != null){
                            logger.Set(LogKind.Normal, null, 27, string.Format("{0} -> {1}", mailAddress, line));
                        }
                        ret.Add(new MailAddress(line));
                    }
                }else{
                    ret.Add(mailAddress);
                }
            }
            return ret;
        }
    }
}
