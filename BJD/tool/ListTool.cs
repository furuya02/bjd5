using System.Linq;
using System.Windows.Forms;
using System.IO;
using Bjd.menu;
using Bjd.util;

namespace Bjd.tool {

    //****************************************************************
    // ツール管理クラス(Managerの中でのみ使用される)
    //****************************************************************
    
    public class ListTool : ListBase<OneTool> {
        public OneTool Get(string nameTag){
            return Ar.FirstOrDefault(o => o.NameTag == nameTag);
        }

        //null追加を回避するために、ar.Add()は、このファンクションを使用する
        bool Add(OneTool o) {
            if (o == null)
                return false;
            Ar.Add(o);
            return true;
        }
        //メニュー取得
        public ListMenu Menu() {

            var menu = new ListMenu();

            foreach (var a in Ar) {
                var nameTag = string.Format("Tool_{0}", a.NameTag);
                menu.Add(new OneMenu(nameTag, a.JpMenu, a.EnMenu,a.Mnemonic,Keys.None));

            }
            return menu;
        }


        //ツールリストの初期化
        public void Initialize(Kernel kernel) {
            Ar.Clear();

            //「ステータス表示」の追加
            var nameTag = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            //Add((OneTool)Util.CreateInstance(kernel,Application.ExecutablePath, "Tool", new object[] { kernel, nameTag }));
            Add(new Tool(kernel,nameTag));


            //OptionListを検索して初期化する
            foreach (var o in kernel.ListOption) {
                if (o.UseServer) {
                    var oneTool = (OneTool)Util.CreateInstance(kernel, o.Path, "Tool", new object[] { kernel, o.NameTag });
                    if (oneTool != null) {
                        Ar.Add(oneTool);
                    }
                }
            }
        }

        //メニュー取得
        public ListMenu GetListMenu(){

            var mainMenu = new ListMenu();

            foreach (var a in Ar) {
                var nameTag = string.Format("Tool_{0}", a.NameTag);
                mainMenu.Add(new OneMenu(nameTag, a.JpMenu, a.EnMenu, a.Mnemonic, Keys.None));

            }
            return mainMenu;
        }


/*
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
                }else if (a.NameTag == "DnsDomain" || a.NameTag.IndexOf("Resource-") == 0){
                    if (dnsMenu != null && dnsMenu.Count == 1){
                        dnsMenu.Add(new OneMenu()); //セパレータ
                    }
                    if (dnsMenu != null){
                        menu = dnsMenu;
                    }
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

                string nameTag = string.Format("Option_{0}", a.NameTag);

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
        */
    }
}
