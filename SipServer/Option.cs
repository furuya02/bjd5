using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace SipServer {
    public class Option : OneOption {

        //メニューに表示される文字列
        public override string JpMenu { get { return "SIPサーバ"; } }
        public override string EnMenu { get { return "Sip Server"; } }
        public override char Mnemonic { get { return 'Z'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "SIPサーバを使用する" : "Use Sip Server")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 5060, 30, 30)); //サーバ基本設定

            onePage.Add(new OneVal("sampleText", "Sample Server : ", Crlf.Nextline, new CtrlTextBox(IsJp() ? "サンプルメッセージ" : "SampleMessage", 60)));

            return onePage;
        }

        //コントロール変化時の処理
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();//「useServer」の値取得
            GetCtrl("tab").SetEnable(b);//「Basic」の有効・無効の設定
        }

    }
}
