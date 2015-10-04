using System;

namespace Bjd.tool {
    public class Tool : OneTool {
        public Tool(Kernel kernel, string nameTag)
            : base(kernel, nameTag) {
        }
        public override string JpMenu { get { return "ステータス表示"; } }
        public override string EnMenu { get { return "Status"; } }
        public override char Mnemonic { get { return 'U'; } }

        override public ToolDlg CreateDlg(Object obj) {
            return new Dlg(Kernel, NameTag, obj, (Kernel.IsJp()) ? "ステータス表示" : "Status");
        }
    }
}

