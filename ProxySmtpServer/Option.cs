using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace ProxySmtpServer {
    class Option : OneOption {

        public override string JpMenu { get { return "SMTP"; } }
        public override string EnMenu { get { return "SMTP"; } }
        public override char Mnemonic { get { return 'S'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "SMTPプロキシを使用する" : "Use SMTP Proxy")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(Page2("Expansion", IsJp() ? "拡張設定" : "Expansion", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 8025, 60, 10)); //サーバ基本設定

            onePage.Add(new OneVal("targetPort", 25, Crlf.Nextline, new CtrlInt(IsJp() ? "接続先ポート" : "Port", 5)));
            onePage.Add(new OneVal("targetServer", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "接続先サーバ" : "Server", 50)));
            onePage.Add(new OneVal("idleTime", 1, Crlf.Nextline, new CtrlInt(IsJp() ? "アイドルタイム(m)" : "Idle time (m)", 5)));

            return onePage;
        }
        private OnePage Page2(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            l.Add(new OneVal("mail", "", Crlf.Nextline,
                             new CtrlTextBox(
                                 IsJp()
                                     ? "メールアドレス（メールクライアントで設定したもの）"
                                     : "MailAddress(The thing which I set in an email client)", 30)));
            l.Add(new OneVal("server", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "接続先サーバ" : "Server", 30)));
            l.Add(new OneVal("dstPort", 25, Crlf.Nextline, new CtrlInt(IsJp() ? "接続先ポート" : "Port", 5)));
            l.Add(new OneVal("address", "", Crlf.Nextline,
                             new CtrlTextBox(
                                 IsJp()
                                     ? "メールアドレス（プロパイダで指定されたもの）"
                                     : "MainAddress(The thing which was appointed in a supplier)", 30)));
            onePage.Add(new OneVal("specialUser", null, Crlf.Nextline,
                                   new CtrlDat(IsJp() ? "特別なユーザの指定" : "Special User", l, 360, IsJp())));

            return onePage;
        }

        //コントロールの変化
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

        }
    }
}



