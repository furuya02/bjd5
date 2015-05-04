using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.option;

namespace SmtpServer {
    class OptionOneMl : OneOption {
        public override string MenuStr
        {
            get { return NameTag; }
        }
        public override char Mnemonic { get { return '0'; } }

        public OptionOneMl(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){
            var pageList = new List<OnePage>();

            var key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key),kernel));
            key = "Member";
            pageList.Add(Page2(key, Lang.Value(key),kernel));
            pageList.Add(Page3("Guide", "Guide", kernel));
            pageList.Add(Page4("Deny", "Deny", kernel));
            pageList.Add(Page5("Confirm", "Confirm", kernel));
            pageList.Add(Page6("Welcome", "Welcome", kernel));
            pageList.Add(Page7("Append", "Append", kernel));
            pageList.Add(Page8("Help", "Help", kernel));
            pageList.Add(Page9("Admin", "Admin", kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }


        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "manageDir";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key),80, kernel)));
            key = "useDetailsLog";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "title";
            onePage.Add(new OneVal(key, 5, Crlf.Nextline, new CtrlComboBox(Lang.Value(key), new[] { "(NAME)", "[NAME]", "(00000)", "[00000]", "(NAME:00000)", "[NAME:00000]", "none" }, 100)));
            key = "maxGet";
            onePage.Add(new OneVal(key, 10, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "maxSummary";
            onePage.Add(new OneVal(key, 100, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "autoRegistration";
            onePage.Add(new OneVal(key, true, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            return onePage;
        }
        private OnePage Page2(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            var key = "name";
            l.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 20)));
            key = "address";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 20)));
            key = "manager";
            l.Add(new OneVal(key, false, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
            key = "reacer";
            l.Add(new OneVal(key, true, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
            key = "contributor";
            l.Add(new OneVal(key, true, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "pass";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlHidden(Lang.Value(key), 10)));
            key = "memberList";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlOrgMemberDat(Lang.Value(key), l, 390,Lang.LangKind)));
            return onePage;
        }
        private OnePage Page3(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "guideDocument";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), 600, 360)));
            return onePage;
        }
        private OnePage Page4(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "denyDocument";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), 600, 360)));
            return onePage;
        }
        private OnePage Page5(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "confirmDocument";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), 600, 360)));
            return onePage;
        }
        private OnePage Page6(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "welcomeDocument";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), 600, 360)));
            return onePage;
        }
        private OnePage Page7(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "appendDocument";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), 600, 360)));
            return onePage;
        }
        private OnePage Page8(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "helpDocument";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), 600, 360)));
            return onePage;
        }
        private OnePage Page9(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "adminDocument";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), 600, 360)));
            return onePage;
        }
    }
}
