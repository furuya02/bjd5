using System.Collections.Generic;

using Bjd;
using Bjd.ctrl;
using Bjd.option;

namespace TunnelServer {
    internal class OptionTunnel : OneOption {

        public override string JpMenu { get { return "トンネルの追加と削除"; } }
        public override string EnMenu { get { return "Add or Remove Tunnel"; } }
        public override char Mnemonic { get { return 'A'; } }

        public OptionTunnel(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic",kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }
        
        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            var l = new ListVal();
            l.Add(new OneVal("protocol", 0, Crlf.Nextline, new CtrlComboBox(IsJp() ? "プロトコル" : "Protocol", new[] { "TCP", "UDP" }, 100)));
            l.Add(new OneVal("srcPort", 0, Crlf.Nextline, new CtrlInt(IsJp() ? "クライアントから見たポート" : "Port (from client side)", 5)));
            l.Add(new OneVal("server", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "接続先サーバ名" : "Connection ahead server", 30)));
            l.Add(new OneVal("dstPort", 0, Crlf.Nextline, new CtrlInt(IsJp() ? "接続先ポート" : "Port (to server side)", 5)));
            onePage.Add(new OneVal("tunnelList", null, Crlf.Nextline, new CtrlDat("", l, 380, IsJp())));

            return onePage;
        }

        //コントロールの変化
        override public void OnChange() {

        }
    }
}
