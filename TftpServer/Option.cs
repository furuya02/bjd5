using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace TftpServer {
    class Option : OneOption {

        //public override string JpMenu { get { return "TFTPサーバ"; } }
        //public override string EnMenu { get { return "TFTP Server"; } }
        public override char Mnemonic { get { return 'T'; } }

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
        
        private OnePage Page1(string name, string title,Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(CreateServerOption(ProtocolKind.Udp, 69, 60, 10)); //サーバ基本設定
            var key = "workDir";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 60, kernel)));
            key = "read";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "write";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "override";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            return onePage;
        }


        //コントロールの変化
        override public void OnChange() {

            // ポート番号変更禁止
            GetCtrl("port").SetEnable(false);

            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

            b = (bool)GetCtrl("write").Read();
            GetCtrl("override").SetEnable(b);
        }
    }
}

