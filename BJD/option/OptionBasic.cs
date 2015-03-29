using System.Collections.Generic;
using Bjd.ctrl;
using Bjd.util;

namespace Bjd.option {
    public class OptionBasic : OneOption{
        public override char Mnemonic { get { return 'O'; } }


        public OptionBasic(Kernel kernel, string path)
            : base(kernel.IsJp(), path, "Basic") {
            var pageList = new List<OnePage>();


            var key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key), kernel));

            
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel) {

            var onePage = new OnePage(name, title);

            var key = "useExitDlg";
            onePage.Add(new OneVal(key, false, Crlf.Nextline,new CtrlCheckBox(Lang.Value(key))));
            key = "useLastSize";
            onePage.Add(new OneVal(key, true, Crlf.Nextline,new CtrlCheckBox(Lang.Value(key))));
            key = "isWindowOpen";
            onePage.Add(new OneVal(key, true, Crlf.Nextline,new CtrlCheckBox(Lang.Value(key))));
            key = "useAdminPassword";
            onePage.Add(new OneVal(key, false, Crlf.Nextline,new CtrlCheckBox(Lang.Value(key))));
            key = "password";
            onePage.Add(new OneVal("password", "", Crlf.Nextline, new CtrlHidden(Lang.Value(key), 20)));
            key = "serverName";
            onePage.Add(new OneVal("serverName", "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 20)));
            key = "editBrowse";
            onePage.Add(new OneVal("editBrowse", false, Crlf.Nextline,new CtrlCheckBox(Lang.Value(key))));
            key = "lang";
            onePage.Add(new OneVal("lang", 2, Crlf.Nextline,new CtrlComboBox(Lang.Value(key), new[] { "Japanese", "English", "Auto" }, 80)));
            return onePage;
        }


        //コントロールの変化
        public override void OnChange() {
            var b = (bool) GetCtrl("useAdminPassword").Read();
            GetCtrl("password").SetEnable(b);
        }

    }
}


