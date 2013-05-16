using System.Collections.Generic;


using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace SmtpServer {
    public class Option : OneOption{

        public override string JpMenu{
            get { return "SMTPサーバ"; }
        }

        public override string EnMenu{
            get { return "SMTP Server"; }
        }

        public override char Mnemonic{
            get { return 'S'; }
        }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){

            Add(new OneVal("useServer", false, Crlf.Nextline,
                           new CtrlCheckBox(IsJp() ? "SMTPサーバを使用する" : "Use SMTP Server")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(Page2("ESMTP", IsJp() ? "拡張SMTP" : "ESMTP", kernel));
            pageList.Add(Page3("Relay", IsJp() ? "中継許可" : "Relay", kernel));
            pageList.Add(Page4("Queue", IsJp() ? "キュー処理" : "Queue", kernel));
            pageList.Add(Page5("Host", IsJp() ? "ホスト設定" : "Host", kernel));
            pageList.Add(Page6("Heda", IsJp() ? "ヘッダ変換" : "Change of Header", kernel));
            pageList.Add(Page7("Aliases", IsJp() ? "エリアス" : "Aliases", kernel));
            pageList.Add(Page8("AutoReception", IsJp() ? "自動受信" : "Auto Reception", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 25, 30, 10)); //サーバ基本設定

            onePage.Add(new OneVal("domainName", "example.com", Crlf.Nextline,
                                   new CtrlTextBox(IsJp() ? "ドメイン名（,で区切って複数指定できます）" : "Domain Name", 50)));
            onePage.Add(new OneVal("bannerMessage", "$s SMTP $p $v; $d", Crlf.Nextline,new CtrlTextBox(IsJp() ? "バナーメッセージ" : "Banner Message", 50)));
            onePage.Add(new OneVal("receivedHeader", "from $h ([$a]) by $s with SMTP id $i for <$t>; $d",Crlf.Nextline,new CtrlTextBox(IsJp() ? "Receivedヘッダ" : "Received Header", 50)));
            onePage.Add(new OneVal("sizeLimit", 5000, Crlf.Nextline,new CtrlInt(IsJp()? "受信サイズ制限(KByte)  [0=制限無し]": "Capacity of the email which every user can store (KByte) [0 is unlimited]",8)));
            onePage.Add(new OneVal("errorFrom", "root@local", Crlf.Nextline,new CtrlTextBox(IsJp() ? "エラー時のFromアドレス" : "From Address on Error", 50)));
            onePage.Add(new OneVal("useNullFrom", false, Crlf.Contonie,new CtrlCheckBox(IsJp() ? "空白のFROMを許可する" : "Forgive Null From")));
            onePage.Add(new OneVal("useNullDomain", false, Crlf.Nextline,new CtrlCheckBox(IsJp() ? "ドメイン名の無いFROMを許可する" : "Forgive Null Domain")));
            onePage.Add(new OneVal("usePopBeforeSmtp", false, Crlf.Contonie,new CtrlCheckBox(IsJp() ? "POP before SMTPを使用する" : "Use POP Before SMTP")));
            onePage.Add(new OneVal("timePopBeforeSmtp", 10, Crlf.Nextline,new CtrlInt(IsJp() ? "POP before SNTP の有効時間（秒)" : "Timeout of POP Before SMTP", 5)));
            onePage.Add(new OneVal("useCheckFrom", false, Crlf.Nextline,new CtrlCheckBox(IsJp()? "メールアドレス（From:）偽造を許可しない": "Don't admit forgery of an email address")));

            return onePage;
        }
    


        private OnePage Page2(string name, string title,Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("useEsmtp", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "拡張SMTPを使用する" : "Use ESMTP")));
            var list1 = new ListVal();
            list1.Add(new OneVal("useAuthCramMD5", true, Crlf.Contonie, new CtrlCheckBox("CRAM-MD5")));
            list1.Add(new OneVal("useAuthPlain", true, Crlf.Contonie, new CtrlCheckBox("PLAIN")));
            list1.Add(new OneVal("useAuthLogin", true, Crlf.Nextline, new CtrlCheckBox("LOGIN")));
            onePage.Add(new OneVal("groupAuthKind", null, Crlf.Nextline,new CtrlGroup(IsJp() ? "使用する認証方式" : "Certification system to use", list1)));
            onePage.Add(new OneVal("usePopAcount", false, Crlf.Nextline,
                               new CtrlCheckBox(IsJp() ? "ユーザ情報はメールボックスの利用者情報を使用する" : "Use MailBox Aount")));
            var list2 = new ListVal();
            list2.Add(new OneVal("user", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "アカウント" : "User", 15)));
            list2.Add(new OneVal("pass", "", Crlf.Contonie, new CtrlHidden(IsJp() ? "パスワード" : "Password", 15)));
            list2.Add(new OneVal("comment", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "コメント" : "Comment", 20)));
            onePage.Add(new OneVal("esmtpUserList", null, Crlf.Nextline,new CtrlDat(IsJp() ? "ユーザ情報" : "User List", list2,115, IsJp())));
            onePage.Add(new OneVal("enableEsmtp", 0, Crlf.Nextline,new CtrlRadio(IsJp() ? "指定したアドレスからのアクセスのみ" : "Access of ths user who appoint it",new[]{IsJp() ? "適用しない" : "don't apply", IsJp() ? "適用する" : "apply"}, OptionDlg.Width()-15, 2)));
            
            var list3 = new ListVal();
            list3.Add(new OneVal("rangeName", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "名前（表示名）" : "Name(Display)", 20)));
            list3.Add(new OneVal("rangeAddress", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "アドレス" : "Address", 20)));
            onePage.Add(new OneVal("range", null, Crlf.Nextline, new CtrlDat("", list3, 115, IsJp())));
            return onePage;
        }

        private OnePage Page3(string name, string title,Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("order", 0, Crlf.Nextline,
                                   new CtrlRadio(IsJp() ? "リストの優先順位" : "Order",
                                                 IsJp()
                                                     ? new[]{"許可リスト優先", "禁止リスト優先"}
                                                     : new[]{"Allow/Deny", "Deny/Allow"}, 600, 2)));
            var list1 = new ListVal();
            list1.Add(new OneVal("allowAddress", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "アドレス" : "Address", 30)));
            onePage.Add(new OneVal("allowList", null, Crlf.Nextline,new CtrlDat(IsJp() ? "許可リスト" : "Allow List", list1, 170, IsJp())));
            var list2 = new ListVal();
            list2.Add(new OneVal("denyAddress", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "アドレス" : "Address", 30)));
            onePage.Add(new OneVal("denyList", null, Crlf.Nextline,new CtrlDat(IsJp() ? "禁止リスト" : "Deny List", list2, 170, IsJp())));
            return onePage;
        }

        private OnePage Page4(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("always", true, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "キュー常時処理（このチェックを外すとキューは処理されません）" : "Use Queue Processing")));
            onePage.Add(new OneVal("threadSpan", 300, Crlf.Nextline, new CtrlInt(IsJp() ? "最小処理間隔(秒)" : "Thread Span", 10)));
            onePage.Add(new OneVal("retryMax", 5, Crlf.Nextline, new CtrlInt(IsJp() ? "リトライ回数" : "Retry Max", 5)));
            onePage.Add(new OneVal("threadMax", 5, Crlf.Nextline, new CtrlInt(IsJp() ? "処理スレッド数" : "Thread Max", 5)));
            onePage.Add(new OneVal("mxOnly", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "MXレコードのみを使用する" : "Only MX")));
            return onePage;            
        }
        private OnePage Page5(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            l.Add(new OneVal("transferTarget", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "対象ドメイン" : "Target Domain", 30)));
            l.Add(new OneVal("transferServer", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "転送サーバ" : "Transfer Server", 30)));
            l.Add(new OneVal("transferPort", 25, Crlf.Nextline, new CtrlInt(IsJp() ? "ポート" : "Transfer Port", 5)));
            l.Add(new OneVal("transferSmtpAuth", false, Crlf.Contonie, new CtrlCheckBox(IsJp() ? "SMTP認証" : "SMTP Auth")));
            l.Add(new OneVal("transferUser", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "ユーザ名" : "User", 25)));
            l.Add(new OneVal("transferPass", "", Crlf.Nextline, new CtrlHidden(IsJp() ? "パスワード" : "Pass", 25)));
            l.Add(new OneVal("transferSsl", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "SSLで接続する" : "connected in SSL")));
            onePage.Add(new OneVal("hostList", null, Crlf.Nextline, new CtrlOrgHostDat("", l, 370, IsJp())));
            return onePage;            
        }
        private OnePage Page6(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var list1 = new ListVal();
            list1.Add(new OneVal("pattern", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "パターン文字列" : "Pattern", 70)));
            list1.Add(new OneVal("Substitution", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "置き換え文字列" : "Substitution", 70)));
            onePage.Add(new OneVal("patternList", null, Crlf.Nextline, new CtrlDat(IsJp() ? "置き換え" : "Substitution", list1, 185, IsJp())));
            var list2 = new ListVal();
            list2.Add(new OneVal("tag", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "タグ" : "Tag", 30)));
            list2.Add(new OneVal("string", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "文字列" : "String", 80)));
            onePage.Add(new OneVal("appendList", null, Crlf.Nextline, new CtrlDat(IsJp() ? "追加" : "Append", list2, 185, IsJp())));
            return onePage;            
        }
        private OnePage Page7(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            l.Add(new OneVal("aliasUser", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "ユーザ名" : "user", 30)));
            l.Add(new OneVal("aliasName", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "別名" : "alias", 80)));
            onePage.Add(new OneVal("aliasList", null, Crlf.Nextline, new CtrlDat(IsJp() ? "エリアス指定 ( 別名はカンマで区切って複数指定できます )" : "Aliase List", l, 250, IsJp())));
            return onePage;            
        }
        private OnePage Page8(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            l.Add(new OneVal("fetchReceptionInterval", 60, Crlf.Nextline, new CtrlInt(IsJp() ? "受信間隔(分)" : "Reception interval(min)", 5)));
            l.Add(new OneVal("fetchServer", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "サーバ" : "Server", 30)));
            l.Add(new OneVal("fetchPort", 110, Crlf.Nextline, new CtrlInt(IsJp() ? "ポート" : "Port", 5)));
            l.Add(new OneVal("fetchUser", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "ユーザ" : "User", 20)));
            l.Add(new OneVal("fetchPass", "", Crlf.Nextline, new CtrlHidden(IsJp() ? "パスワード" : "Password", 20)));
            l.Add(new OneVal("fetchLocalUser", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "配信先(ローカルユーザ)" : "A point to serve (Local user)", 30)));
            l.Add(new OneVal("fetchSynchronize", 0, Crlf.Contonie, new CtrlComboBox(IsJp() ? "同期" : "Synchronize", new[] { IsJp() ? "サーバに残す" : "An email of a server does not eliminate it", IsJp() ? "メールボックスと同期する" : "Synchronize it with a mailbox", IsJp() ? "サーバから削除する" : "An email of a server eliminates it" }, 130)));
            l.Add(new OneVal("fetchTime", 0, Crlf.Nextline, new CtrlInt(IsJp() ? "サーバに残す時間(分)" : "Time to have for a server(min)", 6)));
            onePage.Add(new OneVal("fetchList", null, Crlf.Nextline, new CtrlOrgAutoReceptionDat("", l, 370, IsJp())));
            return onePage;            
        }


        //コントロールの変化
        override public void OnChange() {

            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

            b = (bool)GetCtrl("usePopBeforeSmtp").Read();
            GetCtrl("timePopBeforeSmtp").SetEnable(b);

            b = (bool)GetCtrl("useEsmtp").Read();
            GetCtrl("groupAuthKind").SetEnable(b);
            GetCtrl("usePopAcount").SetEnable(b);
            GetCtrl("esmtpUserList").SetEnable(b);
            GetCtrl("enableEsmtp").SetEnable(b);
            GetCtrl("range").SetEnable(b);

            var m = (bool)GetCtrl("usePopAcount").Read();
            GetCtrl("esmtpUserList").SetEnable((b && !m));

            b = (bool)GetCtrl("always").Read();
            GetCtrl("threadSpan").SetEnable(b);
            GetCtrl("retryMax").SetEnable(b);
            GetCtrl("threadMax").SetEnable(b);
            GetCtrl("mxOnly").SetEnable(b);
        }
    }
}
