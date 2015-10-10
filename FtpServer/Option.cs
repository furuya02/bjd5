using System.Collections.Generic;

using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;

namespace FtpServer {
    public class Option : OneOption {

        //public override string JpMenu { get { return "FTP�T�[�o"; } }
        //public override string EnMenu { get { return "FTP Server"; } }
        public override char Mnemonic { get { return 'F'; } }


        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

                var key = "useServer";
            Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));

            var pageList = new List<OnePage>();
            key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key), kernel));
            key = "VirtualFolder";
            pageList.Add(Page2(key, Lang.Value(key), kernel));
            key = "User";
            pageList.Add(Page3(key,Lang.Value(key), kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //�@���W�X�g������̓ǂݍ���
        }

        private OnePage Page1(string name, string title,Kernel kernel) {
            var onePage = new OnePage(name, title);

            onePage.Add(CreateServerOption(ProtocolKind.Tcp, 21, 30, 50)); //�T�[�o��{�ݒ�
            var key = "bannerMessage";
            onePage.Add(new OneVal(key, "FTP ( $p Version $v ) ready", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 80)));
            //���C�u�h�A���ʎd�l
            //onePage.Add(new OneVal(new ValType(CRLF.NEXTLINE, VTYPE.FILE, (IsJp()) ? "�t�@�C����M���ɋN������X�N���v�g" : "auto run acript", 250,kernel), "autoRunScript","c:\\test.bat"));
            key = "useSyst";
            onePage.Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));
            key = "reservationTime";
            onePage.Add(new OneVal(key, 5000, Crlf.Nextline, new CtrlInt(Lang.Value(key), 6)));
            return onePage;            
        }
        private OnePage Page2(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var listVal = new ListVal();
            var key = "fromFolder";
            listVal.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 70, kernel)));
            key = "toFolder";
            listVal.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 70, kernel)));
            key = "mountList";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), listVal, 360, Lang.LangKind)));
            return onePage;
        }
        private OnePage Page3(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            var listVal = new ListVal();
            var key = "accessControl";
            listVal.Add(new OneVal(key, 0, Crlf.Nextline, new CtrlComboBox(Lang.Value(key), new []{ "FULL", "DOWN", "UP" },100)));
            key = "homeDirectory";
            listVal.Add(new OneVal(key, "", Crlf.Nextline, new CtrlFolder(Lang.Value(key), 60, kernel)));
            key = "userName";
            listVal.Add(new OneVal(key, "", Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 20)));
            key = "password";
            listVal.Add(new OneVal(key, "", Crlf.Nextline, new CtrlHidden(Lang.Value(key), 20)));
            key = "user";
            onePage.Add(new OneVal(key, null, Crlf.Nextline, new CtrlDat(Lang.Value(key), listVal, 360, Lang.LangKind)));
            return onePage;
        }

        //�R���g���[���̕ω�
        override public void OnChange() {

            // �|�[�g�ԍ��ύX�֎~
            GetCtrl("port").SetEnable(false);

            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);
        }
    }
}
