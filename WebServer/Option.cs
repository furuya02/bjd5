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
                    l.Add(new OneVal("aliasName","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "別名" : "Alias", 30)));
                    l.Add(new OneVal("aliasDirectory","",Crlf.Nextline,new CtrlFolder(IsJp() ? "参照ディレクトリ" : "Directory",50,kernel)));
                    onePage.Add(new OneVal("aliaseList", null,Crlf.Nextline,new CtrlDat(IsJp() ? "指定した名前（別名）で指定したディレクトリを直接アクセスします" : "Access the directory which I appointed by the name(alias) that I appointed directly",l,250,IsJp())));
            return onePage;
        }
        private OnePage Page6(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
                    l.Add(new OneVal("mimeExtension","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "拡張子" : "Extension", 10)));
                    l.Add(new OneVal("mimeType","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "MIMEタイプ" : "MIME Type", 50)));
                    onePage.Add(new OneVal("mime", null,Crlf.Nextline,new CtrlDat(IsJp() ? "データ形式を指定するための、「MIMEタイプ」のリストを設定します" : "Set a MIME Type list in order to appoint data form",l,350,IsJp())));
            return onePage;
        }
        private OnePage Page7(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
                    l.Add(new OneVal("authDirectory","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "URL (Directory)" : "Directory", 50)));
                    l.Add(new OneVal("AuthName","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "名前 (AuthName)" : "AuthName", 20)));
                    l.Add(new OneVal("Require","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "ユーザ/グループ (Require)" : "Require", 30)));
                    onePage.Add(new OneVal("authList", null,Crlf.Nextline,new CtrlDat(IsJp() ? "ユーザ/グループは「;」で区切って複数設定できます" : "divide it in [;], and plural [Require] can appoint it",l,350,IsJp())));
            return onePage;
        }
        private OnePage Page8(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
                    l.Add(new OneVal("user","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "ユーザ (user)" : "user", 20)));
                    l.Add(new OneVal("pass","",Crlf.Nextline,new CtrlHidden(IsJp() ? "パスワード (password)" : "password", 20)));
                    onePage.Add(new OneVal("userList", null,Crlf.Nextline,new CtrlDat(IsJp() ? "ユーザ定義" : "User List",l,350,IsJp())));
            return onePage;
        }
        private OnePage Page9(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                    var l = new ListVal();
                    l.Add(new OneVal("group","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "グループ (group)" : "group", 20)));
                    l.Add(new OneVal("userName","",Crlf.Nextline, new CtrlTextBox(IsJp() ? "ユーザ(user)" : "user", 40)));
                    onePage.Add(new OneVal("groupList", null,Crlf.Nextline,new CtrlDat(IsJp() ? "ユーザは「;」で区切って複数設定できます" : "divide it in [;], and plural [user] can appoint it",l,350,IsJp())));
            return onePage;
        }
        private OnePage Page10(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                onePage.Add(new OneVal("encode", 0,Crlf.Nextline,new CtrlComboBox(IsJp() ? "エンコード" : "Encode",new []{"UTF-8", "SHIFT-JIS", "EUC"},100)));
                onePage.Add(new OneVal("indexDocument", "",Crlf.Nextline,new CtrlMemo(IsJp() ? "インデックスドキュメント" : "Index Document", OptionDlg.Width()-15, 145)));
                onePage.Add(new OneVal("errorDocument", "", Crlf.Nextline, new CtrlMemo(IsJp() ? "エラードキュメント" : "Error Document", OptionDlg.Width() - 15, 145)));
            return onePage;
        }
        private OnePage Page11(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("useAutoAcl", false, Crlf.Nextline,
                                   new CtrlCheckBox(IsJp() ? "自動拒否を使用する" : "use automatic deny")));
            onePage.Add(new OneVal("autoAclLabel",
                                   IsJp()
                                       ? "「ACL」設定で「指定するアドレスからのアクセスのみを」-「禁止する」にチェックされている必要があります"
                                       : "It is necessary for it to be checked if I [Deny] by [ACL] setting",
                                   Crlf.Nextline,
                                   new CtrlLabel(IsJp()
                                                     ? "「ACL」設定で「指定するアドレスからのアクセスのみを」-「禁止する」にチェックされている必要があります"
                                                     : "It is necessary for it to be checked if I [Deny] by [ACL] setting")));
            var l = new ListVal();
            l.Add(new OneVal("AutoAclApacheKiller", false, Crlf.Nextline,
                             new CtrlCheckBox(IsJp() ? "Apache Killer の検出" : "Search of Apache Killer")));
            onePage.Add(new OneVal("autoAclGroup", null, Crlf.Nextline,
                                   new CtrlGroup(IsJp() ? "拒否リストに追加するイベント" : "Target Event", l)));
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
