using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace ProxyPop3Server {
    internal class Option : OneOption{

        public override string JpMenu{
            get { return "POP3"; }
        }

        public override string EnMenu{
            get { return "POP3"; }
        }

        public override char Mnemonic{
            get { return 'P'; }
        }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            Add(new OneVal("useServer", false, Crlf.Nextline,
                           new CtrlCheckBox(IsJp() ? "POP3プロキシを使用する" : "Use POP Proxy")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(Page2("Expansion", IsJp() ? "拡張設定" : "Expansion", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 8110, 60, 10)); //サーバ基本設定
            onePage.Add(new OneVal("targetPort", 110, Crlf.Nextline, new CtrlInt(IsJp() ? "接続先ポート" : "port", 5)));
            onePage.Add(new OneVal("targetServer", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "接続先サーバ" : "server", 30)));
            onePage.Add(new OneVal("idleTime", 1, Crlf.Nextline, new CtrlInt(IsJp() ? "アイドルタイム(m)" : "Idle time (m)", 5)));

            return onePage;
        }

        private OnePage Page2(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);

            var l = new ListVal();
            l.Add(new OneVal("specialUser", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "ユーザ名（メールクライアントで設定したもの）" : "UserName(The thing which I set in an email client)", 20)));
            l.Add(new OneVal("specialServer", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "接続先サーバ" : "Server", 20)));
            l.Add(new OneVal("specialPort", 110, Crlf.Nextline, new CtrlInt(IsJp() ? "接続先ポート" : "Port", 5)));
            l.Add(new OneVal("specialName", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "ユーザ名（プロパイダで指定されたもの）" : "UserName(The thing which was appointed in a supplier)", 20)));
            onePage.Add(new OneVal("specialUserList", null, Crlf.Nextline,new CtrlDat(IsJp() ? "特別なユーザの指定" : "Special User", l, 360, IsJp())));

            return onePage;
        }

        //コントロールの変化
        public override void OnChange(){
            var b = (bool) GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

        }
    }
}


