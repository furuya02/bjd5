using System;
using System.Text;
using System.Threading;
using System.IO;

using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using Bjd.server;

namespace FtpServer{


    public partial class Server : OneServer
    {

        private readonly String _bannerMessage;
        private readonly ListUser _listUser;
        private readonly ListMount _listMount;

        public Server(Kernel kernel, Conf conf, OneBind oneBind) : base(kernel, conf, oneBind){

            _bannerMessage = kernel.ChangeTag((String) Conf.Get("bannerMessage"));
            //���[�U���
            _listUser = new ListUser((Dat) Conf.Get("user"));
            //���z�t�H���_
            _listMount = new ListMount((Dat) Conf.Get("mountList"));


        }

        protected override void OnStopServer(){

        }


        protected override bool OnStartServer(){
            return true;
        }


        protected override void OnSubThread(SockObj sockObj){
            //�Z�b�V�������Ƃ̏��
            var session = new Session((SockTcp) sockObj);

            //���̃R�l�N�V�����̊ԁA�P�ÂC���N�����g���Ȃ���g�p�����
            //�{���́A�ؒf�����|�[�g�ԍ��͍ė��p�\�Ȃ̂ŁA�C���N�������g�̕K�v�͖������A
            //�Z���Ԃōė��p���悤�Ƃ���ƃG���[����������ꍇ������̂ŁA���������ړI�ŃC���N�������g���Ďg�p���Ă���

            //�O���[�e�B���O���b�Z�[�W�̑��M
            session.StringSend(string.Format("220 {0}", _bannerMessage));

            //�R�l�N�V������p�����邩�ǂ����̃t���O
            var result = true;

            while (IsLife() && result){
                //���̃��[�v�͍ŏ��ɃN���C�A���g����̃R�}���h��P�s��M���A�Ō�ɁA
                //sockCtrl.LineSend(resStr)�Ń��X�|���X������s��
                //continue��w�肵���ꍇ�́A���X�|���X��Ԃ����Ɏ��̃R�}���h��M�ɓ���i��O�����p�j
                //break��w�肵���ꍇ�́A�R�l�N�V�����̏I����Ӗ�����iQUIT ABORT �y�уG���[�̏ꍇ�j

                Thread.Sleep(0);

                var cmd = recvCmd(session.SockCtrl);
                if (cmd == null){
                    //�ؒf����Ă���
                    break;
                }

                if (cmd.Str == ""){
                    session.StringSend("500 Invalid command: try being more creative.");
                    //��M�ҋ@��
                    //Thread.Sleep(100);
                    continue;
                }

                //�R�}���h������̉��
                //var ftpCmd = (FtpCmd) Enum.Parse(typeof (FtpCmd), cmd.CmdStr);
                var ftpCmd = FtpCmd.Unknown;
                foreach (FtpCmd n in Enum.GetValues(typeof(FtpCmd))) {
                    if (n.ToString().ToUpper() != cmd.CmdStr.ToUpper())
                        continue;
                    ftpCmd = n;
                    break;
                }
                
                
                //FtpCmd ftpCmd = FtpCmd.parse(cmd.CmdStr);
                var param = cmd.ParamStr;

                //SYST�R�}���h���L�����ǂ����̔��f
                if (ftpCmd == FtpCmd.Syst){
                    if (!(bool) Conf.Get("useSyst")){
                        ftpCmd = FtpCmd.Unknown;
                    }
                }
                //�R�}���h�������ȏꍇ�̏���
                if (ftpCmd == FtpCmd.Unknown){
                    //session.StringSend("502 Command not implemented.");
                    session.StringSend("500 Command not understood.");
                }

                //QUIT�͂��ł�󂯕t����
                if (ftpCmd == FtpCmd.Quit){
                    session.StringSend("221 Goodbye.");
                    break;
                }

                if (ftpCmd == FtpCmd.Abor){
                    session.StringSend("250 ABOR command successful.");
                    break;
                }

                //			//����́A���O�C���������󂯕t���Ȃ��R�}���h����H
                //			//RNFR�Ŏw�肳�ꂽ�p�X�̖�����
                //			if (ftpCmd != FtpCmd.Rnfr) {
                //				session.setRnfrName("");
                //			}

                // �R�}���h�g�ւ�
                if (ftpCmd == FtpCmd.Cdup){
                    param = "..";
                    ftpCmd = FtpCmd.Cwd;
                }

                //�s���A�N�Z�X�Ώ� �p�����[�^�ɋɒ[�ɒ���������𑗂荞�܂ꂽ�ꍇ
                if (param.Length > 128){
                    Logger.Set(LogKind.Secure, session.SockCtrl, 1, string.Format("{0} Length={1}", ftpCmd, param.Length));
                    break;
                }

                //�f�t�H���g�̃��X�|���X������
                //���������ׂĒʉ߂��Ă��܂����ꍇ�A���̕����񂪕Ԃ����
                //String resStr2 = string.Format("451 {0} error", ftpCmd);

                // ���O�C���O�̏���
                if (session.CurrentDir == null){
                    //ftpCmd == FTP_CMD.PASS
                    //������
                    //PASS�̑O��USER�R�}���h��K�v�Ƃ���
                    //sockCtrl.LineSend("503 Login with USER first.");

                    if (ftpCmd == FtpCmd.User){
                        if (param == ""){
                            session.StringSend(string.Format("500 {0}: command requires a parameter.", ftpCmd.ToString().ToUpper()));
                            continue;
                        }
                        result = JobUser(session, param);
                    } else if (ftpCmd == FtpCmd.Pass){
                        result = JobPass(session, param);
                    } else{
                        //USER�APASS�ȊO�̓G���[��Ԃ�
                        session.StringSend("530 Please login with USER and PASS.");
                    }
                    // ���O�C����̏���
                } else{
                    // �p�����[�^�̊m�F(�p�����[�^�������ꍇ�̓G���[��Ԃ�)
                    if (param == ""){
                        if (ftpCmd == FtpCmd.Cwd || ftpCmd == FtpCmd.Type || ftpCmd == FtpCmd.Mkd || ftpCmd == FtpCmd.Rmd || ftpCmd == FtpCmd.Dele || ftpCmd == FtpCmd.Port || ftpCmd == FtpCmd.Rnfr || ftpCmd == FtpCmd.Rnto || ftpCmd == FtpCmd.Stor || ftpCmd == FtpCmd.Retr){
                            //session.StringSend("500 command not understood:");
                            session.StringSend(string.Format("500 {0}: command requires a parameter.", ftpCmd.ToString().ToUpper()));
                            continue;
                        }
                    }

                    // �f�[�^�R�l�N�V�����������ƃG���[�ƂȂ�R�}���h
                    if (ftpCmd == FtpCmd.Nlst || ftpCmd == FtpCmd.List || ftpCmd == FtpCmd.Stor || ftpCmd == FtpCmd.Retr){
                        if (session.SockData == null || session.SockData.SockState !=Bjd.sock.SockState.Connect){
                            session.StringSend("226 data connection close.");
                            continue;
                        }
                    }
                    // ���[�U�̃A�N�Z�X���ɃG���[�ƂȂ�R�}���h
                    if (session.OneUser != null){
                        if (session.OneUser.FtpAcl == FtpAcl.Down){
                            if (ftpCmd == FtpCmd.Stor || ftpCmd == FtpCmd.Dele || ftpCmd == FtpCmd.Rnfr || ftpCmd == FtpCmd.Rnto || ftpCmd == FtpCmd.Rmd || ftpCmd == FtpCmd.Mkd){
                                session.StringSend("550 Permission denied.");
                                continue;
                            }
                        } else if (session.OneUser.FtpAcl == FtpAcl.Up){
                            if (ftpCmd == FtpCmd.Retr || ftpCmd == FtpCmd.Dele || ftpCmd == FtpCmd.Rnfr || ftpCmd == FtpCmd.Rnto || ftpCmd == FtpCmd.Rmd || ftpCmd == FtpCmd.Mkd){
                                session.StringSend("550 Permission denied.");
                                continue;
                            }
                        }
                    }

                    // ���O�C����(�F�؊����j���́AUSER�APASS ��󂯕t���Ȃ�
                    if (ftpCmd == FtpCmd.User || ftpCmd == FtpCmd.Pass){
                        session.StringSend("530 Already logged in.");
                        continue;
                    }

                    if (ftpCmd == FtpCmd.Noop){
                        session.StringSend("200 NOOP command successful.");
                    } else if (ftpCmd == FtpCmd.Pwd || ftpCmd == FtpCmd.Xpwd){
                        session.StringSend(string.Format("257 \"{0}\" is current directory.", session.CurrentDir.GetPwd()));
                    } else if (ftpCmd == FtpCmd.Cwd){
                        result = JobCwd(session, param);
                    } else if (ftpCmd == FtpCmd.Syst){
                        var os = Environment.OSVersion;
                        session.StringSend(string.Format("215 {0}", os.VersionString));
                    } else if (ftpCmd == FtpCmd.Type){
                        result = JobType(session, param);
                    } else if (ftpCmd == FtpCmd.Mkd || ftpCmd == FtpCmd.Rmd || ftpCmd == FtpCmd.Dele){
                        result = JobDir(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Nlst || ftpCmd == FtpCmd.List){
                        result = JobNlist(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Port || ftpCmd == FtpCmd.Eprt){
                        result = JobPort(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Pasv || ftpCmd == FtpCmd.Epsv){
                        result = JobPasv(session, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Rnfr){
                        result = jobRnfr(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Rnto){
                        result = JobRnto(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Stor){
                        result = JobStor(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Retr){
                        result = JobRetr(session, param);
                    }
                }
            }
            //���O�C�����Ă���ꍇ�́A���O�A�E�g�̃��O��o�͂���
            if (session.CurrentDir != null){
                //logout
                Logger.Set(LogKind.Normal, session.SockCtrl, 13, string.Format("{0}", session.OneUser.UserName));
            }
            session.SockCtrl.Close();
            if (session.SockData != null){
                session.SockData.Close();
            }
        }

        private static bool JobUser(Session session, String userName){

            //���M���ꂽ���[�U����L������
            //���[�U�����݂��邩�ǂ����́APASS�R�}���h�̎��_�ŕ]�������
            session.UserName = userName;

            //���[�U���̗L���E�����Ɋ֌W�Ȃ��p�X���[�h�̓��͂𑣂�
            session.StringSend(string.Format("331 Password required for {0}.", userName));
            return true;

        }

        private bool JobPass(Session session, String password){

            //�܂�USER�R�}���h���������Ă��Ȃ��ꍇ
            if (session.UserName == null){
                session.StringSend("503 Login with USER first.");
                return true;
            }

            //���[�U��񌟍�
            session.OneUser = _listUser.Get(session.UserName);

            if (session.OneUser == null){
                //�����ȃ��[�U�̏ꍇ
                Logger.Set(LogKind.Secure, session.SockCtrl, 14, string.Format("USER:{0} PASS:{1}", session.UserName, password));
            } else{
                //�p�X���[�h�m�F
                bool success = false;
                // *�̏ꍇ�AAnonymous�ڑ��Ƃ��ď�������
                if (session.OneUser.Password == "*"){
                    //oneUser.UserName = string.Format("{0}(ANONYMOUS)",oneUser.UserName);
                    Logger.Set(LogKind.Normal, session.SockCtrl, 5, string.Format("{0}(ANONYMOUS) {1}", session.OneUser.UserName, password));
                    success = true;
                } else if (session.OneUser.Password == password){
                    Logger.Set(LogKind.Secure, session.SockCtrl, 6, string.Format("{0}", session.OneUser.UserName));
                    success = true;
                }

                if (success){
                    //�ȉ��A�p�X���[�h�F�؂ɐ��������ꍇ�̏���
                    //�z�[���f�B���N�g���̑��݊m�F
                    //�T�[�o�N���i�^�c�j���Ƀf�B���N�g�����폜����Ă���\��������̂ŁA���̎��_�Ŋm�F����
                    if (Util.Exists(session.OneUser.HomeDir) != ExistsKind.Dir){
                        //�z�[���f�B���N�g�������݂��܂���i�������p���ł��Ȃ����ߐؒf���܂���
                        Logger.Set(LogKind.Error, session.SockCtrl, 2, string.Format("userName={0} hoemDir={1}", session.OneUser.UserName, session.OneUser.HomeDir));
                        return false;
                    }

                    //���O�C������ �i�J�����g�f�B���N�g���́A�z�[���f�B���N�g���ŏ����������j
                    session.CurrentDir = new CurrentDir(session.OneUser.HomeDir, _listMount);

                    session.StringSend(string.Format("230 User {0} logged in.", session.UserName));
                    return true;
                }
                //�ȉ��F�؎��s����
                Logger.Set(LogKind.Secure, session.SockCtrl, 15, string.Format("USER:{0} PASS:{1}", session.UserName, password));
            }
            var reservationTime = (int) Conf.Get("reservationTime");

            //�u���[�g�t�H�[�X�h�~�̂��߂̃E�G�C�g(5�b)
            for (int i = 0; i < reservationTime/100 && IsLife(); i++){
                Thread.Sleep(100);
            }
            //�F�؂Ɏ��s�����ꍇ�̏���
            session.StringSend("530 Login incorrect.");
            return true;

        }

        private bool JobType(Session session, String param){
            String resStr;
            switch (param.ToUpper()[0]){
                case 'A':
                    session.FtpType = FtpType.Ascii;
                    resStr = "200 Type set 'A'";
                    break;
                case 'I':
                    session.FtpType = FtpType.Binary;
                    resStr = "200 Type set 'I'";
                    break;
                default:
                    resStr = "500 command not understood.";
                    break;
            }
            session.StringSend(resStr);
            return true;
        }

        private static bool JobCwd(Session session, String param){
            if (session.CurrentDir.Cwd(param)){
                session.StringSend("250 CWD command successful.");
            } else{
                session.StringSend(string.Format("550 {0}: No such file or directory.", param));
            }
            return true;
        }

        private bool JobDir(Session session, String param, FtpCmd ftpCmd){
            bool isDir = !(ftpCmd == FtpCmd.Dele);
            int retCode = -1;
            //�p�����[�^����V�����p�X���𐶐�����
            var path = session.CurrentDir.CreatePath(null, param, isDir);
            if (path == null){
                //TODO �G���[���O�擾�͂��K�v
            } else{
                if (ftpCmd == FtpCmd.Mkd){
                    //�f�B���N�g���͖�����?
                    if (!Directory.Exists(path)) {//�f�B���N�g���͖�����?
                        Directory.CreateDirectory(path);
                        retCode = 257;
                    }
                } else if (ftpCmd == FtpCmd.Rmd) {
                    if (Directory.Exists(path)) {//�f�B���N�g���͗L�邩?
                        try{
                            Directory.Delete(path);
                            retCode = 250;
                        } catch (Exception) {
                    
                        }
                    }
                } else if (ftpCmd == FtpCmd.Dele) {
                    if (File.Exists(path)) {//�t�@�C���͗L�邩?
                        File.Delete(path);
                        retCode = 250;
                    }
                }

                if (retCode != -1){
                    //����
                    Logger.Set(LogKind.Normal, session.SockCtrl, 7, string.Format("User:{0} Cmd:{1} Path:{2}", session.OneUser.UserName, ftpCmd, path));
                    session.StringSend(string.Format("{0} {1} command successful.", retCode, ftpCmd));
                    return true;
                }
                //���s
                //�R�}���h�����ŃG���[���������܂���
                Logger.Set(LogKind.Error, session.SockCtrl, 3, string.Format("User:{0} Cmd:{1} Path:{2}", session.OneUser.UserName, ftpCmd, path));
            }
            session.StringSend(string.Format("451 {0} error.", ftpCmd));
            return true;
        }

        private bool JobNlist(Session session, String param, FtpCmd ftpCmd){
            // �Z�k���X�g���ǂ���
            var wideMode = (ftpCmd == FtpCmd.List);
            var mask = "*.*";

            //�p�����[�^���w�肳��Ă���ꍇ�A�}�X�N��擾����
            if (param != ""){
                foreach (var p in param.Split(' ')){
                    if (p == ""){
                        continue;
                    }
                    if (p.ToUpper().IndexOf("-L") == 0){
                        wideMode = true;
                    }else if(p.ToUpper().IndexOf("-A") == 0) {
                        wideMode = true;
                    } else{
                        //���C���h�J�[�h�w��
                        if (p.IndexOf('*') != -1 || p.IndexOf('?') != -1){
                            mask = param;
                        } else{
                            //�t�H���_�w��
                            //Ver5.9.0
                            try {
                                var existsKind = Util.Exists(session.CurrentDir.CreatePath(null, param, false));
                                switch (existsKind) {
                                    case ExistsKind.Dir:
                                        mask = param + "\\*.*";
                                        break;
                                    case ExistsKind.File:
                                        mask = param;
                                        break;
                                    default:
                                        session.StringSend(string.Format("500 {0}: command requires a parameter.", param));
                                        session.SockData = null;
                                        return true;
                                }
                            } catch (Exception ex) {
                                Logger.Set(LogKind.Error, session.SockCtrl, 18,String.Format("param={0} Exception.message={1}",param,ex.Message));
                                session.StringSend(string.Format("500 {0}: command requires a parameter.", param));
                                session.SockData = null;
                                return true;
                            }
                        }
                    }
                }
            }
            session.StringSend(string.Format("150 Opening {0} mode data connection for ls.", session.FtpType.ToString().ToUpper()));
            //�t�@�C���ꗗ�擾
            foreach (var s in session.CurrentDir.List(mask, wideMode)){
                session.SockData.StringSend(s, "Shift-Jis");
            }
            session.StringSend("226 Transfer complete.");

            session.SockData.Close();
            session.SockData = null;
            return true;
        }

        private bool JobPort(Session session, String param, FtpCmd ftpCmd){
            String resStr = "500 command not understood:";

            Ip ip = null;
            int port = 0;

            if (ftpCmd == FtpCmd.Eprt){
                var tmpBuf = param.Split(new[]{'|'},StringSplitOptions.RemoveEmptyEntries);
                if (tmpBuf.Length == 3){
                    port = Convert.ToInt32(tmpBuf[2]);
                    try{
                        ip = new Ip(tmpBuf[1]);
                    } catch (ValidObjException){
                        ip = null;
                    }
                }
                if (ip == null){
                    resStr = "501 Illegal EPRT command.";
                }
            } else{
                var tmpBuf = param.Split(',');
                if (tmpBuf.Length == 6){
                    try{
                        ip = new Ip(tmpBuf[0] + "." + tmpBuf[1] + "." + tmpBuf[2] + "." + tmpBuf[3]);
                    } catch (ValidObjException ){
                        ip = null;
                    }
                    port = Convert.ToInt32(tmpBuf[4]) * 256 + Convert.ToInt32(tmpBuf[5]);
                }
                if (ip == null){
                    resStr = "501 Illegal PORT command.";
                }
            }
            if (ip != null){

                Thread.Sleep(10);
                var sockData = Inet.Connect(Kernel,ip, port, Timeout, null);
                if (sockData != null){
                    resStr = string.Format("200 {0} command successful.", ftpCmd.ToString().ToUpper());
                }
                session.SockData = sockData;
            }
            session.StringSend(resStr);
            return true;

        }

        private bool JobPasv(Session session, FtpCmd ftpCmd){
            var port = session.Port;
            var ip = session.SockCtrl.LocalIp;
            // �f�[�^�X�g���[���̃\�P�b�g�̍쐬
            for (int i = 0; i < 100; i++){
                port++;
                if (port >= 9999){
                    port = 2000;
                }
                //�o�C���h�\���ǂ����̊m�F
                if (SockServer.IsAvailable(Kernel,ip, port)){
                    //����
                    if (ftpCmd == FtpCmd.Epsv){
                        //Java fix Ver5.8.3
                        //session.StringSend(string.Format("229 Entering Extended Passive Mode. (|||{0}|)", port));
                        session.StringSend(string.Format("229 Entering Extended Passive Mode (|||{0}|)", port));
                    } else {
                        var ipStr = ip.ToString();
                        //Java fix Ver5.8.3
                        //session.StringSend(string.Format("227 Entering Passive Mode. ({0},{1},{2})", ipStr.Replace('.',','), port/256, port%256));
                        session.StringSend(string.Format("227 Entering Passive Mode ({0},{1},{2})", ipStr.Replace('.', ','), port / 256, port % 256));
                    }
                    //�w�肵���A�h���X�E�|�[�g�ő҂��󂯂�
                    var sockData = SockServer.CreateConnection(Kernel,ip, port, null, this);
                    if (sockData == null){
                        //�ڑ����s
                        return false;
                    }
                    if (sockData.SockState != Bjd.sock.SockState.Error){
                        //�Z�b�V�������̕ۑ�
                        session.Port = port;
                        session.SockData = sockData;
                        return true;
                    }
                }
            }
            session.StringSend("500 command not understood:");
            return true;
        }

        private bool JobRnto(Session session, String param, FtpCmd ftpCmd){
            //Ver6.0.4
            //if (session.RnfrName != "") {
            if (!string.IsNullOrEmpty(session.RnfrName)) {
                var path = session.CurrentDir.CreatePath(null, param, false);
                
                //Ver6.0.3 �f�B���N�g���g���o�[�T��
                if (path == null) {
                    session.StringSend("550 Permission denied.");
                    return false;
                }

                var existsKind = Util.Exists(path);
                if (existsKind == ExistsKind.Dir){
                    session.StringSend("550 rename: Is a derectory name.");
                    return true;
                }
                if (existsKind == ExistsKind.File){
                    File.Delete(path);
                }
                if (Directory.Exists(session.RnfrName)) {//�ύX�̑Ώۂ��f�B���N�g���ł���ꍇ
                    Directory.Move(session.RnfrName, path);
                } else {//�ύX�̑Ώۂ��t�@�C���ł���ꍇ
                    //Ver6.0.4
                    if (!Directory.Exists(Path.GetDirectoryName(path))){
                        //�w���̃f�B���N�g�������݂��Ȃ��ꍇ�̃G���[                        
                        session.StringSend("550 Permission denied.");
                        return false;
                    }
                    File.Move(session.RnfrName, path);
                }
                Logger.Set(LogKind.Normal, session.SockCtrl, 8, string.Format("{0} {1} -> {2}", session.OneUser.UserName, session.RnfrName, path));
                session.StringSend("250 RNTO command successful.");
                return true;
            }
            session.StringSend(string.Format("451 {0} error.", ftpCmd));
            return true;
        }

        private bool jobRnfr(Session session, String param, FtpCmd ftpCmd){
            var path = session.CurrentDir.CreatePath(null, param, false);
            //Ver6.0.3 �f�B���N�g���g���o�[�T��
            if (path == null) {
                session.StringSend("550 Permission denied.");
                return false;
            }

            if (Util.Exists(path) != ExistsKind.None){
                session.RnfrName = path;
                session.StringSend("350 File exists, ready for destination name.");
                return true;
            }
            session.StringSend(string.Format("451 {0} error.", ftpCmd));
            return true;
        }

        private bool JobStor(Session session, String param, FtpCmd ftpCmd){
            String path = session.CurrentDir.CreatePath(null, param, false);
            
            //Ver6.0.3 �f�B���N�g���g���o�[�T��
            if (path==null){
                session.StringSend("550 Permission denied.");
                return true;
            }
            
            ExistsKind exists = Util.Exists(path);
            if (exists != ExistsKind.Dir){
                //File file = new File(path);
                if (exists == ExistsKind.File){
                    // �A�b�v���[�h���[�U�́A�����̃t�@�C����㏑���ł��Ȃ�
                    if (session.OneUser.FtpAcl == FtpAcl.Up && File.Exists(path)){
                        session.StringSend("550 Permission denied.");
                        return true;
                    }
                }
                //String str = string.Format("150 Opening {0} mode data connection for {1}.", session.getFtpType(), param);
                session.SockCtrl.StringSend(string.Format("150 Opening {0} mode data connection for {1}.", session.FtpType.ToString().ToUpper(), param),"shift-jis");

                //Up start
                Logger.Set(LogKind.Normal, session.SockCtrl, 9, string.Format("{0} {1}", session.OneUser.UserName, param));

                try{
                    int size = RecvBinary(session.SockData, path);
                    session.StringSend("226 Transfer complete.");
                    //Up end
                    Logger.Set(LogKind.Normal, session.SockCtrl, 10, string.Format("{0} {1} {2}bytes", session.OneUser.UserName, param, size));
                } catch (IOException){
                    session.StringSend("426 Transfer abort.");
                    //Up end
                    Logger.Set(LogKind.Error, session.SockCtrl, 17, string.Format("{0} {1}", session.OneUser.UserName, param));
                }

                session.SockData.Close();
                session.SockData = null;

                return true;
            }
            session.StringSend(string.Format("451 {0} error.", ftpCmd));
            return true;
        }

        private bool JobRetr(Session session, String param){
            var path = session.CurrentDir.CreatePath(null, param, false);
            //Ver6.0.3 �f�B���N�g���g���o�[�T��
            if (path == null) {
                session.StringSend("550 Permission denied.");
                return false;
            }

            if (Util.Exists(path) == ExistsKind.File){

                var dirName = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);
                var di = new DirectoryInfo(dirName);
                var files = di.GetFiles(fileName);

                if (files.Length == 1){
                    String str = string.Format("150 Opening {0} mode data connection for {1} ({2} bytes).", session.FtpType.ToString().ToUpper(), param, files[0].Length);
                    session.StringSend(str); //Shift-jis�ł���K�v������H

                    //DOWN start
                    Logger.Set(LogKind.Normal, session.SockCtrl, 11, string.Format("{0} {1}", session.OneUser.UserName, param));
                    try{
                        int size = SendBinary(session.SockData, path);
                        session.StringSend("226 Transfer complete.");
                        //DOWN end
                        Logger.Set(LogKind.Normal, session.SockCtrl, 12, string.Format("{0} {1} {2}bytes", session.OneUser.UserName, param, size));
                    } catch (IOException){
                        session.StringSend("426 Transfer abort.");
                        //DOWN end
                        Logger.Set(LogKind.Error, session.SockCtrl, 16, string.Format("{0} {1}", session.OneUser.UserName, param));
                    }
                    session.SockData.Close();
                    session.SockData = null;

                    return true;
                }
            }
            session.StringSend(string.Format("550 {0}: No such file or directory.", param));
            return true;
        }

        //�t�@�C����M�i�o�C�i���j
        private int RecvBinary(SockTcp sockTcp, String fileName){
            var sb = new StringBuilder();
            sb.Append(string.Format("RecvBinary({0}) ", fileName));

            var fs = new FileStream(fileName, FileMode.Create);
            var bw = new BinaryWriter(fs);
            fs.Seek(0, SeekOrigin.Begin);

            var size = 0;
            const int timeout = 3000;
            while (IsLife()){
                int len = sockTcp.Length();
                if (len < 0){
                    break;
                }
                if (len == 0){
                    if (sockTcp.SockState != Bjd.sock.SockState.Connect){
                        break;
                    }
                    Thread.Sleep(10);
                    continue;
                }
                byte[] buf = sockTcp.Recv(len, timeout, this);
                if (buf.Length != len){
                    throw new IOException("buf.length!=len");
                }
                bw.Write(buf, 0, buf.Length);

                //�g���[�X�\��
                sb.Append(string.Format("Binary={0}byte ", len));
                size += len;

            }
            bw.Flush();
            bw.Close();
            fs.Close();
            
            //noEncode = true; //�o�C�i���ł��鎖���������Ă���
            //Trace(TraceKind.Send, Encoding.ASCII.GetBytes(sb.ToString()), true); //�g���[�X�\��

            return size;
        }

        private int SendBinary(SockTcp sockTcp, String fileName){
            var sb = new StringBuilder();
            sb.Append(string.Format("SendBinary({0}) ", fileName));

            int size = 0;

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)){
                using (var br = new BinaryReader(fs)){
                    var buf = new byte[3000000];
                    while (IsLife()) {
                        var len = br.Read(buf, 0, 3000000);
                        if (len <= 0) {
                            break;
                        }
                        //if (oneSsl != null) {
                        //}else{
                        sockTcp.Send(buf,len);
                        //}
                        //�g���[�X�\��
                        sb.Append(string.Format("Binary={0}byte ", len));
                        size += len;
                    }
                }
            }

            //noEncode = true; //�o�C�i���ł��鎖���������Ă���
            //Trace(TraceKind.Send, Encoding.ASCII.GetBytes(sb.ToString()), true); //�g���[�X�\��
            return size;
        }


        //RemoteServer�ł̂ݎg�p�����
        public override void Append(OneLog oneLog) {

        }

    }
}

