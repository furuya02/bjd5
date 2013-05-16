using System;

using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;
using System.Collections.Generic;

namespace TunnelServer {
    class Option : OneOption {

        public override string JpMenu { get { return NameTag; } }
        public override string EnMenu { get { return NameTag; } }
        public override char Mnemonic { get { return '0'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

            Add(new OneVal("useServer", false, Crlf.Nextline, new CtrlCheckBox(IsJp() ? "この定義を使用する" : "Use this configration")));

            var pageList = new List<OnePage>();
            pageList.Add(Page1("Basic", IsJp() ? "基本設定" : "Basic", kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            
            //nameTagからポート番号を取得しセットする（変更不可）
            var tmp = NameTag.Split(':');
            var protocolKind = ProtocolKind.Tcp;
            var port = 0;
            var targetServer = "";
            var targetPort = 0;
            if (tmp.Length == 4) {
                //値を強制的に設定
                protocolKind = (tmp[0] == "Tunnel-TCP") ? ProtocolKind.Tcp : ProtocolKind.Udp;
                port = Convert.ToInt32(tmp[1]);
                targetServer = tmp[2];
                targetPort = Convert.ToInt32(tmp[3]);
            }
            onePage.Add(CreateServerOption(protocolKind, port, 60, 10)); //サーバ基本設定

            onePage.Add(new OneVal("targetPort", targetPort, Crlf.Nextline, new CtrlInt(IsJp() ? "接続先ポート" : "Port", 5)));
            onePage.Add(new OneVal("targetServer", targetServer, Crlf.Nextline, new CtrlTextBox(IsJp() ? "接続先サーバ" : "Server", 50)));
            onePage.Add(new OneVal("idleTime", 1, Crlf.Nextline, new CtrlInt(IsJp() ? "アイドルタイム(m)" : "Idle time (m)", 5)));


            return onePage;
        }


        //コントロールの変化
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

            GetCtrl("port").SetEnable(false);// ポート番号 変更不可
            //GetCtrl("protocolKind").SetEnable(false);// プロトコル 変更不可
            GetCtrl("targetServer").SetEnable(false);// 接続先サーバ名 変更不可
            GetCtrl("targetPort").SetEnable(false);// 接続先ポート番号 変更不可
        }
    }
}
