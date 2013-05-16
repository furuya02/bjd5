
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;
using System.Collections.Generic;

namespace DhcpServer {
    class Option : OneOption {
        public override string JpMenu { get { return "DHCPサーバ"; } }
        public override string EnMenu { get { return "DHCP Server"; } }

        public override char Mnemonic{ get { return  'H'; }}
       
        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox((IsJp()) ? "DHCPサーバを使用する" : "Use DHCP Server")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(Page2("Acl","ACL(MAC)", kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Udp, 67, 10, 10)); //サーバ基本設定


            onePage.Add(new OneVal("leaseTime", 18000, Crlf.Nextline,new CtrlInt(IsJp() ? "リース時間(秒)" : "Lease Time(sec)", 8)));
            onePage.Add(new OneVal("startIp", new Ip(IpKind.V4_0), Crlf.Nextline,new CtrlAddress(IsJp() ? "開始アドレス  　 " : "Start Address")));
            onePage.Add(new OneVal("endIp", new Ip(IpKind.V4_0), Crlf.Nextline,new CtrlAddress(IsJp() ? "終了アドレス 　  " : "End Address")));
            onePage.Add(new OneVal("maskIp", new Ip("255.255.255.0"), Crlf.Nextline,new CtrlAddress(IsJp() ? "サブネットマスク  " : "Subnet Mask")));
            onePage.Add(new OneVal("gwIp", new Ip(IpKind.V4_0), Crlf.Nextline,new CtrlAddress(IsJp() ? "ゲートウエイ    " : "Gateway")));
            onePage.Add(new OneVal("dnsIp0", new Ip(IpKind.V4_0), Crlf.Nextline,new CtrlAddress(IsJp() ? "DNS（プライマリ)" : "DNS(Primary)")));
            onePage.Add(new OneVal("dnsIp1", new Ip(IpKind.V4_0), Crlf.Nextline,new CtrlAddress(IsJp() ? "DNS（セカンダリ)" : "DNS(Secondary)")));
            onePage.Add(new OneVal("useWpad", false, Crlf.Contonie,new CtrlCheckBox(IsJp()
                                                       ? "WPAD(Web Proxy Auto-Discovery Protocol)を使用する"
                                                       : "use WPAD(Web Proxy Auto-Discovery Protocol)")));
            onePage.Add(new OneVal("wpadUrl", "http://", Crlf.Nextline, new CtrlTextBox("URL", 37)));

            return onePage;
        }

        private OnePage Page2(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

                onePage.Add(new OneVal("useMacAcl", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "MACアドレスによる制限（有効にすると登録したMACアドレスのみ使用可能になります）" : "limit by a MAC address (it is OK only the person who registered itself)")));

                var l = new ListVal();
                l.Add(new OneVal("macAddress", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "MACアドレス(99-99-99-99-99-99)" : "MAC Address(99-99-99-99-99-99)", 50)));
                l.Add(new OneVal("v4Address", new Ip(IpKind.V4_0), Crlf.Nextline, new CtrlAddress(IsJp() ? "IPアドレス" : "IP Address")));
                l.Add(new OneVal("macName", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "名前（表示名）" : "Name(Display)", 50)));
                onePage.Add(new OneVal("macAcl", null, Crlf.Nextline, new CtrlDat(IsJp() ? "IPアドレスに「255.255.255.255」指定した場合、基本設定で指定した範囲からランダムに割り当てられます" : "When appointed 255.255.255.255 to IP Address, basic setting is used", l, 250, IsJp())));

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
