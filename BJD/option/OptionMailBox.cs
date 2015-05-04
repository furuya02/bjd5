using System.Collections.Generic;
using Bjd.ctrl;

namespace Bjd.option {
    public class OptionMailBox : OneOption {
        public override char Mnemonic { get { return 'B'; } }
       
        public OptionMailBox(Kernel kernel, string path)
            : base(kernel.IsJp(), path, "MailBox"){
            var pageList = new List<OnePage>();
            var key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key),kernel));
            key = "User";
            pageList.Add(Page2(key, Lang.Value(key)));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title,Kernel kernel){
            var onePage = new OnePage(name, title);
            var key = "dir";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key),40,kernel)));
            key = "useDetailsLog";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox((Lang.Value(key)))));
            return onePage;
        }

        private OnePage Page2(string name, string title){
            var onePage = new OnePage(name, title);
            var listVal = new ListVal();
            var key = "userName";
            listVal.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "password";
            listVal.Add(new OneVal(key, "", Crlf.Nextline, new CtrlHidden(Lang.Value(key), 30)));
            key = "user";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), listVal, 250, Lang.LangKind)));
            return onePage;
        }
    }
}
