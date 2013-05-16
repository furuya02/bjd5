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

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "Webサーバを使用する" : "Use Web Server")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel,protocol));
            pageList.Add(Page2("CGI", "CGI", kernel));
            pageList.Add(Page3("SSI", "SSI", kernel));
            pageList.Add(Page4("WebDAV","WebDAV" , kernel));
            pageList.Add(Page5("Alias", IsJp() ? "別名指定" : "Alias", kernel));
            pageList.Add(Page6("MimeType", IsJp() ? "MIMEタイプ" : "MIME Type", kernel));
            pageList.Add(Page7("Certification", IsJp() ? "認証リスト" : "Certification", kernel));
            pageList.Add(Page8("CertUserList", IsJp() ? "認証（ユーザリスト）" : "Certification(User List)", kernel));
            pageList.Add(Page9("CertGroupList", IsJp() ? "認証（グループリスト）" : "Certification(Group List)", kernel));
            pageList.Add(Page10("ModelSentence", IsJp() ? "雛型" : "Model Sentence", kernel));
            pageList.Add(Page11("AutoACL", IsJp() ? "自動拒否" : "AutoDeny", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(_kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel,int protocol) {
            var onePage = new OnePage(name, title);

            onePage.Add(new OneVal("protocol", protocol, Crlf.Nextline, new CtrlComboBox(IsJp() ? "プロトコル" : "Protocol", new[]{ "HTTP", "HTTPS" },100)));
            
            var port = 80;
            //nameTagからポート番号を取得しセットする（変更不可）
            var tmp = NameTag.Split(':');
            if (tmp.Length == 2) {
                port = Convert.ToInt32(tmp[1]);
            }
            onePage.Add(CreateServerOption(ProtocolKind.Tcp, port, 3, 10)); //サーバ基本設定

            onePage.Add(new OneVal("documentRoot", "", Crlf.Nextline, new CtrlFolder(IsJp() ? "ドキュメントのルートディレクトリ" : "DocumentRoot", 50,kernel)));
            onePage.Add(new OneVal("welcomeFileName", "index.html", Crlf.Nextline, new CtrlTextBox(IsJp() ? "Welcomeファイルの指定(カンマで区切って複数指定可能です)" : "Welcome File", 30)));
            onePage.Add(new OneVal("useHidden", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "隠し属性ファイルへリクエストを許可する" : "Cover it and prohibit a request to a file of attribute")));
            onePage.Add(new OneVal("useDot", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "URLに..が含まれるリクエストを許可する" : "Prohibit the request that .. is include in")));
            onePage.Add(new OneVal("useExpansion", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "BJDを経由したリクエストの特別拡張を有効にする" : "Use special expansion")));
            onePage.Add(new OneVal("useDirectoryEnum", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "ディレクトリ一覧を表示する" : "Display Index")));
            onePage.Add(new OneVal("serverHeader", "BlackJumboDog Version $v", Crlf.Nextline, new CtrlTextBox(IsJp() ? "Server:ヘッダの指定" : "Server Header", 50)));
            onePage.Add(new OneVal("useEtag", false, Crlf.Contonie, new CtrlCheckBox(IsJp() ? "ETagを追加する" : "Use ETag")));
            onePage.Add(new OneVal("serverAdmin", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "管理者メールアドレス" : "server admin", 30)));

            return onePage;
        }

        private OnePage Page2(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                onePage.Add(new OneVal("useCgi", false,Crlf.Nextline,new CtrlCheckBox(IsJp() ? "CGIを使用する" : "Use CGI")));
                {//DAT
                    var l = new ListVal();
                    l.Add(new OneVal("cgiExtension","",Crlf.Contonie,new CtrlTextBox(IsJp() ? "拡張子" : "Extension", 10)));
                    l.Add(new OneVal("Program","",Crlf.Nextline,new CtrlFile(IsJp() ? "プログラム" : "Program",50,kernel)));
                    onePage.Add(new OneVal("cgiCmd",null,Crlf.Nextline,new CtrlDat("",l,142,IsJp())));
                }//DAT
                onePage.Add(new OneVal("cgiTimeout", 10,Crlf.Nextline,new CtrlInt(IsJp() ? "CGIタイムアウト(秒)" : "CGI Timeout(sec)", 5)));
                {//DAT
                    var l = new ListVal();
                    l.Add(new OneVal("CgiPath","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "CGIパス" : "CGI Path", 50)));
                    l.Add(new OneVal("cgiDirectory","",Crlf.Nextline,new CtrlFolder(IsJp() ? "参照ディレクトリ" : "Directory",60,kernel)));
                    onePage.Add(new OneVal("cgiPath", null,Crlf.Nextline,new CtrlDat(IsJp() ? "CGIパスを指定した場合、指定したパスのみCGIが許可されます" : "When I appointed a CGI path It is admitted CGI only the path that I appointed",l,155,IsJp())));
                }//DAT
            return onePage;
        }

        private OnePage Page3(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                onePage.Add(new OneVal("useSsi", false,Crlf.Nextline,new CtrlCheckBox(IsJp() ? "SSIを使用する" : "Use SSI")));
                onePage.Add(new OneVal("ssiExt", "html,htm",Crlf.Nextline,new CtrlTextBox(IsJp() ? "SSIとして認識する拡張子(カンマで区切って複数指定できます)" : "Extension to recognize as SSI ( Separator , )", 30)));
                onePage.Add(new OneVal("useExec", false,Crlf.Nextline,new CtrlCheckBox(IsJp() ? "exec cmd (cgi) を有効にする" : "Use exec,cmd(cgi)")));
            return onePage;
        }
        private OnePage Page4(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
                onePage.Add(new OneVal("useWebDav", false,Crlf.Nextline,new CtrlCheckBox(IsJp() ? "WebDAVを使用する" : "Use WebDAV")));
                var l = new ListVal();
                    l.Add(new OneVal("WebDAV Path","",Crlf.Nextline,new CtrlTextBox(IsJp() ? "WebDAVパス" : "WebDAV Path", 50)));
                    l.Add(new OneVal("Writing permission",false,Crlf.Nextline,new CtrlCheckBox(IsJp() ? "書き込みを許可する" : "Writing permission")));
                    l.Add(new OneVal("webDAVDirectory", "", Crlf.Nextline, new CtrlFolder(IsJp() ? "参照ディレクトリ" : "Directory", 50, kernel)));
                    onePage.Add(new OneVal("webDavPath", null,Crlf.Nextline,new CtrlDat(IsJp() ? "指定したパスでのみWevDAVが有効になります" : "WebDAV becomes effective only in the path that I appointed",l,280,IsJp())));
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
