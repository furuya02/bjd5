using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.server;

namespace SmtpServer {
    public enum SmtpCmdKind {
        Quit,
        Noop,
        Helo,
        Ehlo,
        Mail,
        Rcpt,
        Rset,
        Data,
        //Auth,
        Unknown
    }
    public class SmtpCmd{
        
        public List<string> ParamList { get; private set; }
        public String Str { get; private set; }
        public SmtpCmdKind Kind { get; private set; }

        public SmtpCmd(Cmd cmd){

            Str = cmd.Str;

            //パラメータ分離
            ParamList = new List<string>();
            if (cmd.ParamStr != null) {
                foreach (var s in cmd.ParamStr.Split(new char[2] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries)) {
                    ParamList.Add(s.Trim(' '));
                }
            }

            //コマンド文字列の解釈
            Kind = SmtpCmdKind.Unknown;
            foreach (SmtpCmdKind n in Enum.GetValues(typeof(SmtpCmdKind))) {
                if (n.ToString().ToUpper() == cmd.CmdStr.ToUpper()) {
                    Kind = n;
                    break;
                }
            }
        }
    }
}
