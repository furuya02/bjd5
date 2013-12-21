using System;
using Bjd.menu;
using Bjd.plugin;
using System.Windows.Forms;
using Bjd.util;

namespace Bjd.option{
    //オプションのリストを表現するクラス
    //Kernelの中で使用される
    public class ListOption : ListBase<OneOption>{

        private readonly Kernel _kernel;

        public ListOption(Kernel kernel, ListPlugin listPlugin){
            _kernel = kernel;
            Initialize(listPlugin);
        }

        public OneOption Get(String nameTag){
            foreach (var o in Ar){
                if (o.NameTag == nameTag){
                    return o;
                }
            }
//            if (nameTag == "Basic"){
//                return new OptionBasic(_kernel, "");
//            }
            return null;
        }

        //null追加を回避するために、Ar.Add()は、このファンクションを使用する
        private bool Add(OneOption o){
            if (o == null){
                return false;
            }
            Ar.Add(o);
            return true;
        }

        //Kernel.Dispose()で、有効なオプションだけを出力するために使用する
        public void Save(IniDb iniDb){
            foreach (var o in Ar){
                o.Save(iniDb);
            }
        }


        //オプションリストの初期化
        private void Initialize(ListPlugin listPlugin){

            Ar.Clear();

            //固定的にBasicとLogを生成する
            const string executePath = ""; // Application.ExecutablePath
            Add(new OptionBasic(_kernel, executePath)); //「基本」オプション
            Add(new OptionLog(_kernel, executePath)); //「ログ」オプション

            foreach (var onePlugin in listPlugin){

                var oneOption = onePlugin.CreateOption(_kernel, "Option", onePlugin.Name);
                if (oneOption.NameTag == "Web"){
                    //WebServerの場合は、バーチャルホストごとに１つのオプションを初期化する
                    OneOption o = onePlugin.CreateOption(_kernel, "OptionVirtualHost", "VirtualHost");
                    if (Add(o)){
                        var dat = (Dat) o.GetValue("hostList");
                        if (dat != null){
                            foreach (var e in dat){
                                if (e.Enable){
                                    string name = string.Format("Web-{0}:{1}", e.StrList[1], e.StrList[2]);
                                    Add(onePlugin.CreateOption(_kernel, "Option", name));
                                }
                            }
                        }
                    }
                } else if (oneOption.NameTag == "Tunnel"){
                    //TunnelServerの場合は、１トンネルごとに１つのオプションを初期化する
                    OneOption o = onePlugin.CreateOption(_kernel, "OptionTunnel", "TunnelList");
                    if (Add(o)){
                        var dat = (Dat) o.GetValue("tunnelList");
                        if (dat != null){
                            foreach (var e in dat){
                                if (e.Enable){
                                    string name = string.Format("{0}:{1}:{2}:{3}", (e.StrList[0] == "0") ? "TCP" : "UDP", e.StrList[1], e.StrList[2], e.StrList[3]);
                                    Add(onePlugin.CreateOption(_kernel, "Option", String.Format("Tunnel-{0}", name)));
                                }
                            }
                        }
                    }
                } else{
                    Add(oneOption);

                    //DnsServerのプラグイン固有オプションの生成
                    if (oneOption.NameTag == "Dns"){
                        OneOption o = onePlugin.CreateOption(_kernel, "OptionDnsDomain", "DnsDomain");
                        if (Add(o)){
                            var dat = (Dat) o.GetValue("domainList");
                            if (dat != null){
                                foreach (var e in dat){
                                    if (e.Enable){
                                        Add(onePlugin.CreateOption(_kernel, "OptionDnsResource", String.Format("Resource-{0}", e.StrList[0])));
                                    }
                                }
                            }
                        }
                    } else if (oneOption.NameTag == "Smtp"){
                        //Ver6.0.0
                        OneOption o = onePlugin.CreateOption(_kernel, "OptionMl", "Ml");
                        //var o = (OneOption)Util.CreateInstance(kernel, path, "OptionMl", new object[] { kernel, path, "Ml" });
                        if (Add(o)){
                            var dat = (Dat)o.GetValue("mlList");
                            if (dat != null){
                                foreach (var e in dat){
                                    if (e.Enable){
                                        Add(onePlugin.CreateOption(_kernel, "OptionOneMl", String.Format("Ml-{0}", e.StrList[0])));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (Get("Smtp") != null || Get("Pop") != null){
                Add(new OptionMailBox(_kernel, Application.ExecutablePath)); //メールボックス
            }
        }


        /*
		//DLLを検索し、各オプションを生成する
		//Ver5.2.4 関係ない*Server.dll以外は、対象外とする
		//var list = Directory.GetFiles(kernel.ProgDir(), "*.dll").ToList();
		var list = Directory.GetFiles(kernel.ProgDir(), "*Server.dll").ToList();
		list.Sort();
		//foreach (var path in Directory.GetFiles(kernel.ProgDir(), "*.dll")) {
		foreach (var path in list) {

		    //テスト時の関連ＤＬＬを読み飛ばす
		    if (path.IndexOf("TestDriven") != -1)
		        continue;

		    string nameTag = Path.GetFileNameWithoutExtension(path);

		    //DLLバージョン確認
		    var vi = FileVersionInfo.GetVersionInfo(path);
		    if (vi.FileVersion != Define.ProductVersion()) {
		        throw new Exception(string.Format("A version of DLL is different [{0} {1}]", nameTag, vi.FileVersion));
		    }
		    
		    if (nameTag == "WebServer") {
		        var op = (OneOption)Util.CreateInstance(kernel, path, "OptionVirtualHost", new object[] { kernel, path, "VirtualHost" });
		        if (Add(op)) {
		            //WebServerの場合は、バーチャルホストごとに１つのオプションを初期化する
		            foreach (var o in (Dat)op.GetValue("hostList")) {
		                if (o.Enable) {
		                    string name = string.Format("Web-{0}:{1}", o.StrList[1], o.StrList[2]);
		                    Add((OneOption)Util.CreateInstance(kernel, path, "Option", new object[] { kernel, path, name }));
		                }
		            }
		        }
		    } else if (nameTag == "TunnelServer") {
		        //TunnelServerの場合は、１トンネルごとに１つのオプションを初期化する
		        var op = (OneOption)Util.CreateInstance(kernel, path, "OptionTunnel", new object[] { kernel, path, "TunnelList" });
		        if (Add(op)) {
		            //トンネルのリスト
		            foreach (var o in (Dat)op.GetValue("tunnelList")) {
		                if (o.Enable) {

		                    //int protocol = (int)o[0].Obj;//プロトコル
		                    //int port = (int)o[1].Obj;//クライアントから見たポート
		                    //string targetServer = (string)o[2].Obj;//接続先サーバ
		                    //int targetPort = (int)o[3].Obj;//接続先ポート
		                    string name = string.Format("{0}:{1}:{2}:{3}", (o.StrList[0] == "0") ? "TCP" : "UDP", o.StrList[1], o.StrList[2], o.StrList[3]);
		                    Add((OneOption)Util.CreateInstance(kernel, path, "Option", new object[] { kernel, path, "Tunnel-" + name }));
		                }
		            }
		        }
		    } else {  //上記以外
		        //DLLにclass Optionが含まれていない場合、Util.CreateInstanceはnulllを返すため、以下の処理はスキップされる
		        if (Add((OneOption)Util.CreateInstance(kernel, path, "Option", new object[] { kernel, path, nameTag }))) {
		            //DnsServerがリストされている場合 ドメインリソースも追加する
		            if (nameTag == "DnsServer") {
		                var o = (OneOption)Util.CreateInstance(kernel, path, "OptionDnsDomain", new object[] { kernel, path, "DnsDomain" });
		                if (Add(o)) {
		                    foreach (var e in (Dat)o.GetValue("domainList")) {
		                        if (e.Enable) {
		                            Add((OneOption)Util.CreateInstance(kernel, path, "OptionDnsResource", new object[] { kernel, path, "Resource-" + e.StrList[0] }));
		                        }
		                    }
		                }
		            }else if (nameTag == "SmtpServer") {
		#if ML_SERVER
		                var o = (OneOption)Util.CreateInstance(kernel,path, "OptionMl", new object[] { kernel, path, "Ml" });
		                if (Add(o)) {
		                    foreach (var e in (Dat)o.GetValue("mlList")) {
		                        if (e.Enable) {
		                            Add((OneOption)Util.CreateInstance(kernel,path, "OptionOneMl", new object[] { kernel, path, "Ml-" + e.StrList[0] }));
		                        }
		                    }
		                }
		#endif
		            }
		        }
		    }
		}
		//SmtpServer若しくはPopServerがリストされている場合、MailBoxを生成する
		if (Get("SmtpServer")!=null || Get("PopServer")!=null){
		    Add(new OptionMailBox(kernel, Application.ExecutablePath, "MailBox"));//メールボックス
		}

		}
		*/
    

    /**
	 * メニュー取得
	 * @return
	 */

        public ListMenu GetListMenu(){

            var mainMenu = new ListMenu();
            ListMenu webMenu = null;
            ListMenu dnsMenu = null;
            ListMenu mailMenu = null;
            ListMenu proxyMenu = null;
            int countTunnel = 0;

            foreach (var a in Ar){
                var menu = mainMenu;

                if (a.NameTag == "Dns"){
                    var m = new OneMenu("Option_DnsServer0", "DNSサーバ", "DNS Server", 'D', Keys.None);
                    mainMenu.Add(m);
                    dnsMenu = new ListMenu();
                    m.SubMenu = dnsMenu;
                    menu = dnsMenu;
                } else if (a.NameTag == "DnsDomain" || a.NameTag.IndexOf("Resource-") == 0) {
                    if (dnsMenu != null && dnsMenu.Count == 1){
                        dnsMenu.Add(new OneMenu()); //セパレータ
                    }
                    if (dnsMenu != null){
                        menu = dnsMenu;
                    }
                } else if (a.NameTag == "Ml" || a.NameTag.IndexOf("Ml-") == 0) {
                    if (mailMenu != null && mailMenu.Count == 3)
                        mailMenu.Add(new OneMenu());//セパレータ
                    menu = mailMenu;
                } else if (a.NameTag == "Pop3" || a.NameTag == "Smtp") {
                    if (mailMenu == null) {
                        var m = mainMenu.Add(new OneMenu("Option_MailServer0", "メールサーバ", "Mail Server", 'M', Keys.None));
                        mailMenu = new ListMenu();
                        m.SubMenu = mailMenu;
                    }
                    menu = mailMenu;
                } else if (a.NameTag.IndexOf("Proxy") == 0 || a.NameTag == "TunnelList") {
                    if (proxyMenu == null) {
                        var m = mainMenu.Add(new OneMenu("Option_Proxy", "プロキシサーバ", "Proxyl Server", 'P', Keys.None));
                        proxyMenu = new ListMenu();
                        m.SubMenu = proxyMenu;
                    }
                    menu = proxyMenu;
                } else if (a.NameTag == "VirtualHost") {
                    OneMenu m = mainMenu.Add(new OneMenu("Option_WebServer0", "Webサーバ", "Web Server", 'W', Keys.None));
                    webMenu = new ListMenu();
                    m.SubMenu = webMenu;
                    menu = webMenu;
                } else if (a.NameTag.IndexOf("Web-") == 0) {
                    if (webMenu != null && (webMenu.Count==1)) {
                        webMenu.Add(new OneMenu()); // セパレータ
                    }
                    menu = webMenu;
                } else if (a.NameTag.IndexOf("Tunnel-") == 0) {
                    if (countTunnel == 0) {
                        if (proxyMenu != null) {
                            proxyMenu.Add(new OneMenu()); // セパレータ
                        }
                    }
                    countTunnel++;
                    menu = proxyMenu;
                }

                String nameTag = string.Format("Option_{0}", a.NameTag);

                if (a.NameTag == "MailBox") {
                    if (mailMenu != null) {
                        mailMenu.Insert(0, new OneMenu()); // セパレータ
                        mailMenu.Insert(0, new OneMenu(nameTag, a.JpMenu, a.EnMenu, a.Mnemonic,Keys.None));
                    }
                }else{
                    menu.Add(new OneMenu(nameTag, a.JpMenu, a.EnMenu, a.Mnemonic, Keys.None));
                }

            }
            return mainMenu;
        }
    }
}

/*
    //****************************************************************
    // オプション管理クラス(Managerの中でのみ使用される)
    //****************************************************************
    public class ListOption : ListBase<OneOption> {
        public OneOption Get(string nameTag){
            return Ar.FirstOrDefault(o => o.NameTag == nameTag);
        }

        //null追加を回避するために、ar.Add()は、このファンクションを使用する
        bool Add(OneOption o) {
            if (o == null) 
                return false;
            Ar.Add(o);
            return true;
        }
        //Kernel.Dispose()で、有効なオプションだけを出力するために使用する
        public void Save() {
            foreach (var o in Ar) {
                o.Save(OptionIni.GetInstance());
            }
        }
        //オプションの読み込み
        public bool Read2(string nameTag) {
            foreach (var o in Ar) {
                if (o.NameTag == nameTag) {
                    o.Read(OptionIni.GetInstance());
                    return true;
                }
            }
            return false;
        }
        //メニュー取得
        public ListMenu Menu() {

            var mainMenu = new ListMenu();
            ListMenu webMenu = null;
            ListMenu dnsMenu = null;
            ListMenu mailMenu = null;
            ListMenu proxyMenu = null;
            int countTunnel = 0;
            
            foreach (var a in Ar) {
                var menu = mainMenu;

                if (a.NameTag == "DnsServer") {
                    var m = mainMenu.Add(new OneMenu("Option_DnsServer0", "DNSサーバ(&D)", "&DNS Server"));
                    m.SubMenu = new ListMenu();
                    dnsMenu = m.SubMenu;
                    menu = dnsMenu;
                }else if (a.NameTag == "DnsDomain" || a.NameTag.IndexOf("Resource-") == 0) {
                    if(dnsMenu != null && dnsMenu.Count==1)
                        dnsMenu.Add(new OneMenu("-", "", ""));
                    menu = dnsMenu;
                }else if (a.NameTag == "Pop3Server" || a.NameTag == "SmtpServer") {
                    if (mailMenu == null) {
                        var m = mainMenu.Add(new OneMenu("Option_MailServer0", "メールサーバ(&M)", "&Mail Server"));
                        m.SubMenu = new ListMenu();
                        mailMenu = m.SubMenu;
                    }
                    menu = mailMenu;
                } else if (a.NameTag.IndexOf("Ml") == 0) {
                    if (mailMenu != null && mailMenu.Count == 3)
                        mailMenu.Add(new OneMenu("-", "", ""));
                    menu = mailMenu;
                } else if (a.NameTag == "VirtualHost") {
                    var m = mainMenu.Add(new OneMenu("Option_WebServer0","Webサーバ(&W)","&Web Server"));
                    m.SubMenu = new ListMenu();
                    webMenu = m.SubMenu;
                    menu = webMenu;
                }else if (a.NameTag.IndexOf("Web-") == 0) {
                    if(webMenu != null && webMenu.Count==1)
                        webMenu.Add(new OneMenu("-", "", ""));
                    menu = webMenu;
                } else if (a.NameTag.IndexOf("Tunnel-") == 0) {
                    if (countTunnel == 0) {
                        if (proxyMenu != null) 
                            proxyMenu.Add(new OneMenu("-", "", ""));
                    }
                    countTunnel++;
                    menu = proxyMenu;
                } else if (a.NameTag.IndexOf("Proxy") == 0 || a.NameTag == "TunnelList") {
                    if (proxyMenu == null) {
                        var m = mainMenu.Add(new OneMenu("Option_Proxy", "プロキシサーバ(&P)", "&Proxyl Server"));
                        m.SubMenu = new ListMenu();
                        proxyMenu = m.SubMenu;
                    }
                    menu = proxyMenu;
                }

                var nameTag = string.Format("Option_{0}",a.NameTag);

                if (a.NameTag == "MailBox") {
                    if (mailMenu != null){
                        mailMenu.Insert(0, new OneMenu("-", "", ""));
                        mailMenu.Insert(0, new OneMenu(nameTag, a.JpMenu, a.EnMenu));
                    }
                } else {
                    if (menu != null) 
                        menu.Add(new OneMenu(nameTag, a.JpMenu, a.EnMenu));
                }

            }
            return mainMenu;
        }


        //オプションリストの初期化
        public void Initialize(Kernel kernel) {

            Ar.Clear();
            //固定的にBasicとLogを生成する
            Add(new OptionBasic(kernel, Application.ExecutablePath));//「基本」オプション
            Add(new OptionLog(kernel,Application.ExecutablePath));//「ログ」オプション

            //DLLを検索し、各オプションを生成する
            //Ver5.2.4 関係ない*Server.dll以外は、対象外とする
            //var list = Directory.GetFiles(kernel.ProgDir(), "*.dll").ToList();
            var list = Directory.GetFiles(kernel.ProgDir(), "*Server.dll").ToList();
            list.Sort();
            //foreach (var path in Directory.GetFiles(kernel.ProgDir(), "*.dll")) {
            foreach (var path in list) {

                //テスト時の関連ＤＬＬを読み飛ばす
                if (path.IndexOf("TestDriven") != -1)
                    continue;

                string nameTag = Path.GetFileNameWithoutExtension(path);

                //DLLバージョン確認
                var vi = FileVersionInfo.GetVersionInfo(path);
                if (vi.FileVersion != Define.ProductVersion()) {
                    throw new Exception(string.Format("A version of DLL is different [{0} {1}]", nameTag, vi.FileVersion));
                }
                
                if (nameTag == "WebServer") {
                    var op = (OneOption)Util.CreateInstance(kernel, path, "OptionVirtualHost", new object[] { kernel, path, "VirtualHost" });
                    if (Add(op)) {
                        //WebServerの場合は、バーチャルホストごとに１つのオプションを初期化する
                        foreach (var o in (Dat)op.GetValue("hostList")) {
                            if (o.Enable) {
                                string name = string.Format("Web-{0}:{1}", o.StrList[1], o.StrList[2]);
                                Add((OneOption)Util.CreateInstance(kernel, path, "Option", new object[] { kernel, path, name }));
                            }
                        }
                    }
                } else if (nameTag == "TunnelServer") {
                    //TunnelServerの場合は、１トンネルごとに１つのオプションを初期化する
                    var op = (OneOption)Util.CreateInstance(kernel, path, "OptionTunnel", new object[] { kernel, path, "TunnelList" });
                    if (Add(op)) {
                        //トンネルのリスト
                        foreach (var o in (Dat)op.GetValue("tunnelList")) {
                            if (o.Enable) {

                                //int protocol = (int)o[0].Obj;//プロトコル
                                //int port = (int)o[1].Obj;//クライアントから見たポート
                                //string targetServer = (string)o[2].Obj;//接続先サーバ
                                //int targetPort = (int)o[3].Obj;//接続先ポート
                                string name = string.Format("{0}:{1}:{2}:{3}", (o.StrList[0] == "0") ? "TCP" : "UDP", o.StrList[1], o.StrList[2], o.StrList[3]);
                                Add((OneOption)Util.CreateInstance(kernel, path, "Option", new object[] { kernel, path, "Tunnel-" + name }));
                            }
                        }
                    }
                } else {  //上記以外
                    //DLLにclass Optionが含まれていない場合、Util.CreateInstanceはnulllを返すため、以下の処理はスキップされる
                    if (Add((OneOption)Util.CreateInstance(kernel, path, "Option", new object[] { kernel, path, nameTag }))) {
                        //DnsServerがリストされている場合 ドメインリソースも追加する
                        if (nameTag == "DnsServer") {
                            var o = (OneOption)Util.CreateInstance(kernel, path, "OptionDnsDomain", new object[] { kernel, path, "DnsDomain" });
                            if (Add(o)) {
                                foreach (var e in (Dat)o.GetValue("domainList")) {
                                    if (e.Enable) {
                                        Add((OneOption)Util.CreateInstance(kernel, path, "OptionDnsResource", new object[] { kernel, path, "Resource-" + e.StrList[0] }));
                                    }
                                }
                            }
                        }else if (nameTag == "SmtpServer") {
#if ML_SERVER
                            var o = (OneOption)Util.CreateInstance(kernel,path, "OptionMl", new object[] { kernel, path, "Ml" });
                            if (Add(o)) {
                                foreach (var e in (Dat)o.GetValue("mlList")) {
                                    if (e.Enable) {
                                        Add((OneOption)Util.CreateInstance(kernel,path, "OptionOneMl", new object[] { kernel, path, "Ml-" + e.StrList[0] }));
                                    }
                                }
                            }
#endif
                        }
                    }
                }
            }
            //SmtpServer若しくはPopServerがリストされている場合、MailBoxを生成する
            if (Get("SmtpServer")!=null || Get("PopServer")!=null){
                Add(new OptionMailBox(kernel, Application.ExecutablePath));//メールボックス
            }

        }
    }

}
    */