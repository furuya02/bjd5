
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;
using System.Collections.Generic;

namespace DhcpServer {
    class Option : OneOption {
        public override char Mnemonic{ get { return  'H'; }}
       
        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

                var key = "useServer";
            Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox((Lang.Value(key)))));

            var pageList = new List<OnePage>();
            key = "Basic";
            pageList.Add(Page1(key,Lang.Value(key), kernel));
            pageList.Add(Page2("Acl","ACL(MAC)", kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Udp, 67, 10, 10)); //サーバ基本設定

            var key = "leaseTime";
            onePage.Add(new OneVal(key, 18000, Crlf.Nextline, new CtrlInt(Lang.Value(key), 8)));
            key = "startIp";
            onePage.Add(new OneVal(key, new Ip(IpKind.V4_0), Crlf.Nextline, new CtrlAddress(Lang.Value(key))));
            key = "endIp";
            onePage.Add(new OneVal(key, new Ip(IpKind.V4_0), Crlf.Nextline, new CtrlAddress(Lang.Value(key))));
            key = "maskIp";
            onePage.Add(new OneVal(key, new Ip("255.255.255.0"), Crlf.Nextline, new CtrlAddress(Lang.Value(key))));
            key = "gwIp";
            onePage.Add(new OneVal(key, new Ip(IpKind.V4_0), Crlf.Nextline, new CtrlAddress(Lang.Value(key))));
            key = "dnsIp0";
            onePage.Add(new OneVal(key, new Ip(IpKind.V4_0), Crlf.Nextline, new CtrlAddress(Lang.Value(key))));
            key = "dnsIp1";
            onePage.Add(new OneVal(key, new Ip(IpKind.V4_0), Crlf.Nextline, new CtrlAddress(Lang.Value(key))));
            key = "useWpad";
            onePage.Add(new OneVal(key, false, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
            onePage.Add(new OneVal("wpadUrl", "http://", Crlf.Nextline, new CtrlTextBox("URL", 37)));

            return onePage;
        }

        private OnePage Page2(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            var key = "useMacAcl";
            onePage.Add(new OneVal(key, false, Crlf.Nextline,new CtrlCheckBox(Lang.Value(key))));

            var l = new ListVal();
            key = "macAddress";
            l.Add(new OneVal(key, "", Crlf.Nextline,new CtrlTextBox(Lang.Value(key), 50)));
            key = "v4Address";
            l.Add(new OneVal(key, new Ip(IpKind.V4_0), Crlf.Nextline, new CtrlAddress(Lang.Value(key))));
            key = "macName";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
            key = "macAcl";
            onePage.Add(new OneVal(key, null, Crlf.Nextline,new CtrlDat(Lang.Value(key), l, 250, IsJp())));

            return onePage;
        }

        //コントロールの変化
        override public void OnChange() {
            // ポート番号変更禁止
            GetCtrl("port").SetEnable(false);

            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);
            b = (bool)GetCtrl("useWpad").Read();
            GetCtrl("wpadUrl").SetEnable(b);

        }
    }

}
