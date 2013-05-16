using System.Collections.Generic;

using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace ProxyHttpServer {
    class Option : OneOption {

        public override string JpMenu { get { return "ブラウザ"; } }
        public override string EnMenu { get { return "Browser"; } }
        public override char Mnemonic { get { return 'B'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "プロキシサーバ[Browser]を使用する" : "Use Proxy Server[Browser]")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(Page2("HigherProxy", IsJp() ? "上位プロキシ" : "Higher Proxy", kernel));
            pageList.Add(Page3("Cache1", IsJp() ? "キャッシュ(1)" : "Cache(1)", kernel));
            pageList.Add(Page4("Cache2", IsJp() ? "キャッシュ(2)" : "Cache(2)", kernel));
            pageList.Add(Page5("LimitUrl", IsJp() ? "ＵＲＬ制限" : "Limit URL", kernel));
            pageList.Add(Page6("LimitContents", IsJp() ? "コンテンツ制限" : "Limit Contents", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }
        
        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 8080, 60, 300)); //サーバ基本設定

            onePage.Add(new OneVal("useRequestLog", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "リクエストログを通常ログで出力する" : "Display request in normal log")));

            var list1 = new ListVal();
            list1.Add(new OneVal("anonymousAddress", "BlackJumboDog@", Crlf.Nextline, new CtrlTextBox(IsJp() ? "メールアドレス" : "Mail Address", 70)));
            list1.Add(new OneVal("serverHeader", "BlackJumboDog Version $v", Crlf.Nextline, new CtrlTextBox(IsJp() ? "Server:ヘッダの指定" : "Server Header", 70)));
            onePage.Add(new OneVal("anonymousFtp", null, Crlf.Nextline, new CtrlGroup(IsJp() ? "anonymousFTP(ftp://～）に接続に関する指定" : "anonymousFTP(ftp://～）setting", list1)));

            onePage.Add(new OneVal("useBrowserHedaer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "リクエストヘッダをブラウザと同じにする" : "Request header ths same as z browser")));

            var list2 = new ListVal();
            list2.Add(new OneVal("addHeaderRemoteHost", false, Crlf.Contonie, new CtrlCheckBox("Remote-Host:")));
            list2.Add(new OneVal("addHeaderXForwardedFor", false, Crlf.Contonie, new CtrlCheckBox("X-Forwarded-For:")));
            list2.Add(new OneVal("addHeaderForwarded", false, Crlf.Nextline, new CtrlCheckBox("Forwarded:")));
            onePage.Add(new OneVal("groupAddHeader", null, Crlf.Nextline, new CtrlGroup(IsJp() ? "追加するヘッダ" : "Append Headers", list2)));

            return onePage;
        }

        private OnePage Page2(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("useUpperProxy", false, Crlf.Nextline,
                                   new CtrlCheckBox(IsJp() ? "更に上位のプロキシを経由する" : "Use higher Proxy")));
            onePage.Add(new OneVal("upperProxyServer", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "サーバ" : "Server", 30)));
            onePage.Add(new OneVal("upperProxyPort", 8080, Crlf.Nextline, new CtrlInt(IsJp() ? "ポート" : "Port", 5)));
            onePage.Add(new OneVal("upperProxyUseAuth", false, Crlf.Contonie,
                                 new CtrlCheckBox(IsJp() ? "認証を使用する" : "Use Authorization")));
            onePage.Add(new OneVal("upperProxyAuthName", "", Crlf.Contonie, new CtrlTextBox(IsJp() ? "ユーザ名" : "User", 20)));
            onePage.Add(new OneVal("upperProxyAuthPass", "", Crlf.Nextline, new CtrlHidden(IsJp() ? "パスワード" : "Pass", 20)));

            var list2 = new ListVal();
            list2.Add(new OneVal("address", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "アドレス" : "Address", 30)));
            onePage.Add(new OneVal("disableAddress", null, Crlf.Nextline,
                                 new CtrlDat(
                                     IsJp()
                                         ? "次で始まるアドレスには上位プロキシを使用しない"
                                         : "Don't use higher proxy for an address beginning in nest", list2, 200, IsJp())));
            return onePage;
        }

        private OnePage Page3(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("useCache", false, Crlf.Nextline,
                                   new CtrlCheckBox(IsJp() ? "キャッシュを使用する" : "Use Cache")));
            onePage.Add(new OneVal("cacheDir", "", Crlf.Nextline,
                                   new CtrlFolder(IsJp() ? "キャッシュ保存ディレクトリ" : "Cache Directory", 60,
                                                  kernel)));
            onePage.Add(new OneVal("testTime", 3, Crlf.Nextline,
                                   new CtrlInt(IsJp() ? "検査時間間隔(h)" : "Test Time Distance(h)", 5)));
            onePage.Add(new OneVal("memorySize", 1000, Crlf.Nextline,
                                   new CtrlInt(IsJp() ? "サイズ（メモリ）(KByte)" : "Size[Memory](KByte)", 10)));
            onePage.Add(new OneVal("diskSize", 5000, Crlf.Nextline,
                                   new CtrlInt(IsJp() ? "サイズ（ディスク）(KByte)" : "Size[Disk](KByte)", 10)));
            onePage.Add(new OneVal("expires", 24, Crlf.Nextline,
                                   new CtrlInt(IsJp() ? "デフォルト有効期限(h)" : "Default Expiration", 5)));
            onePage.Add(new OneVal("maxSize", 1200, Crlf.Nextline,
                                   new CtrlInt(IsJp() ? "最大サイズ(KByte)" : "Max Size(KByte)", 10)));

            return onePage;
        }
        private OnePage Page4(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            onePage.Add(new OneVal("enableHost", 1, Crlf.Nextline,
                                   new CtrlRadio(IsJp() ? "下記で指定したホストのみを" : "Only access of the Host", new[]{
                                       IsJp() ? "キャッシュする" : "Yes",
                                       IsJp() ? "キャッシュしない" : "No"
                                   }, 600, 2)));
            var list1 = new ListVal();
            list1.Add(new OneVal("host", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "ホスト名" : "Host", 30)));
            onePage.Add(new OneVal("cacheHost", null, Crlf.Nextline,
                                   new CtrlDat(IsJp() ? "ホスト指定" : "Host", list1, 135, IsJp())));
            onePage.Add(new OneVal("enableExt", 1, Crlf.Nextline,
                                   new CtrlRadio(IsJp() ? "下記で指定した拡張子のみを" : "Only access of the Extension", new[]{
                                       IsJp() ? "キャッシュする" : "Yes",
                                       IsJp() ? "キャッシュしない" : "No"
                                   }, 600, 2)));
            var list2 = new ListVal();
            list2.Add(new OneVal("ext", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "拡張子" : "Extension", 10)));
            onePage.Add(new OneVal("cacheExt", null, Crlf.Nextline,
                                   new CtrlDat(IsJp() ? "拡張子指定" : "Extension", list2, 135, IsJp())));

            return onePage;
        }

        private OnePage Page5(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            var list1 = new ListVal();
            list1.Add(new OneVal("allowUrl", "", Crlf.Contonie, new CtrlTextBox("URL", 30)));
            list1.Add(new OneVal("allowMatching", 0, Crlf.Nextline,
                                 new CtrlComboBox(IsJp() ? "マッチング方法" : "A matching method", new[]{
                                     IsJp() ? "前方一致" : "Front agreement",
                                     IsJp() ? "後方一致" : "Rear agreement",
                                     IsJp() ? "部分一致" : "Part agreement",
                                     IsJp() ? "正規表現" : "Regular expression"
                                 }, 100)));
            onePage.Add(new OneVal("limitUrlAllow", null, Crlf.Nextline,
                                   new CtrlDat(IsJp() ? "許可するURL" : "Allow URL", list1, 185, IsJp())));
            var list2 = new ListVal();
            list2.Add(new OneVal("denyUrl", "", Crlf.Contonie, new CtrlTextBox("URL", 30)));
            list2.Add(new OneVal("denyMatching", 0, Crlf.Nextline,
                                 new CtrlComboBox(IsJp() ? "マッチング方法" : "A matching method", new[]{
                                     IsJp() ? "前方一致" : "Front agreement",
                                     IsJp() ? "後方一致" : "Rear agreement",
                                     IsJp() ? "部分一致" : "Part agreement",
                                     IsJp() ? "正規表現" : "Regular expression"
                                 }, 100)));
            onePage.Add(new OneVal("limitUrlDeny", null, Crlf.Nextline,
                                   new CtrlDat(IsJp() ? "制限するURL" : "Deny URL", list2, 185, IsJp())));

            return onePage;
        }
        private OnePage Page6(string name, string title, Kernel kernel){
            var onePage = new OnePage(name, title);
            var l = new ListVal();
            l.Add(new OneVal("string", "", Crlf.Nextline, new CtrlTextBox(IsJp() ? "文字列" : "String", 50)));
            onePage.Add(new OneVal("limitString", null, Crlf.Nextline,
                                   new CtrlDat(IsJp() ? "次の文字列を含むアクセスを制限する" : "Limit String List", l, 300, IsJp())));

            return onePage;
        }

        //コントロールの変化
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

            b = (bool)GetCtrl("useBrowserHedaer").Read();
            GetCtrl("groupAddHeader").SetEnable(b ? false : true);


            b = (bool)GetCtrl("useCache").Read();
            GetCtrl("cacheDir").SetEnable(b);
            GetCtrl("enableHost").SetEnable(b);
            GetCtrl("cacheHost").SetEnable(b);
            GetCtrl("enableExt").SetEnable(b);
            GetCtrl("cacheExt").SetEnable(b);
            GetCtrl("testTime").SetEnable(b);
            GetCtrl("diskSize").SetEnable(b);
            GetCtrl("expires").SetEnable(b);
            GetCtrl("memorySize").SetEnable(b);
            GetCtrl("maxSize").SetEnable(b);


            b = (bool)GetCtrl("useUpperProxy").Read();
            GetCtrl("upperProxyServer").SetEnable(b);
            GetCtrl("upperProxyPort").SetEnable(b);
            GetCtrl("upperProxyUseAuth").SetEnable(b);
            GetCtrl("upperProxyAuthName").SetEnable(b);
            GetCtrl("upperProxyAuthPass").SetEnable(b);
            GetCtrl("disableAddress").SetEnable(b);

            if(b){
                //Ver5.6.9
                var b2 = (bool)GetCtrl("upperProxyUseAuth").Read();
                GetCtrl("upperProxyAuthName").SetEnable(b2);
                GetCtrl("upperProxyAuthPass").SetEnable(b2);
            }
        }
    }
}
