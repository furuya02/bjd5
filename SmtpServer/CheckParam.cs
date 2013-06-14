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

        public bool Mail(List<String>paramList ){
            if (paramList.Count < 2) {
                Message = "501 Syntax error in parameters scanning \"\"";
                return false;
            }
            if (paramList[0].ToUpper() != "FROM") {
                Message = "501 Syntax error in parameters scanning \"MAIL\"";
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



            return true;
        }
    }
}
