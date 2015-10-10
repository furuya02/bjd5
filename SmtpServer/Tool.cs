using System;
using Bjd;
using Bjd.tool;

namespace SmtpServer {
    public class Tool : OneTool {
        public Tool(Kernel kernel, string nameTag)
            : base(kernel, nameTag) {

        }
        public override string JpMenu { get { return "[SMTP] メールボックス（メールキュー）"; } }
        public override string EnMenu { get { return "[SMTP] MainBox(Queue)"; } }
        public override char Mnemonic { get { return 'B'; } }

        override public ToolDlg CreateDlg(Object obj) {
            return new Dlg(Kernel, NameTag, obj, (Kernel.IsJp()) ? "メールボックス（メールキュー）" : "MailBox");
        }

    }
}


