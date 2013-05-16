using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace ProxyFtpServer {
    class Option : OneOption {

        public override string JpMenu { get { return "FTP"; } }
        public override string EnMenu { get { return "FTP"; } }
        public override char Mnemonic { get { return 'F'; } }


        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "プロキシサーバ[FTP]を使用する" : "Use Proxy Server [FTP]")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 8021, 60, 10)); //サーバ基本設定

            onePage.Add(new OneVal("idleTime", 1, Crlf.Nextline, new CtrlInt(IsJp() ? "アイドルタイム(m)" : "Idle Timeout(sec)", 5)));


            return onePage;
        }


        //コントロールの変化
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);
        }
    }
     
}
