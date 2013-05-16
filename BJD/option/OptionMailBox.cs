using System.Collections.Generic;
using Bjd.ctrl;

namespace Bjd.option {
    public class OptionMailBox : OneOption {
        public override string JpMenu{
            get { return "メールボックス"; }
        }

        public override string EnMenu{
            get { return "MailBox"; }
        }
        public override char Mnemonic { get { return 'B'; } }


       
        public OptionMailBox(Kernel kernel, string path)
            : base(kernel.IsJp(), path, "MailBox"){
            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic",kernel));
            pageList.Add(Page2("User", IsJp() ? "利用者" : "User"));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title,Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("dir", "", Crlf.Nextline, new CtrlFolder(IsJp() ? "作業ディレクトリ" : "Working Directory",40,kernel)));
            onePage.Add(new OneVal("useDetailsLog", false, Crlf.Nextline, new CtrlCheckBox((IsJp() ? "詳細ログを出力する" : "Use Details Log"))));
            return onePage;
        }

        private OnePage Page2(string name, string title){
            var onePage = new OnePage(name, title);
                var listVal = new ListVal();
                listVal.Add(new OneVal("userName", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "ユーザ名" : "user", 30)));
                listVal.Add(new OneVal("password", "", Crlf.Nextline, new CtrlHidden(IsJp() ? "パスワード" : "password", 30)));
                onePage.Add(new OneVal("user", null, Crlf.Nextline, new CtrlDat(IsJp() ? "利用者の指定" : "User List",listVal, 250, IsJp())));
            return onePage;
        }
    }
}
