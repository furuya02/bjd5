using System.Collections.Generic;

using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace Pop3Server {
    public class Option : OneOption{

        public override string JpMenu { get { return "POPサーバ"; } }
        public override string EnMenu { get { return "POP Server"; } }
        public override char Mnemonic { get { return 'P'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "POP3サーバを使用する" : "Use POP Server")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic"));
            pageList.Add(Page2("Cange Password", IsJp() ? "パスワード変更" : "Cange Password"));
            pageList.Add(Page3("AutoDeny", IsJp() ? "自動拒否" : "AutoDeny"));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }
        
        private OnePage Page1(string name, string title) {
            var onePage = new OnePage(name, title);
            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 110, 30, 10)); //サーバ基本設定
            onePage.Add(new OneVal("bannerMessage", "$p (Version $v) ready", Crlf.Nextline, new CtrlTextBox(IsJp() ? "バナーメッセージ" : "BannerMessage",80)));
            onePage.Add(new OneVal("authType", 0, Crlf.Nextline, new CtrlRadio(IsJp() ? "認証方式" : "Authorization ", new [] { IsJp() ? "USER/PASS認証" : "Only USER/PASS", IsJp() ? "APOP認証" : "Only APOP", IsJp() ? "USER/PASS及びAPOP認証" : "Bath" },600, 2)));
            onePage.Add(new OneVal("authTimeout", 30, Crlf.Nextline, new CtrlInt(IsJp() ? "認証失敗時のタイムアウト(秒)" : "Timeout in certification failure(sec)",5)));
            return onePage;            
        }

        private OnePage Page2(string name, string title) {
            var onePage = new OnePage(name, title);
                onePage.Add(new OneVal("useChps", false,Crlf.Nextline, new CtrlCheckBox(IsJp() ? "パスワード変更を許可する" : "Use CHPS")));
                onePage.Add(new OneVal("minimumLength", 8,Crlf.Nextline, new CtrlInt(IsJp() ? "最低文字数" : "admit only a password more then this sharacters", 5)));
                onePage.Add(new OneVal("disableJoe", true,Crlf.Nextline, new CtrlCheckBox(IsJp() ? "ユーザ名と同一のパスワードを許可しない" : "Don't admit password same as a user name")));

                var list = new ListVal();
                list.Add(new OneVal("useNum", true, Crlf.Contonie, new CtrlCheckBox(IsJp() ? "数字" : "Number")));
                list.Add(new OneVal("useSmall", true, Crlf.Contonie, new CtrlCheckBox(IsJp() ? "英小文字" : "Small")));
                list.Add(new OneVal("useLarge", true, Crlf.Contonie, new CtrlCheckBox(IsJp() ? "英大文字" : "Large")));
                list.Add(new OneVal("useSign", true, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "記号" : "Sign")));
                onePage.Add(new OneVal("groupNeed", null, Crlf.Nextline, new CtrlGroup(IsJp() ? "必ず含まなければならない文字" : "A required letter",list)));
            return onePage;            
        }

        private OnePage Page3(string name, string title) {
            var onePage = new OnePage(name, title);
                onePage.Add(new OneVal("useAutoAcl", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "自動拒否を使用する" : "use automatic deny")));
                onePage.Add(new OneVal("autoAclLabel", IsJp() ? "「ACL」設定で「指定するアドレスからのアクセスのみを」-「禁止する」にチェックされている必要があります" : "It is necessary for it to be checked if I [Deny] by [ACL] setting", Crlf.Nextline, new CtrlLabel(IsJp() ? "「ACL」設定で「指定するアドレスからのアクセスのみを」-「禁止する」にチェックされている必要があります" : "It is necessary for it to be checked if I [Deny] by [ACL] setting")));
                onePage.Add(new OneVal("autoAclMax", 5, Crlf.Contonie, new CtrlInt(IsJp() ? "認証失敗数（回）" : "Continuation failure frequency", 5)));
                onePage.Add(new OneVal("autoAclSec", 60, Crlf.Nextline, new CtrlInt(IsJp() ? "対象期間(秒)" : "confirmation period(sec)", 5)));
            return onePage;            
        }

        //コントロールの変化
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

            b = (bool)GetCtrl("useChps").Read();
            GetCtrl("minimumLength").SetEnable(b);
            GetCtrl("disableJoe").SetEnable(b);
            GetCtrl("groupNeed").SetEnable(b);

            b = (bool)GetCtrl("useAutoAcl").Read();
            GetCtrl("autoAclLabel").SetEnable(b);
            GetCtrl("autoAclMax").SetEnable(b);
            GetCtrl("autoAclSec").SetEnable(b);
        }

    }
}
