using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using Bjd.browse;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;
using Bjd.log;
using Bjd.sock;
using Bjd.util;

namespace Bjd.remote {
    public class RemoteClient : ThreadBase {
        readonly Logger _logger;
        readonly string _optionFileName;
        private readonly Kernel _kernel;

        private readonly Ip _ip = new Ip(IpKind.V4_0);
        readonly int _port = 10001;//�f�t�H���g�l(10001)�@�N�����̃p�����[�^�Ŏw�肳��Ȃ��ꍇ��10001���g�p�����

        SockTcp _sockTcp;
        ToolDlg _toolDlg;
        BrowseDlg _browseDlg;
        
        public RemoteClient(Kernel kernel)
            :base(kernel.CreateLogger("RemoteClient",true,null)){

            _kernel = kernel;
            
            var args = Environment.GetCommandLineArgs();
            
            //this.kernel = kernel;
            IsConected = false;
            _logger = _kernel.CreateLogger("RemoteClient", true, this);
            _optionFileName = string.Format("{0}\\{1}.ini", _kernel.ProgDir(), "$remote");

            //Java fix IsJp�͌����_�ł͕s��
            _kernel.Menu.InitializeRemote(true);//�ؒf���̌y�ʃ��j���[
            //_kernel.Menu.OnClick += Menu_OnClick;
            
            //�R�}���h���C�������̏���
            if (args.Length != 2 && args.Length !=3) {
                _logger.Set(LogKind.Error,null,1,string.Format("args.Length={0}",args.Length));
                return;
            }
            //�ڑ���A�h���X
            try{
                _ip = new Ip(args[1]);
            }catch(ValidObjException){
                _logger.Set(LogKind.Error,null,2,string.Format("ip={0}", args[1]));
                return;
            }
            //_ip = new Ip(args[1]);
            //if (_ip.ToString() == "0.0.0.0") {
            //    _logger.Set(LogKind.Error,null,2,string.Format("ip={0}", args[1]));
            //    return;
            //}
            //�ڑ���|�[�g�ԍ�
            if (args.Length == 3) {
                try {
                    _port = Convert.ToInt32(args[2]);
                } catch {
                    _logger.Set(LogKind.Error,null,3,string.Format("port={0}", args[2]));
                    _ip = new Ip(IpKind.V4_0);//���������s
                }
            }
        }

        
        //****************************************************************
        //�v���p�e�B
        //****************************************************************
        public bool IsConected { get; private set; }
        new public void Dispose() {//�j��������

            Stop();
            File.Delete(_optionFileName);

            base.Dispose();
        }
        override protected bool OnStartThread() {//�O����
            return true;
        }
        override protected void OnStopThread() {}

        //�㏈��
        override protected void OnRunThread() {//�{��

            _kernel.View.SetColor();//�y�E�C���h�F�z

            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;


            if (_ip == new Ip(IpKind.V4_0)) {
                return; //���������s
            }
            
            while (IsLife()) {

                //TraceDlg traceDlg = null;
                Ssl ssl = null;
                var timeout = 3;
                _sockTcp = Inet.Connect(_kernel,_ip,_port,timeout,ssl);

                if (_sockTcp == null) {
                    //isRun = false;
                    _logger.Set(LogKind.Error, _sockTcp,4,string.Format("address={0} port={1}", _ip, _port));
                    //�Đڑ�����݂�̂́A2�b��
                    for (int i = 0; i < 20 && IsLife(); i++) {
                        Thread.Sleep(100);
                    }
                } else {

                    _logger.Set(LogKind.Normal,_sockTcp,5,string.Format("address={0} port={1}",_ip,_port));

                    while (IsLife()) {//�ڑ���

                        if (_sockTcp.SockState != SockState.Connect){
                            //�ڑ����؂ꂽ�ꍇ�́A�����^�C�~���O��u���Ă���A�Đڑ������ɖ߂�
                            //�Đڑ�����݂�̂́A1�b��
                            _sockTcp.Close();
                            for (int i = 0; i < 10 && IsLife(); i++) {
                                Thread.Sleep(100);
                            }
                            break;
                        }

                        var o = RemoteData.Recv(_sockTcp,this);
                        if (o==null) {
                            Thread.Sleep(100);
                            continue;
                        }

                        switch(o.Kind){
                            case RemoteDataKind.DatAuth://�F�؏��i�p�X���[�h�v���j
                                var dlg = new PasswordDlg(_kernel);
                                if (DialogResult.OK == dlg.ShowDialog()) {
                                    //�n�b�V��������̍쐬�iMD5�j
                                    string md5Str = Inet.Md5Str(dlg.PasswordStr + o.Str);
                                    //DAT_AUTH�ɑ΂���p�X���[�h(C->S)
                                    RemoteData.Send(_sockTcp,RemoteDataKind.CmdAuth, md5Str);

                                } else {
                                    StopLife();//Ver5.8.4 �b�菈�u
                                }
                                break;
                            case RemoteDataKind.DatVer://�o�[�W�������
                                if (!_kernel.Ver.VerData(o.Str)) {
                                    //�T�[�o�ƃN���C�A���g�Ńo�[�W�����ɈႢ���L��ꍇ�A�N���C�A���g�@�\���~����
                                    StopLife();//Ver5.8.4 �b�菈�u
                                } else {
                                    IsConected = true;//�ڑ���
                                    _kernel.View.SetColor();//�y�E�C���h�F�z

                                    //���O�C������
                                    _logger.Set(LogKind.Normal,_sockTcp,10,"");
                                }
                                break;
                            case RemoteDataKind.DatLocaladdress://���[�J���A�h���X
                                LocalAddress.SetInstance(o.Str);
                                //_kernel.LocalAddress = new LocalAddress(o.Str);
                                break;
                            case RemoteDataKind.DatTool://�f�[�^��M
                                if (_toolDlg != null) {
                                    var tmp = o.Str.Split(new[] { '\t' }, 2);
                                    _toolDlg.CmdRecv(tmp[0],tmp[1]);
                                }
                                break;
                            case RemoteDataKind.DatBrowse://�f�B���N�g������M
                                if(_browseDlg != null) {
                                    _browseDlg.CmdRecv(o.Str);
                                }
                                break;
                            case RemoteDataKind.DatTrace://�g���[�X��M
                                _kernel.TraceDlg.AddTrace(o.Str);
                                break;
                            case RemoteDataKind.DatLog://���O��M
                                _kernel.LogView.Append(new OneLog(o.Str));//���O�r���[�ւ̒ǉ�
                                break;
                            case RemoteDataKind.DatOption://�I�v�V�����̎�M
                                //Option.ini���M����$remote.ini�ɏo�͂���
                                using (var sw = new StreamWriter(_optionFileName, false, Encoding.GetEncoding("Shift_JIS"))) {
                                    sw.Write(o.Str);
                                    sw.Close();
                                }
                                _kernel.ListInitialize();

                                break;
                            default:
                                _logger.Set(LogKind.Error, null,999,string.Format("kind = {0}",o.Kind));
                                break;
                        }

                    }
                //err:
                    _sockTcp.Close();
                    _sockTcp = null;
                    IsConected = false;//�ڑ��f
                    _kernel.Menu.InitializeRemote(_kernel.IsJp());
                    _kernel.View.SetColor();
                    _logger.Set(LogKind.Normal,null, 8,"");
                }
            }
            _logger.Set(LogKind.Normal,null, 7,"");//�����[�g�N���C�A���g��~
        }
        public void VisibleTrace2(bool enabled) {
            //TraceDlg�̕\���E��\��(C->S)
            RemoteData.Send(_sockTcp, RemoteDataKind.CmdTrace, enabled ? "1" : "0");
        }

        //RunMode��Remote�̏ꍇ�AKernel��MenuOnClick����A�����炪�Ă΂��
        public void MenuOnClick(String cmd){
            //�I�v�V�������j���[�̏ꍇ
            if (cmd.IndexOf("Option_") == 0){
                var oneOption = _kernel.ListOption.Get(cmd.Substring(7));
                if (oneOption != null) {
                    var dlg = new OptionDlg(_kernel, oneOption);
                    if (DialogResult.OK == dlg.ShowDialog()) {
                        oneOption.Save(_kernel.IniDb);//�I�v�V������ۑ�����
                        //�T�[�o���֑��M����
                        string optionStr;
                        using (var sr = new StreamReader(_optionFileName, Encoding.GetEncoding("Shift_JIS"))) {
                            optionStr = sr.ReadToEnd();
                            sr.Close();
                        }
                        //Option�̑��M(C->S)
                        RemoteData.Send(_sockTcp, RemoteDataKind.CmdOption, optionStr);
                    }
                }
            //�u�c�[���v���j���[�̏ꍇ
            }else if (cmd.IndexOf("Tool_") == 0){
                var oneTool = _kernel.ListTool.Get(cmd.Substring(5));
                if (oneTool != null) {
                    _toolDlg = oneTool.CreateDlg(_sockTcp);
                    _toolDlg.ShowDialog();
                    _toolDlg.Dispose();
                    _toolDlg = null;
                }
            //�u�N���^��~�v�̏ꍇ
            } else if (cmd.IndexOf("StartStop_") == 0) {
                string nameTag = cmd.Substring(10);
                if (nameTag == "Restart") {
                    if (_sockTcp != null) {
                        //�u�ċN���v���j���[�I��(C->S)
                        RemoteData.Send(_sockTcp, RemoteDataKind.CmdRestart, "");
                    }
                }
            }
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                    case 1: return (_kernel.IsJp())?"�����[�g�N���C�A���g���N���ł��܂���i����������܂���j" : "RemoteClient can't start(A lack of parameter)";
                    case 2: return (_kernel.IsJp())?"�����[�g�N���C�A���g���N���ł��܂���i�A�h���X�ɖ�肪����܂��j":"RemoteClient can't start(There is a problem to an address)";
                    case 3: return (_kernel.IsJp())?"�����[�g�N���C�A���g���N���ł��܂���i�|�[�g�ԍ��ɖ�肪����܂��j":"RemoteClient can't start(There is a problem to a port number)";
                    case 4: return (_kernel.IsJp())?"�T�[�o�֐ڑ��ł��܂���":"Can't be connected to a server";
                    case 5: return (_kernel.IsJp())?"�T�[�o�֐ڑ����܂���":"Connected to a server";
                    case 6: return (_kernel.IsJp())?"�����[�g�N���C�A���g�J�n":"RemoteClient started it";
                    case 7: return (_kernel.IsJp())?"�����[�g�N���C�A���g��~":"RemoteClient stopped";
                    case 8: return (_kernel.IsJp())?"�����[�g�T�[�o����ؒf����܂���":"Disconnected to a remote server";
                    case 9: return (_kernel.IsJp())?"�����[�g�N���C�A���g���N���ł��܂���i�|�[�g�ԍ�[�f�[�^�p]�ɖ�肪����܂��j":"RemoteClient can't start(There is a problem to a port number [data port])";
                    case 10: return (_kernel.IsJp())?"���O�C��":"Login";
                    case 11: return (_kernel.IsJp()) ? "�����ȃf�[�^�ł�" : "invalid data";
            }
            return "unknown";
        }
        
        public string ShowBrowseDlg(CtrlType ctrlType) {
            string resultStr = null;
            _browseDlg = new BrowseDlg(_kernel, _sockTcp, ctrlType);
            _browseDlg.Init();
            if(DialogResult.OK == _browseDlg.ShowDialog()) {
                resultStr = _browseDlg.Result;
            }
            _browseDlg = null;
            return resultStr;
        }
    }
}
