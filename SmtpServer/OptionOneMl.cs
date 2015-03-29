using System.Collections.Generic;
using Bjd;
using Bjd.ctrl;
using Bjd.option;

namespace SmtpServer {
    class OptionOneMl : OneOption {
        //public override string JpMenu { get { return NameTag; } }
        //public override string EnMenu { get { return NameTag; } }
        public override char Mnemonic { get { return '0'; } }

        public OptionOneMl(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag){
            var pageList = new List<OnePage>();

            var key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key),kernel));
            key = "Member";
            pageList.Add(Page2(key, Lang.Value(key),kernel));
            pageList.Add(Page3("Guide", "Guide", kernel));
            pageList.Add(Page4("Deny", "Deny", kernel));
            pageList.Add(Page5("Confirm", "Confirm", kernel));
            pageList.Add(Page6("Welcome", "Welcome", kernel));
            pageList.Add(Page7("Append", "Append", kernel));
            pageList.Add(Page8("Help", "Help", kernel));
            pageList.Add(Page9("Admin", "Admin", kernel));
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }


        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("manageDir", "", Crlf.Nextline, new CtrlFolder(IsJp() ? "管理領域（フォルダ）" : "Management Directory",80, kernel)));
            onePage.Add(new OneVal("useDetailsLog", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "詳細ログを出力する" : "Use Details Log")));
            onePage.Add(new OneVal("title", 5, Crlf.Nextline, new CtrlComboBox(IsJp() ? "題名の形式" : "Title", new []{ "(NAME)", "[NAME]", "(00000)", "[00000]", "(NAME:00000)", "[NAME:00000]", "none" },100)));
            onePage.Add(new OneVal("maxGet", 10, Crlf.Nextline, new CtrlInt(IsJp() ? "getコマンドに対して添付するメッセージの最大数" : "The greatest number of a message to attach for a get command", 5)));
            onePage.Add(new OneVal("maxSummary", 100, Crlf.Nextline, new CtrlInt(IsJp() ? "summaryコマンドに対して列挙する件名の最大数" : "The greatest number of a title to enumerate for a summary command", 5)));
            onePage.Add(new OneVal("autoRegistration", true, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "自動登録" : "Auto Registration")));
            return onePage;
        }
        private OnePage Page2(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            l.Add(new OneVal("name", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "名前" : "Name", 20)));
            l.Add(new OneVal("address", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "メールアドレス" : "MailAddress", 20)));
            l.Add(new OneVal("manager", false, Crlf.Contonie, new CtrlCheckBox(IsJp() ? "管理者" : "Manager")));
            l.Add(new OneVal("reacer", true, Crlf.Contonie, new CtrlCheckBox(IsJp() ? "配信する" : "Reader")));
            l.Add(new OneVal("contributor", true, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "投稿を許可する" : "Contributor")));
            l.Add(new OneVal("pass", "", Crlf.Nextline, new CtrlHidden(IsJp() ? "パスワード" : "Password", 10)));
            onePage.Add(new OneVal("memberList", null, Crlf.Nextline, new CtrlOrgMemberDat(IsJp() ? "メンバーリスト" : "Member List", l,  390, IsJp())));
            return onePage;
        }
        private OnePage Page3(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("guideDocument", "", Crlf.Nextline, new CtrlMemo(IsJp() ? "メーリングリストの紹介 [Guide]" : "An introduction of a mailing list [Guide]", 600, 360)));
            return onePage;
        }
        private OnePage Page4(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("denyDocument", "", Crlf.Nextline, new CtrlMemo(IsJp() ? "メンバー以外から投稿があった際に返信するメッセージ  [Deny]" : "A message to reply to a contribution from out of a member [Deny]", 600, 360)));
            return onePage;
        }
        private OnePage Page5(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("confirmDocument", "", Crlf.Nextline, new CtrlMemo(IsJp() ? "subscribe時の確認用メッセージ [Confirm]" : "A message for confirmation in subscribe [Confirm]", 600, 360)));
            return onePage;
        }
        private OnePage Page6(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("welcomeDocument", "", Crlf.Nextline, new CtrlMemo(IsJp() ? "登録完了時の歓迎メッセージ [Welcome]" : "A welcome message in registration completion [Welcome]", 600, 360)));
            return onePage;
        }
        private OnePage Page7(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("appendDocument", "", Crlf.Nextline, new CtrlMemo(IsJp() ? "管理者宛の登録依頼メッセージ [Accept]" : "A registration request message addressed to a manager [Append]", 600, 360)));
            return onePage;
        }
        private OnePage Page8(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("helpDocument", "", Crlf.Nextline, new CtrlMemo(IsJp() ? "メンバー用ヘルプメッセージ [Help]" : "A help message for members [Help]", 600, 360)));
            return onePage;
        }
        private OnePage Page9(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("adminDocument", "", Crlf.Nextline, new CtrlMemo(IsJp() ? "管理者用ヘルプメッセージ [Admin]" : "A help message for adminstrator [Admin]", 600, 360)));
            return onePage;
        }
    }
}
