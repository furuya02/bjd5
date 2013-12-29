using System;
using System.Collections.Generic;
using System.Collections;
using Bjd.log;
using Bjd.mail;
using Bjd.util;

namespace SmtpServer {

    class MlCmd : IEnumerable {
        readonly List<OneMlCmd> _ar = new List<OneMlCmd>();
        public MlCmd(Logger logger, Mail mail, MlOneUser mlOneUser) {
            //this.logger = logger;
            var lines = Inet.GetLines(mail.GetBody());
            foreach (var line in lines) {
                var str = mail.GetEncoding().GetString(line);
                str = Inet.TrimCrlf(str);
                //Ver5.6.4 前後の空白を除去する
                str = str.Trim();
                if (str == "")//空白行は無視する
                    continue;
                if (!SetCmd(str, mlOneUser)) {
                    logger.Set(LogKind.Error, null, 40, str);//解釈失敗
                }
            }
        }
        bool SetCmd(string str, MlOneUser mlOneUser) {
            foreach (MlCmdKind cmdKind in Enum.GetValues(typeof(MlCmdKind))) {
                //Ver6.0.1 コマンドは行頭から始まっているもの以外は受け付けない
                var cmdStr = str.ToUpper().Trim();
//                if (str.ToUpper().IndexOf(cmdKind.ToString().ToUpper()) >= 0) {
                if (cmdStr.IndexOf(cmdKind.ToString().ToUpper()) == 0) {
                    var param = "";
                    var tmp = str.Split(new[] { ' ' }, 2);
                    if (tmp.Length == 2)
                        param = tmp[1];
                    _ar.Add(new OneMlCmd(cmdKind, param, mlOneUser));
                    return true;
                }
            }
            return false;
        }
        //イテレータ
        public IEnumerator GetEnumerator(){
            return _ar.GetEnumerator();
        }
    }
}
