using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.mail;

namespace SmtpServer {
    class CheckParam {
        public String Message { get; private set; }
        private readonly bool _useNullFrom;
        private readonly bool _useNullDomain;
        public CheckParam(bool useNullFrom, bool useNullDomain) {
            Message = "";
            //空白のFROM(MAIN From:<>)を許可するかどうかをチェックする
            _useNullFrom = useNullFrom;
            //ドメイン名の無いFROMを許可するかどうかのチェック
            _useNullDomain = useNullDomain;
        }

        public bool Rcpt(List<String> paramList){
            if (paramList.Count < 1) {
                Message = "501 Syntax error in parameters scanning \"\"";
                return false;
            }
            //RCPT の後ろが　FROM:メールアドレスになっているかどうかを確認する
            if (paramList[0].ToUpper() != "TO:") {
                Message = string.Format("501 5.5.2 Syntax error in parameters scanning {0}",paramList[0]);
                return false;
            }
            if (paramList.Count < 2) {
                Message = "501 Syntax error in parameters scanning \"\"";
                return false;
            }
            if (0 <= paramList[1].IndexOf('!')) {
                Message = string.Format("553 5.3.0 {0}... UUCP addressing is not supported", paramList[1]);
                //Logger.Set(LogKind.Secure, sockTcp, 18, s);
                return false;
            }
            //Ver5.6.0 \bをエラーではじく
            if (paramList[1].IndexOf('\b') != -1) {
                Message = "501 Syntax error in parameters scanning \"From\"";
                return false;
            }
            var mailAddress = new MailAddress(paramList[1]);
            
            if (mailAddress.User == "") {
                Message = "501 Syntax error in parameters scanning \"MailAddress\"";
                return false;
            }
            Message = "";
            return true;
        }

        public bool Mail(List<String>paramList ){
            if (paramList.Count < 1) {
                Message = "501 Syntax error in parameters scanning \"\"";
                return false;
            }
            if (paramList[0].ToUpper() != "FROM:") {
                Message = string.Format("501 5.5.2 Syntax error in parameters scanning {0}", paramList[0]);
                return false;
            }
            if (paramList.Count < 2) {
                Message = "501 Syntax error in parameters scanning \"\"";
                return false;
            }
            //\bをエラーではじく
            if (paramList[1].IndexOf('\b') != -1) {
                Message = "501 Syntax error in parameters scanning \"From\"";
                return false;
            }

            var mailAddress = new MailAddress(paramList[1]);

            if (mailAddress.User == "" && mailAddress.Domain == "") {
                //空白のFROM(MAIN From:<>)を許可するかどうかをチェックする
                if (!_useNullFrom) {
                    Message = "501 Syntax error in parameters scanning \"From\"";
                    return false;
                }
            } else {
                if (mailAddress.User == "") {
                    Message = "501 Syntax error in parameters scanning \"MailAddress\"";
                    return false;
                }
                //ドメイン名の無いFROMを許可するかどうかのチェック
                if (!_useNullDomain && mailAddress.Domain == "") {
                    Message = string.Format("553 {0}... Domain part missing", paramList[1]);
                    return false;
                }
            }
            Message = "";
            return true;
        }
    }
}
