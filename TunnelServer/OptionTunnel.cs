using System.Collections.Generic;

using Bjd;
using Bjd.ctrl;
using Bjd.option;

namespace TunnelServer {
    internal class OptionTunnel : OneOption {

        //public override string JpMenu { get { return "トンネルの追加と削除"; } }
        //public override string EnMenu { get { return "Add or Remove Tunnel"; } }
        public override char Mnemonic { get { return 'A'; } }

        public OptionTunnel(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            var pageList = new List<OnePage>();
            var key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key), kernel));
            //pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }
        
        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            var l = new ListVal();
            var key = "protocol";
            l.Add(new OneVal(key, 0, Crlf.Nextline, new CtrlComboBox(Lang.Value(key), new[] { "TCP", "UDP" }, 100)));
            key = "srcPort";
            l.Add(new OneVal(key, 0, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "server";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "dstPort";
            l.Add(new OneVal(key, 0, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            onePage.Add(new OneVal("tunnelList", null, Crlf.Nextline, new CtrlDat("", l, 380, IsJp())));

            return onePage;
        }

        //コントロールの変化
        override public void OnChange() {

        }
    }
}
