using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.option;

namespace DnsServer {
    public class OptionDnsDomain : OneOption{

        public override string JpMenu { get { return "ドメインの追加と削除"; } }
        public override string EnMenu { get { return "Add or Remove Domains"; } }
        public override char Mnemonic { get { return 'A'; } }

        public OptionDnsDomain(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            var pageList = new List<OnePage>();
            var key = "Basic";
            pageList.Add(Page1(key,Lang.Value(key),kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var list = new ListVal();
            var key = "name";
            list.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 80)));
            key = "authority";
            list.Add(new OneVal(key, true, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            onePage.Add(new OneVal("domainList", null, Crlf.Nextline, new CtrlDat("", list, 400, IsJp())));
            return onePage;
        }
    }
}
