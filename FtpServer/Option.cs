using System.Collections.Generic;

using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace FtpServer {
    public class Option : OneOption {

        public override string JpMenu { get { return "FTPサーバ"; } }
        public override string EnMenu { get { return "FTP Server"; } }
        public override char Mnemonic { get { return 'F'; } }


        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox((IsJp()) ? "FTPサーバを使用する" : "Use FTP Server")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic",kernel));
            pageList.Add(Page2("VirtualFolder", IsJp() ? "仮想フォルダ" : "VirtualFolder",kernel));
            pageList.Add(Page3("User", IsJp() ? "利用者" : "User", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 21, 30, 50)); //サーバ基本設定

            onePage.Add(new OneVal("bannerMessage", "FTP ( $p Version $v ) ready", Crlf.Nextline, new CtrlTextBox((IsJp()) ? "バナーメッセージ" : "Banner Message", 80)));
            //ライブドア特別仕様
            //onePage.Add(new OneVal(new ValType(CRLF.NEXTLINE, VTYPE.FILE, (IsJp()) ? "ファイル受信時に起動するスクリプト" : "auto run acript", 250,kernel), "autoRunScript","c:\\test.bat"));
            onePage.Add(new OneVal("useSyst", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "SYSTコマンドを有効にする ( セキュリティリスクの高いオプションです。必要のない限りチェックしないでください。)" : "Validate a SYST command")));
            onePage.Add(new OneVal("reservationTime", 5000, Crlf.Nextline, new CtrlInt(IsJp() ? "認証失敗時の保留時間(ミリ秒)" : "Reservation time in certification failure (msec)", 6)));
            return onePage;            
        }
        private OnePage Page2(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var listVal = new ListVal();
            listVal.Add(new OneVal("fromFolder", "", Crlf.Nextline, new CtrlFolder(IsJp() ? "実フォルダ" : "Real Folder", 70, kernel)));
            listVal.Add(new OneVal("toFolder", "", Crlf.Nextline, new CtrlFolder(IsJp() ? "マウント先" : "Mount Folder", 70, kernel)));
            onePage.Add(new OneVal("mountList", null, Crlf.Nextline, new CtrlDat(IsJp() ? "マウントの指定" : "Mount List", listVal, 360, IsJp())));
            return onePage;
        }
        private OnePage Page3(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var listVal = new ListVal();
            listVal.Add(new OneVal("accessControl", 0, Crlf.Nextline, new CtrlComboBox(IsJp() ? "アクセス制限" : "Access Control", new []{ "FULL", "DOWN", "UP" },100)));
            listVal.Add(new OneVal("homeDirectory", "", Crlf.Nextline, new CtrlFolder(IsJp() ? "ホームディレクトリ" : "Home Derectory", 60, kernel)));
            listVal.Add(new OneVal("userName", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "ユーザ名" : "User Name", 20)));
            listVal.Add(new OneVal("password", "", Crlf.Nextline, new CtrlHidden(IsJp() ? "パスワード" : "Password", 20)));
            onePage.Add(new OneVal("user", null, Crlf.Nextline, new CtrlDat(IsJp() ? "利用者（アクセス権）の指定" : "User List", listVal,360, IsJp())));
            return onePage;
        }

        //コントロールの変化
        override public void OnChange() {

            // ポート番号変更禁止
            GetCtrl("port").SetEnable(false);

            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);
        }
    }
}
