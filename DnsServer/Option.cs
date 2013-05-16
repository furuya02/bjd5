
using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace DnsServer {
    class Option : OneOption {
        public override string JpMenu { get { return "DNSサーバ"; } }
        public override string EnMenu { get { return "DNS Server"; } }
        public override char Mnemonic { get { return 'D'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox((IsJp()) ? "DNSサーバを使用する" : "Use DNS Server")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Udp, 53, 10, 10)); //サーバ基本設定

            onePage.Add(new OneVal("rootCache", "named.ca", Crlf.Nextline, new CtrlTextBox(IsJp() ? "ルートキャッシュ" : "Root Cache", 30)));
            onePage.Add(new OneVal("useRD", true, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "再帰要求を使用する" : "Use Recurrence")));

            var list = new ListVal();
            list.Add(new OneVal("soaMail", "postmaster", Crlf.Nextline, new CtrlTextBox(IsJp() ? "管理者メールアドレス" : "MailAddress(Admin)", 30)));
            list.Add(new OneVal("soaSerial", 1, Crlf.Nextline, new CtrlInt(IsJp() ? "連続番号" : "Serial", 5)));
            list.Add(new OneVal("soaRefresh", 3600, Crlf.Contonie, new CtrlInt(IsJp() ? "更新時間(秒)" : "Refresh(sec)", 5)));
            list.Add(new OneVal("soaRetry", 300, Crlf.Nextline, new CtrlInt(IsJp() ? "再試行(秒)" : "Retry(sec)", 5)));
            list.Add(new OneVal("soaExpire", 360000, Crlf.Contonie, new CtrlInt(IsJp() ? "終了時間(秒)" : "Expire(sec)", 5)));
            list.Add(new OneVal("soaMinimum", 3600, Crlf.Nextline, new CtrlInt(IsJp() ? "最小時間(秒)" : "Minimum(sec)", 5)));
            onePage.Add(new OneVal("GroupSoa", null, Crlf.Nextline, new CtrlGroup(IsJp() ? "ゾーン管理情報(この設定はすべてのドメインのSOAレコードとして使用されます)" : "Group SOA", list)));



            return onePage;
        }


        //コントロールの変化
        override public void OnChange() {

            // ポート番号変更禁止
            GetCtrl("port").SetEnable(false);


            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);


        }
    }
}
