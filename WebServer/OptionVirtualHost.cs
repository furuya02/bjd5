using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.option;

namespace WebServer {
    public class OptionVirtualHost : OneOption {
        public override string JpMenu { get { return "Webの追加と削除"; } }
        public override string EnMenu { get { return "Add or Remove VirtualHost"; } }
        public override char Mnemonic { get { return 'A'; } }



        public OptionVirtualHost(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            var pageList = new List<OnePage>();
            pageList.Add(Page1("VirtualHost", IsJp() ? "仮想ホスト" : "Virtual Host", kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            var list1 = new ListVal();
            list1.Add(new OneVal("protocol", 0, Crlf.Nextline,
                                 new CtrlComboBox(IsJp() ? "プロトコル" : "Protocol", new[]{"HTTP", "HTTPS"}, 100)));
            list1.Add(new OneVal("host", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "ホスト名" : "Host Name", 30)));
            list1.Add(new OneVal("port", 80, Crlf.Nextline, new CtrlInt(IsJp() ? "ポート番号" : "Port", 5)));
            onePage.Add(new OneVal("hostList", null, Crlf.Nextline, new CtrlOrgDat("", list1, 600, 270, kernel.IsJp())));
            var list2 = new ListVal();
            list2.Add(new OneVal("certificate", "", Crlf.Nextline,
                                 new CtrlFile(IsJp() ? "サイト証明書(.ptx)" : "site certificate(.ptx)", 50,kernel)));
            list2.Add(new OneVal("privateKeyPassword", "", Crlf.Nextline,
                                 new CtrlHidden(IsJp() ? "秘密鍵のパスワード" : "A password of private key", 20)));
            onePage.Add(new OneVal("groupHttps", null, Crlf.Nextline,
                                   new CtrlGroup(
                                       IsJp()
                                           ? "HTTPSを使用する場合は、証明書(pfx形式)が必要です"
                                           : "When they use HTTPS, a certificate is necessary", list2)));

            return onePage;
        }
        //コントロールの変化
        override public void OnChange() {
        }
    }
}
