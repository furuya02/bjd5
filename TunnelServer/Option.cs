using System;

using Bjd;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;
using System.Collections.Generic;

namespace TunnelServer {
    class Option : OneOption {

        public override string MenuStr
        {
            get { return NameTag; }
        }
        public override char Mnemonic { get { return '0'; } }

        public Option(Kernel kernel, string path, string nameTag)
            : base(kernel.IsJp(), path, nameTag) {

                var key = "useServer";
            Add(new OneVal(key, false, Crlf.Nextline, new CtrlCheckBox(Lang.Value(key))));

            var pageList = new List<OnePage>();
            key = "Basic";
            pageList.Add(Page1(key, Lang.Value(key), kernel));
            pageList.Add(PageAcl());
            Add(new OneVal("tab", null, Crlf.Nextline, new CtrlTabPage("tabPage", pageList)));

            Read(kernel.IniDb); //�@���W�X�g������̓ǂݍ���
        }

        private OnePage Page1(string name, string title, Kernel kernel) {
            var onePage = new OnePage(name, title);
            
            //nameTag����|�[�g�ԍ���擾���Z�b�g����i�ύX�s�j
            var tmp = NameTag.Split(':');
            var protocolKind = ProtocolKind.Tcp;
            var port = 0;
            var targetServer = "";
            var targetPort = 0;
            if (tmp.Length == 4) {
                //�l������I�ɐݒ�
                protocolKind = (tmp[0] == "Tunnel-TCP") ? ProtocolKind.Tcp : ProtocolKind.Udp;
                port = Convert.ToInt32(tmp[1]);
                targetServer = tmp[2];
                targetPort = Convert.ToInt32(tmp[3]);
            }
            onePage.Add(CreateServerOption(protocolKind, port, 60, 10)); //�T�[�o��{�ݒ�

            var key = "targetPort";
            onePage.Add(new OneVal(key, targetPort, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));
            key = "targetServer";
            onePage.Add(new OneVal(key, targetServer, Crlf.Nextline, new CtrlTextBox(Lang.Value(key), 50)));
            key = "idleTime";
            onePage.Add(new OneVal(key, 1, Crlf.Nextline, new CtrlInt(Lang.Value(key), 5)));


            return onePage;
        }


        //�R���g���[���̕ω�
        override public void OnChange() {
            var b = (bool)GetCtrl("useServer").Read();
            GetCtrl("tab").SetEnable(b);

            GetCtrl("port").SetEnable(false);// �|�[�g�ԍ� �ύX�s��
            //GetCtrl("protocolKind").SetEnable(false);// �v���g�R�� �ύX�s��
            GetCtrl("targetServer").SetEnable(false);// �ڑ���T�[�o�� �ύX�s��
            GetCtrl("targetPort").SetEnable(false);// �ڑ���|�[�g�ԍ� �ύX�s��
        }
    }
}
