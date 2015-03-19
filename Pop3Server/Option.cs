using System.Collections.Generic;

using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace Pop3Server {
    public class Option : OneOption{

        public override string JpMenu { get { return "POPサーバ"; } }
        public override string EnMenu { get { return "POP Server"; } }
        public override char Mnemonic { get { return 'P'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

                var key = "useServer";
            Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));

            var pageList = new List<OnePage>();
            key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key)));
            key = "Cange Password";
            pageList.Add(Page2(key, Lang.Value(key)));
            key = "AutoDeny";
            pageList.Add(Page3(key, Lang.Value(key)));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //　レジストリからの読み込み
        }
        
        private OnePage Page1(string name, string title) {
            var onePage = new OnePage(name, title);
            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 110, 30, 10)); //サーバ基本設定
            var key = "bannerMessage";
            onePage.Add(new OneVal(key, "$p (Version $v) ready", Crlf.Nextline, new CtrlTextBox(Lang.Value(key),80)));
            key = "authType";
            onePage.Add(new OneVal(key, 0, Crlf.Nextline, new CtrlRadio(Lang.Value(key), new[] { Lang.Value(key + "1"), Lang.Value(key + "2"), Lang.Value(key + "3") }, 600, 2)));
            key = "authTimeout";
            onePage.Add(new OneVal(key, 30, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            return onePage;            
        }

        private OnePage Page2(string name, string title) {
            var onePage = new OnePage(name, title);
            var key = "useChps";
                onePage.Add(new OneVal(key, false,Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
                key = "minimumLength";
                onePage.Add(new OneVal(key, 8, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
                key = "disableJoe";
                onePage.Add(new OneVal(key, true, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));

                var list = new ListVal();
                key = "useNum";
                list.Add(new OneVal(key, true, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
                key = "useSmall";
                list.Add(new OneVal(key, true, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
                key = "useLarge";
                list.Add(new OneVal(key, true, Crlf.Contonie, new CtrlCheckBox(Lang.Value(key))));
                key = "useSign";
                list.Add(new OneVal(key, true, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
                key = "groupNeed";
                onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlGroup(Lang.Value(key), list)));
            return onePage;            
        }

        private OnePage Page3(string name, string title) {
            var onePage = new OnePage(name, title);
            var key = "useAutoAcl";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "autoAclLabel";
            onePage.Add(new OneVal(key, Lang.Value(key), Crlf.Nextline, new CtrlLabel(Lang.Value(key))));
            key = "autoAclMax";
            onePage.Add(new OneVal(key, 5, Crlf.Contonie, new CtrlInt(Lang.Value(key), 5)));
            key = "autoAclSec";
            onePage.Add(new OneVal(key, 60, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            return onePage;            
        }

        //コントロールの変化
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

            b = (bool)GetCtrl("useChps").Read();
            GetCtrl("minimumLength").SetEnable(b);
            GetCtrl("disableJoe").SetEnable(b);
            GetCtrl("groupNeed").SetEnable(b);

            b = (bool)GetCtrl("useAutoAcl").Read();
            GetCtrl("autoAclLabel").SetEnable(b);
            GetCtrl("autoAclMax").SetEnable(b);
            GetCtrl("autoAclSec").SetEnable(b);
        }

    }
}
