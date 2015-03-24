using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace WebApiServer {
    public class Option : OneOption {

        //メニューに表示される文字列
        public override string JpMenu { get { return "WebAPIサーバ"; } }
        public override string EnMenu { get { return "WebAPI Server"; } }
        public override char Mnemonic { get { return 'A'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

                var key = "useServer";
            Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));

            var pageList = new List<OnePage>();
            key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key), kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み

        }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 5050, 30, 10)); //サーバ基本設定

            return onePage;
        }

        //コントロール変化時の処理
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();//「useServer」の値取得
            GetCtrl("tab").SetEnable(b);//「Basic」の有効・無効の設定
        }


    }
}
