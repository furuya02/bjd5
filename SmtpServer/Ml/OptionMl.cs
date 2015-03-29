using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.option;


namespace SmtpServer {
    class OptionMl : OneOption {
        
        //public override string JpMenu { get { return "メーリングリストの追加と削除"; } }
        //public override string EnMenu { get { return "Add or Remove Maling List"; } }
        public override char Mnemonic { get { return 'A'; } }

        public OptionMl(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Mailing List", IsJp() ? "メーリングリスト" : "Mailing List", kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);

            var list = new ListVal();
            list.Add(new OneVal("user", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "名前 ( 半角英数字のみ )" : "Name", 250)));
            onePage.Add(new OneVal("mlList", null, Crlf.Nextline, new CtrlDat(IsJp() ? "利用者の指定" : "User List", list, 250, kernel.IsJp())));


            return onePage;
        }


        //コントロールの変化
        override public void OnChange() {
            
        }
    }
}
