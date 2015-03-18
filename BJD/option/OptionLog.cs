using System.Collections.Generic;
using Bjd.ctrl;
using Bjd.util;

namespace Bjd.option {
    public class OptionLog : OneOption {
        public override string JpMenu {
            get {
                return "ログ表示";
            }
        }

        public override string EnMenu {
            get {
                return "Log";
            }
        }

        public override char Mnemonic{ get { return 'L'; } }

        public OptionLog(Kernel kernel, string path) :base (kernel.IsJp() ,path, "Log"){
    		var pageList = new List<OnePage>();

            var key = "Basic";
		    pageList.Add(Page1(key,Lang.Value(key), kernel));
            key = "Limit";
            pageList.Add(Page2(key, Lang.Value(key)));
		    Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));
            Read(kernel.IniDb); //　レジストリからの読み込み
	    }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "normalLogKind";
            onePage.Add(new OneVal(key, 2, Crlf.Nextline, new CtrlComboBox(Lang.Value(key), new[] { Lang.Value(key + "1"), Lang.Value(key + "2"), Lang.Value(key + "3") }, 200)));
            key = "secureLogKind";
            onePage.Add(new OneVal(key, 2, Crlf.Nextline, new CtrlComboBox(Lang.Value(key), new[] { Lang.Value(key + "1"), Lang.Value(key + "2"), Lang.Value(key + "3") }, 200)));
            key = "saveDirectory";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 60, kernel)));
            key = "useLogFile";
            onePage.Add(new OneVal(key, true, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "useLogClear";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "saveDays";
            onePage.Add(new OneVal(key, 31, Crlf.Nextline, new CtrlInt(Lang.Value(key), 3)));
            key = "linesMax";
            onePage.Add(new OneVal(key, 3000, Crlf.Nextline, new CtrlInt(Lang.Value(key) , 5)));
            key = "linesDelete";
            onePage.Add(new OneVal(key, 2000, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            onePage.Add(new OneVal("font", null, Crlf.Nextline, new CtrlFont("", IsJp())));
            return onePage;
        }

        private OnePage Page2(string name, string title) {
            var onePage = new OnePage(name, title);
            var key = "isDisplay";
            onePage.Add(new OneVal(key, 1, Crlf.Nextline, new CtrlRadio(Lang.Value(key), new[] { Lang.Value(key + "1"), Lang.Value(key + "2") }, OptionDlg.Width() - 15, 2)));
            var list = new ListVal();

            key = "Character";
            list.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
            key = "limitString";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), list, 230, IsJp())));
            key = "useLimitString";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            return onePage;
        }
        //コントロールの変化
        override public void OnChange() {
            var b = (bool)GetCtrl("useLogClear").Read();
            GetCtrl("saveDays").SetEnable(b);
     
        }
    }  
}
         

