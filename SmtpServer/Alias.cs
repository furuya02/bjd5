using System;
using System.Collections.Generic;
using System.Text;

using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.option;

namespace SmtpServer {

    class Alias {
         readonly Logger _logger;

        readonly Dictionary<String, String> _ar = new Dictionary<string, string>();
        readonly List<string> _domainList;

        public Alias(Kernel kernel, Conf conf, Logger logger, List<string> domainList) {
            _logger = logger;
            _domainList = domainList;


            var dat = (Dat)conf.Get("aliasList");
            if (dat == null)
                return;
            foreach (var o in dat){
                if (o.Enable){
                    //有効なデータだけを対象にする
                    string name = o.StrList[0];
                    string alias = o.StrList[1];

                    //aliasの文字列に矛盾がないかどうかを確認する
                    string[] tmp = alias.Split(',');
                    var sb = new StringBuilder();
                    foreach (string str in tmp){
                        if (str.IndexOf('@') != -1){
                            //グローバルアドレスの追加
                            sb.Append(str);
                            sb.Append(',');
                        } else if (str.IndexOf('/') == 0){
                            //ローカルファイルの場合
                            sb.Append(str);
                            sb.Append(',');
                        } else if (str.IndexOf('$') == 0){
                            //定義の場合
                            if (str == "$ALL"){
                                foreach (string user in kernel.MailBox.UserList){
                                    sb.Append(string.Format("{0}@{1}", user, domainList[0]));
                                    sb.Append(',');
                                }
                            } else if (str == "$USER"){
                                //Ver5.4.3 $USER追加
                                sb.Append(string.Format("{0}@{1}", name, domainList[0]));
                                sb.Append(',');
                            } else{
                                logger.Set(LogKind.Error, null, 45, string.Format("name:{0} alias:{1}", name, alias));
                            }
                        } else{
                            if (!kernel.MailBox.IsUser(str)){
                                //ユーザ名は有効か？
                                logger.Set(LogKind.Error, null, 19, string.Format("name:{0} alias:{1}", name, alias));
                            } else{
                                sb.Append(string.Format("{0}@{1}", str, domainList[0]));
                                sb.Append(',');
                            }
                        }
                    }
                    string buffer;
                    if (_ar.TryGetValue(name, out buffer)){
                        logger.Set(LogKind.Error, null, 30, string.Format("user:{0} alias:{1}", name, alias));
                    } else{
                        _ar.Add(name, sb.ToString());
                    }
                }
            }
        }
        public bool IsUser(string user) {
            string buffer;
            return _ar.TryGetValue(user,out buffer);
        }
        public RcptList Reflection(RcptList rcptList) {
            var ret = new RcptList();
            foreach(MailAddress mailAddress in rcptList){

                string buffer;
                if (mailAddress.IsLocal(_domainList) && _ar.TryGetValue(mailAddress.User, out buffer)) {
                    string[] lines = buffer.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string line in lines) {
                        _logger.Set(LogKind.Normal,null,27,string.Format("{0} -> {1}",mailAddress,line));
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
