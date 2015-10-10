using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.option;

namespace WebServer {
    public class OptionVirtualHost : OneOption {
        //public override string JpMenu { get { return "Web�̒ǉ��ƍ폜"; } }
        //public override string EnMenu { get { return "Add or Remove VirtualHost"; } }
        public override char Mnemonic { get { return 'A'; } }



        public OptionVirtualHost(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            var pageList = new List<OnePage>();

            var key = "VirtualHost";
            pageList.Add(Page1(key, Lang.Value(key), kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //�@���W�X�g������̓ǂݍ���
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            var list1 = new ListVal();
            var key = "protocol";
            list1.Add(new OneVal(key, 0, Crlf.Nextline,new CtrlComboBox(Lang.Value(key), new[]{"HTTP", "HTTPS"}, 100)));
            key = "host";
            list1.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 30)));
            key = "port";
            list1.Add(new OneVal(key, 80, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            onePage.Add(new OneVal("hostList", null, Crlf.Nextline, new CtrlOrgDat("", list1, 600, 270, Lang.LangKind)));
            var list2 = new ListVal();
            key = "certificate";
            list2.Add(new OneVal(key, "", Crlf.Nextline,new CtrlFile(Lang.Value(key), 50, kernel)));
            key = "privateKeyPassword";
            list2.Add(new OneVal(key, "", Crlf.Nextline,new CtrlHidden(Lang.Value(key), 20)));
            key = "groupHttps";
            onePage.Add(new OneVal(key, null, Crlf.Nextline,new CtrlGroup(Lang.Value(key), list2)));

            return onePage;
        }
        //�R���g���[���̕ω�
        override public void OnChange() {
        }
    }
}
