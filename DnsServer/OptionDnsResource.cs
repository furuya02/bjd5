using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.option;

namespace DnsServer {
    public class OptionDnsResource : OneOption {

        public override string MenuStr
        {
            get { return NameTag; }
        }
        public override char Mnemonic { get { return '0'; } }


        public OptionDnsResource(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            var pageList = new List<OnePage>();

            var key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key), kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var list = new ListVal();
            list.Add(new OneVal("type", 0, Crlf.Nextline, new CtrlComboBox("Type", new [] { "A(PTR)", "NS", "MX", "CNAME", "AAAA" },80)));
            list.Add(new OneVal("name", "", Crlf.Contonie, new CtrlTextBox("Name", 30)));
            list.Add(new OneVal("alias", "", Crlf.Nextline, new CtrlTextBox("Alias", 30)));
            list.Add(new OneVal("address", "", Crlf.Contonie, new CtrlTextBox("Address", 30)));
            list.Add(new OneVal("priority", 10, Crlf.Nextline, new CtrlInt("Priority", 5)));
            onePage.Add(new OneVal("resourceList", null, Crlf.Nextline, new CtrlOrgDat("", list, 350, IsJp())));
            return onePage;
        }


    }
}