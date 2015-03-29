using System.Collections.Generic;


using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace SmtpServer {
    public class Option : OneOption{

        public override char Mnemonic{
            get { return 'S'; }
        }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

                var key = "useServer";
            Add(new OneVal(key, false, Crlf.Nextline,new CtrlCheckBox(Lang.Value(key))));

            var pageList = new List<OnePage>();
            key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key), kernel));
            key = "ESMTP";
            pageList.Add(Page2(key, Lang.Value(key), kernel));
            key = "Relay";
            pageList.Add(Page3(key, Lang.Value(key), kernel));
            key = "Queue";
            pageList.Add(Page4(key, Lang.Value(key), kernel));
            key = "Host";
            pageList.Add(Page5(key, Lang.Value(key), kernel));
            key = "Heda";
            pageList.Add(Page6(key, Lang.Value(key), kernel));
            key = "Aliases";
            pageList.Add(Page7(key, Lang.Value(key), kernel));
            key = "AutoReception";
            pageList.Add(Page8(key, Lang.Value(key), kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 25, 30, 10)); //サーバ基本設定

            var key = "domainName";
            onePage.Add(new OneVal(key, "example.com", Crlf.Nextline,new CtrlTextBox(Lang.Value(key), 50)));
            key = "bannerMessage";
            onePage.Add(new OneVal(key, "$s SMTP $p $v; $d", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
            key = "receivedHeader";
            onePage.Add(new OneVal(key, "from $h ([$a]) by $s with SMTP id $i for <$t>; $d", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
            key = "sizeLimit";
            onePage.Add(new OneVal(key, 5000, Crlf.Nextline, new CtrlInt(Lang.Value(key), 8)));
            key = "errorFrom";
            onePage.Add(new OneVal(key, "root@local", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
            key = "useNullFrom";
            onePage.Add(new OneVal(key, false, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
            key = "useNullDomain";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "usePopBeforeSmtp";
            onePage.Add(new OneVal(key, false, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
            key = "timePopBeforeSmtp";
            onePage.Add(new OneVal(key, 10, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "useCheckFrom";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));

            return onePage;
        }
    


        private OnePage Page2(string name, string title,Kernel kernel){
            var onePage = new OnePage(name, title);
            var key = "useEsmtp";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            var list1 = new ListVal();
            list1.Add(new OneVal("useAuthCramMD5", true, Crlf.Contonie, new CtrlCheckBox("CRAM-MD5")));
            list1.Add(new OneVal("useAuthPlain", true, Crlf.Contonie, new CtrlCheckBox("PLAIN")));
            list1.Add(new OneVal("useAuthLogin", true, Crlf.Nextline, new CtrlCheckBox("LOGIN")));
            key = "groupAuthKind";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlGroup(Lang.Value(key), list1)));
            key = "usePopAcount";
            onePage.Add(new OneVal(key, false, Crlf.Nextline,
                               new CtrlCheckBox(Lang.Value(key))));
            var list2 = new ListVal();
            key = "user";
            list2.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 15)));
            key = "pass";
            list2.Add(new OneVal(key, "", Crlf.Contonie, new CtrlHidden(Lang.Value(key), 15)));
            key = "comment";
            list2.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 20)));
            key = "esmtpUserList";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), list2, 115, IsJp())));
            key = "enableEsmtp";
            onePage.Add(new OneVal(key, 0, Crlf.Nextline, new CtrlRadio(Lang.Value(key), new[] { Lang.Value(key + "1"), Lang.Value(key + "2") }, OptionDlg.Width() - 15, 2)));
            
            var list3 = new ListVal();
            key = "rangeName";
            list3.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 20)));
            key = "rangeAddress";
            list3.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 20)));
            onePage.Add(new OneVal("range", null, Crlf.Nextline, new CtrlDat("", list3, 115, IsJp())));
            return onePage;
        }

        private OnePage Page3(string name, string title,Kernel kernel){
            var onePage = new OnePage(name, title);
            var key = "order";
            onePage.Add(new OneVal(key, 0, Crlf.Nextline,new CtrlRadio(Lang.Value(key),new[] { Lang.Value(key+"1"),Lang.Value(key+"2") } , 600, 2)));
            var list1 = new ListVal();
            key = "allowAddress";
            list1.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "allowList";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), list1, 170, IsJp())));
            var list2 = new ListVal();
            key = "denyAddress";
            list2.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "denyList";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), list2, 170, IsJp())));
            return onePage;
        }

        private OnePage Page4(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "always";
            onePage.Add(new OneVal(key, true, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "threadSpan";
            onePage.Add(new OneVal(key, 300, Crlf.Nextline, new CtrlInt(Lang.Value(key), 10)));
            key = "retryMax";
            onePage.Add(new OneVal(key, 5, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "threadMax";
            onePage.Add(new OneVal(key, 5, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "mxOnly";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            return onePage;            
        }
        private OnePage Page5(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            var key = "transferTarget";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "transferServer";
            l.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 30)));
            key = "transferPort";
            l.Add(new OneVal(key, 25, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "transferSmtpAuth";
            l.Add(new OneVal(key, false, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
            key = "transferUser";
            l.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 25)));
            key = "transferPass";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlHidden(Lang.Value(key), 25)));
            key = "transferSsl";
            l.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            onePage.Add(new OneVal("hostList", null, Crlf.Nextline, new CtrlOrgHostDat("", l, 370, IsJp())));
            return onePage;            
        }
        private OnePage Page6(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var list1 = new ListVal();
            var key = "pattern";
            list1.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 70)));
            key = "Substitution";
            list1.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 70)));
            key = "patternList";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), list1, 185, IsJp())));
            var list2 = new ListVal();
            key = "tag";
            list2.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "string";
            list2.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 80)));
            key = "appendList";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), list2, 185, IsJp())));
            return onePage;            
        }
        private OnePage Page7(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            var key = "aliasUser";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "aliasName";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 80)));
            key = "aliasList";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), l, 250, IsJp())));
            return onePage;            
        }
        private OnePage Page8(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            var key = "fetchReceptionInterval";
            l.Add(new OneVal(key, 60, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "fetchServer";
            l.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 30)));
            key = "fetchPort";
            l.Add(new OneVal(key, 110, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "fetchUser";
            l.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 20)));
            key = "fetchPass";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlHidden(Lang.Value(key), 20)));
            key = "fetchLocalUser";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "fetchSynchronize";
            l.Add(new OneVal(key, 0, Crlf.Contonie, new CtrlComboBox(Lang.Value(key), new[] { Lang.Value(key + "1"), Lang.Value(key + "2"), Lang.Value(key + "3") }, 180)));
            key = "fetchTime";
            l.Add(new OneVal(key, 0, Crlf.Nextline, new CtrlInt(Lang.Value(key), 6)));
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
