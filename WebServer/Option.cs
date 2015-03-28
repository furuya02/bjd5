using System;
using System.Collections.Generic;

using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace WebServer {
    public class Option : OneOption {

        public override string JpMenu { get { return NameTag; } }
        public override string EnMenu { get { return NameTag; } }
        public override char Mnemonic { get { return '0'; } }

        private Kernel _kernel; //仮装Webの重複を検出するため必要となる



        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            _kernel = kernel;

            var protocol = 0;//HTTP
            //nameTagからポート番号を取得しセットする（変更不可）
            var tmp = NameTag.Split(':');
            if (tmp.Length == 2) {
                int port = Convert.ToInt32(tmp[1]);
                protocol = (port == 443) ? 1:0;
            }
            var key = "useServer";
            Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));

            var pageList = new List<OnePage>();
            key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key), kernel, protocol));
            pageList.Add(Page2("CGI", "CGI", kernel));
            pageList.Add(Page3("SSI", "SSI", kernel));
            pageList.Add(Page4("WebDAV","WebDAV" , kernel));
            key = "Alias";
            pageList.Add(Page5(key, Lang.Value(key), kernel));
            key = "MimeType";
            pageList.Add(Page6(key, Lang.Value(key), kernel));
            key = "Certification";
            pageList.Add(Page7(key, Lang.Value(key), kernel));
            key = "CertUserList";
            pageList.Add(Page8(key, Lang.Value(key), kernel));
            key = "CertGroupList";
            pageList.Add(Page9(key, Lang.Value(key), kernel));
            key = "ModelSentence";
            pageList.Add(Page10(key, Lang.Value(key), kernel));
            key = "AutoACL";
            pageList.Add(Page11(key, Lang.Value(key), kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(_kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel,int protocol) {
            var onePage = new OnePage(name, title);

            var key = "protocol";
            onePage.Add(new OneVal(key, protocol, Crlf.Nextline, new CtrlComboBox(Lang.Value(key), new[] { "HTTP", "HTTPS" }, 100)));
            
            var port = 80;
            //nameTagからポート番号を取得しセットする（変更不可）
            var tmp = NameTag.Split(':');
            if (tmp.Length == 2) {
                port = Convert.ToInt32(tmp[1]);
            }
            onePage.Add(CreateServerOption(ProtocolKind.Tcp, port, 3, 10)); //サーバ基本設定

            key = "documentRoot";
            onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 50, kernel)));
            key = "welcomeFileName";
            onePage.Add(new OneVal(key, "index.html", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
            key = "useHidden";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "useDot";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "useExpansion";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "useDirectoryEnum";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "serverHeader";
            onePage.Add(new OneVal(key, "BlackJumboDog Version $v", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
            key = "useEtag";
            onePage.Add(new OneVal(key, false, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
            key = "serverAdmin";
            onePage.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 30)));

            return onePage;
        }

        private OnePage Page2(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "useCgi";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
                {//DAT
                    var l = new ListVal();
                    key = "cgiExtension";
                    l.Add(new OneVal(key, "", Crlf.Contonie, new CtrlTextBox(Lang.Value(key), 10)));
                    key = "Program";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFile(Lang.Value(key), 50, kernel)));
                    onePage.Add(new OneVal("cgiCmd",null,Crlf.Nextline,new CtrlDat("",l,142,IsJp())));
                }//DAT
                key = "cgiTimeout";
                onePage.Add(new OneVal(key, 10, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
                {//DAT
                    var l = new ListVal();
                    key = "CgiPath";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
                    key = "cgiDirectory";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 60, kernel)));
                    key = "cgiPath";
                    onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), l, 155, IsJp())));
                }//DAT
            return onePage;
        }

        private OnePage Page3(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "useSsi";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
                key = "ssiExt";
                onePage.Add(new OneVal(key, "html,htm",Crlf.Nextline,new CtrlTextBox(Lang.Value(key), 30)));
                key = "useExec";
                onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            return onePage;
        }
        private OnePage Page4(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "useWebDav";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
                var l = new ListVal();
                key = "WebDAV Path";
                l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
                key = "Writing permission";
                l.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
                key = "webDAVDirectory";
                l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 50, kernel)));
                key = "webDavPath";
                onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), l, 280, IsJp())));
            return onePage;
        }
        private OnePage Page5(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
                    var key = "aliasName";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
                    key = "aliasDirectory";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 50, kernel)));
                    key = "aliaseList";
                    onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), l, 250, IsJp())));
            return onePage;
        }
        private OnePage Page6(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
                    var key = "mimeExtension";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 10)));
                    key = "mimeType";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
                    key = "mime";
                    onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), l, 350, IsJp())));
            return onePage;
        }
        private OnePage Page7(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
                    var key = "authDirectory";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
                    key = "AuthName";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 20)));
                    key = "Require";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 30)));
                    key = "authList";
                    onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), l, 350, IsJp())));
            return onePage;
        }
        private OnePage Page8(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
            var key = "user";
            l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 20)));
                    key = "pass";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlHidden(Lang.Value(key), 20)));
                    key = "userList";
                    onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), l, 350, IsJp())));
            return onePage;
        }
        private OnePage Page9(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
                    var key = "group";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 20)));
                    key = "userName";
                    l.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 40)));
                    key = "groupList";
                    onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), l, 350, IsJp())));
            return onePage;
        }
        private OnePage Page10(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var key = "encode";
            onePage.Add(new OneVal(key, 0, Crlf.Nextline, new CtrlComboBox(Lang.Value(key), new[] { "UTF-8", "SHIFT-JIS", "EUC" }, 100)));
                key = "indexDocument";
                onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), OptionDlg.Width() - 15, 145)));
                key = "errorDocument";
                onePage.Add(new OneVal(key, "", Crlf.Nextline, new CtrlMemo(Lang.Value(key), OptionDlg.Width() - 15, 145)));
            return onePage;
        }
        private OnePage Page11(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            var key = "useAutoAcl";
            onePage.Add(new OneVal(key, false, Crlf.Nextline,new CtrlCheckBox(Lang.Value(key))));
            key = "autoAclLabel";
            onePage.Add(new OneVal(key,Lang.Value(key+"1"),Crlf.Nextline,new CtrlLabel(Lang.Value(key+"2"))));
            var l = new ListVal();
            key = "AutoAclApacheKiller";
            l.Add(new OneVal(key, false, Crlf.Nextline,
                             new CtrlCheckBox(Lang.Value(key))));
            key = "autoAclGroup";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlGroup(Lang.Value(key), l)));
            return onePage;
        }

        //コントロールの変化
        override public void OnChange(){


            var b = (bool) GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

            GetCtrl("protocol").SetEnable(false);
            GetCtrl("port").SetEnable(false);

            b = (bool) GetCtrl("useCgi").Read();
            GetCtrl("cgiCmd").SetEnable(b);
            GetCtrl("cgiTimeout").SetEnable(b);
            GetCtrl("cgiPath").SetEnable(b);

            b = (bool) GetCtrl("useSsi").Read();
            GetCtrl("ssiExt").SetEnable(b);
            GetCtrl("useExec").SetEnable(b);

            b = (bool) GetCtrl("useWebDav").Read();
            GetCtrl("webDavPath").SetEnable(b);

            ////同一ポートで待ち受ける仮想サーバの同時接続数は、最初の定義をそのまま使用する
            //var port = (int)GetValue("port");
            //foreach (var o in Kernel.ListOption){
            //    if (o.NameTag.IndexOf("Web-") != 0)
            //        continue;
            //    if (port != (int) o.GetValue("port"))
            //        continue;
            //    if (o == this)
            //        continue;
            //    //このオプション以外の最初の定義を発見した場合
            //    var multiple = (int)o.GetValue("multiple");
            //    SetVal("multiple", multiple);
            //    GetCtrl("multiple").SetEnable(false);
            //   break;
            //}
            //同一ポートの仮想サーバのリストを作成する
            var ar = new List<OneOption>();
            var port = (int) GetValue("port");
            foreach (var o in _kernel.ListOption){
                if (o.NameTag.IndexOf("Web-") != 0)
                    continue;
                if (port != (int) o.GetValue("port"))
                    continue;
                if (!o.UseServer){
                    //使用していないサーバは対象外
                    continue;
                }
                ar.Add(o);
            }
            //同一ポートの仮想サーバが複数ある場合
            if (ar.Count > 1){
                //最初の定義以外は、同時接続数を設定できなくする
                if (ar[0] != this){
                    var multiple = (int) ar[0].GetValue("multiple");
                    SetVal(_kernel.IniDb,"multiple", multiple);
                    GetCtrl("multiple").SetEnable(false);
                }
            }

            b = (bool) GetCtrl("useAutoAcl").Read();
            GetCtrl("autoAclLabel").SetEnable(b);
            GetCtrl("autoAclGroup").SetEnable(b);

        }
    }
}
