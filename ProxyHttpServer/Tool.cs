using System;
using Bjd;
using Bjd.tool;

namespace ProxyHttpServer {
    public class Tool : OneTool {
        public override string JpMenu { get { return "キャッシュ一覧"; } }
        public override string EnMenu { get { return "Cache Database"; } }
        public override char Mnemonic { get { return 'C'; } }

        public Tool(Kernel kernel, string nameTag)
            : base(kernel, nameTag) {

        }


        override public ToolDlg CreateDlg(Object obj) {
            return new Dlg(Kernel, NameTag, obj, (Kernel.IsJp()) ? "キャッシュ一覧" : "Cache Database");
        }

    }
}

