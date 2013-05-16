using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace TftpServer {
    class Option : OneOption {

        public override string JpMenu { get { return "TFTPサーバ"; } }
        public override string EnMenu { get { return "TFTP Server"; } }
        public override char Mnemonic { get { return 'T'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "TFTPサーバを使用する" : "Use TFTP Server")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic" , kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }
        
        private OnePage Page1(string name, string title,Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(CreateServerOption(ProtocolKind.Udp, 69, 60, 10)); //サーバ基本設定

            onePage.Add(new OneVal("workDir", "", Crlf.Nextline,new CtrlFolder(IsJp() ? "作業フォルダ" : "A work folder", 60, kernel)));
            onePage.Add(new OneVal("read", false, Crlf.Nextline,new CtrlCheckBox(IsJp() ? "「読込み」を許可する" : "Reading permission")));
            onePage.Add(new OneVal("write", false, Crlf.Nextline,new CtrlCheckBox(IsJp() ? "「書込み」を許可する" : "Reading permission")));
            onePage.Add(new OneVal("override", false, Crlf.Nextline,new CtrlCheckBox(IsJp() ? "「上書き」を許可する" : "Reading permission")));
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

