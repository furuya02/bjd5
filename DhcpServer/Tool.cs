using System;

using Bjd;
using Bjd.tool;

namespace DhcpServer {
    public class Tool : OneTool {
        public Tool(Kernel kernel, string nameTag)
            : base(kernel, nameTag) {

        }
        public override string JpMenu { get { return "���[�X�ꗗ"; } }
        public override string EnMenu { get { return "Lease Database"; } }

        public override char Mnemonic{ get { return 'L'; }
        }

        override public ToolDlg CreateDlg(Object obj) {
            return new Dlg(Kernel, NameTag, obj, (Kernel.IsJp()) ? "���[�X�ꗗ" : "Lease Database");
        }
    }
}