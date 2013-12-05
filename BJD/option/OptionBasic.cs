using System.Collections.Generic;
using Bjd.ctrl;

namespace Bjd.option {
    public class OptionBasic : OneOption{

        public override string JpMenu{ get { return "基本オプション"; } }
        public override string EnMenu{ get { return "Basic Option"; } }
        public override char Mnemonic { get { return 'O'; } }



        public OptionBasic(Kernel kernel, string path)
            : base(kernel.IsJp(), path, "Basic"){

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("useExitDlg", false, Crlf.Nextline,
                                   new CtrlCheckBox(IsJp() ? "終了確認のメッセージを表示する" : "Display a message of end confirmation")));
            onePage.Add(new OneVal("useLastSize", true, Crlf.Nextline,
                                   new CtrlCheckBox(IsJp()
                                                        ? "前回起動時のウインドウサイズを記憶する"
                                                        : "Memorize size of a wind in last time start")));
            onePage.Add(new OneVal("isWindowOpen", true, Crlf.Nextline,
                                   new CtrlCheckBox(IsJp() ? "起動時にウインドウを開く" : "Open a window in start")));
            onePage.Add(new OneVal("useAdminPassword", false, Crlf.Nextline,
                                   new CtrlCheckBox(IsJp()
                                                        ? "ウインドウ表示時に管理者パスワードを使用する"
                                                        : "At the time of window indication, a password is necessary")));
            onePage.Add(new OneVal("password", "", Crlf.Nextline, new CtrlHidden(IsJp() ? "管理者パスワード" : "password", 20)));
            onePage.Add(new OneVal("serverName", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "サーバ名" : "Server Name", 20)));
            onePage.Add(new OneVal("editBrowse", false, Crlf.Nextline,
                                   new CtrlCheckBox(IsJp() ? "フォルダ・ファイル選択を編集にする" : "can edit browse control")));
            onePage.Add(new OneVal("lang", 2, Crlf.Nextline,
                                   new CtrlComboBox(IsJp() ? "言語" : "Language", new []{"Japanese", "English", "Auto"}, 80)));
            return onePage;
        }


        //コントロールの変化
        public override void OnChange() {
            var b = (bool) GetCtrl("useAdminPassword").Read();
            GetCtrl("password").SetEnable(b);
        }

    }
}


