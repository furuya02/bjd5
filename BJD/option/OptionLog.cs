using System.Collections.Generic;
using Bjd.ctrl;

namespace Bjd.option {
    public class OptionLog : OneOption {
        public override string JpMenu { get { return "ログ表示"; } }
        public override string EnMenu { get { return "Log"; } }
        public override char Mnemonic{ get { return 'L'; } }

        public OptionLog(Kernel kernel, string path) :base (kernel.IsJp() ,path, "Log"){
    		var pageList = new List<OnePage>();
		    pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
		    pageList.Add(Page2("Limit", IsJp() ? "表示制限" : "Limit Display"));
		    Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));
            Read(kernel.IniDb); //　レジストリからの読み込み
	    }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("normalLogKind", 2, Crlf.Nextline, new CtrlComboBox(IsJp() ? "通常ログ ファイル名" : "Nomal Log", new []{IsJp() ? "日ごと ( bjd.yyyy.mm.dd.log )" : "daily （bjd.yyyy.mm.dd.log）",IsJp() ? "月ごと ( bjd.yyyy.mm.log )" : "monthly （bjd.yyyy.mm.log）",IsJp() ? "一定 ( BlackJumboDog.Log )" : "Uniformity (BlackJumboDog.Log)"	}, 200)));
            onePage.Add(new OneVal("secureLogKind", 2, Crlf.Nextline, new CtrlComboBox(IsJp() ? "セキュリティログ ファイル名" : "Secure Log", new[] { IsJp() ? "日ごと ( secure.yyyy.mm.dd.log )" : "dayiy （secure.yyyy.mm.dd.log）", IsJp() ? "月ごと ( secure.yyyy.mm.log )" : "monthly secure.yyyy.mm.log）", IsJp() ? "一定 ( Secure.Log )" : "Uniformity (BlackJumboDog.Log)" }, 200)));
            onePage.Add(new OneVal("saveDirectory", "", Crlf.Nextline, new CtrlFolder(IsJp() ? "ログの保存場所" : "Save place", 60, kernel)));
            onePage.Add(new OneVal("useLogFile", true, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "ログファイルを生成する" : "Generate a Log File")));
            onePage.Add(new OneVal("useLogClear", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "ログの削除を自動的に行う" : "Eliminate it regularly")));
            onePage.Add(new OneVal("saveDays", 31, Crlf.Nextline, new CtrlInt(IsJp() ? "ログ保存日数(0を指定した場合、削除しない)" : "Save days(When You appointed 0, Don't eliminate)", 3)));
            onePage.Add(new OneVal("linesMax", 3000, Crlf.Nextline, new CtrlInt(IsJp() ? "表示する最大行数" : "The number of maximum line to display", 5)));
            onePage.Add(new OneVal("linesDelete", 2000, Crlf.Nextline, new CtrlInt(IsJp() ? "最大行数に達した際に削除する行数" : "The number of line to eliminate when I reached a maximum", 5)));
            onePage.Add(new OneVal("font", null, Crlf.Nextline, new CtrlFont("", IsJp())));
            return onePage;
        }

        private OnePage Page2(string name, string title) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("isDisplay", 1, Crlf.Nextline, new CtrlRadio(IsJp() ? "指定文字列のみを" : "A case including character string", new []{IsJp() ? "表示する" : "Display",IsJp() ? "表示しない" : "Don't display" }, OptionDlg.Width() - 15, 2)));
            var list = new ListVal();
            list.Add(new OneVal("Character", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "文字列指定" : "Character", 50)));
            onePage.Add(new OneVal("limitString", null, Crlf.Nextline, new CtrlDat(IsJp() ? "制限する文字列の指定" : "Limit Character", list, 230, IsJp())));
            onePage.Add(new OneVal("useLimitString", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "上記のルールをログファイルにも適用する" : "Apply this rule in Log")));
            return onePage;
        }
        //コントロールの変化
        override public void OnChange() {
            var b = (bool)GetCtrl("useLogClear").Read();
            GetCtrl("saveDays").SetEnable(b);
     
        }
    }  
}
         

