using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;



using Bjd;
using Bjd.acl;
using Bjd.log;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;

namespace Pop3Server {

    partial class Server : OneServer{
        private readonly AttackDb _attackDb; //��������

        //�R���X�g���N�^
        public Server(Kernel kernel, Conf conf, OneBind oneBind)
            : base(kernel, conf, oneBind){

            //Ver5.8.9
            if (kernel.RunMode == RunMode.Normal || kernel.RunMode == RunMode.Service){
                //���[���{�b�N�X�̏�������Ԋm�F
                if (kernel.MailBox == null || !kernel.MailBox.Status){
                    Logger.Set(LogKind.Error, null, 4, "");
                }
            }

            var useAutoAcl = (bool) Conf.Get("useAutoAcl"); // ACL���ۃ��X�g�֎����ǉ�����
            if (!useAutoAcl)
                return;
            var max = (int) Conf.Get("autoAclMax"); // �F�؎��s���i��j
            var sec = (int) Conf.Get("autoAclSec"); // �Ώۊ���(�b)
            _attackDb = new AttackDb(sec, max);
        }

        //�����[�g����i�f�[�^�̎擾�j
        public override string Cmd(string cmdStr){
            return "";
        }

        private enum Pop3LoginState{
            User = 0, //USER/APOP�҂����
            Pass = 1, //�p�X���[�h�҂����
            Login = 2 //���O�C����

        }

//        [DllImport("kernel32.dll")]
//        private static extern int GetCurrentThreadId();

        protected override bool OnStartServer(){
            return true;
        }

        protected override void OnStopServer(){
        }

        //�ڑ��P�ʂ̏���
        protected override void OnSubThread(SockObj sockObj){

            var sockTcp = (SockTcp) sockObj;

            var pop3LoginState = Pop3LoginState.User;

            var authType = (int) Conf.Get("authType"); // 0=USER/PASS 1=APOP 2=����
            var useChps = (bool) Conf.Get("useChps"); //�p�X���[�h�ύX[CPHS]�̎g�p�E���g�p


            string user = null;

            //�O���[�e�B���O���b�Z�[�W�̕\��
            var bannerMessage = Kernel.ChangeTag((string) Conf.Get("bannerMessage"));

            var authStr = ""; //APOP�p�̔F�ؕ�����
            if (authType == 0){
//USER/PASS
                sockTcp.AsciiSend("+OK " + bannerMessage);
            }
            else{
//APOP
                authStr = APop.CreateAuthStr(Kernel.ServerName);
                sockTcp.AsciiSend("+OK " + bannerMessage + " " + authStr);

            }

            //���[���{�b�N�X�Ƀ��O�C�����āA���̎��_�̃��[�����X�g��擾����
            //���ۂ̃��[���̍폜�́AQUIT��M���ɁAmailList.Update()�ŏ�������
            MessageList messageList = null;

            while (IsLife()){
                //���̃��[�v�͍ŏ��ɃN���C�A���g����̃R�}���h��P�s��M���A�Ō�ɁA
                //sockCtrl.LineSend(resStr)�Ń��X�|���X������s��
                //continue��w�肵���ꍇ�́A���X�|���X��Ԃ����Ɏ��̃R�}���h��M�ɓ���i��O�����p�j
                //break��w�肵���ꍇ�́A�R�l�N�V�����̏I����Ӗ�����iQUIT ABORT �y�уG���[�̏ꍇ�j

                Thread.Sleep(0);

                var str = "";
                var cmdStr = "";

                var remoteIp = new Ip(sockTcp.RemoteAddress.Address.ToString());

                var paramStr2 = "";
                if (!RecvCmd(sockTcp, ref str, ref cmdStr, ref paramStr2))
                    break; //�ؒf���ꂽ

                if (str == "waiting"){
                    Thread.Sleep(100); //��M�ҋ@��
                    continue;
                }

                //�R�}���h������̉��
                var cmd = Pop3Cmd.Unknown;
                foreach (Pop3Cmd n in Enum.GetValues(typeof (Pop3Cmd))){
                    if (n.ToString().ToUpper() == cmdStr.ToUpper()){
                        cmd = n;
                        break;
                    }
                }
                if (cmd == Pop3Cmd.Unknown){
//�����R�}���h
                    goto UNKNOWN;
                }

                //�p�����[�^����
                var paramList = new List<string>();
                if (paramStr2 != null){
                    paramList.AddRange(
                        paramStr2.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim(' ')));
                }

                //���ł�󂯕t����
                if (cmd == Pop3Cmd.Quit){
                    if (messageList != null){
                        messageList.Update(); //�����ō폜���������s�����
                    }
                    goto END;
                }

                if (pop3LoginState == Pop3LoginState.User){

                    if (cmd == Pop3Cmd.User && (authType == 0 || authType == 2)){
                        if (paramList.Count < 1){
                            goto FEW;
                        }
                        user = paramList[0];
                        pop3LoginState = Pop3LoginState.Pass;
                        sockTcp.AsciiSend(string.Format("+OK Password required for {0}.", user));
                    }
                    else if (cmd == Pop3Cmd.Apop && (authType == 1 || authType == 2)){
                        //APOP
                        if (paramList.Count < 2){
                            goto FEW;
                        }
                        user = paramList[0];

                        //�F��(APOP�Ή�)
                        var success = APop.Auth(user, Kernel.MailBox.GetPass(user), authStr, paramList[1]);
                        //var success = APopAuth(user, authStr, paramList[1]);
                        AutoDeny(success, remoteIp); //�u���[�g�t�H�[�X�΍�
                        if (success){
                            if (
                                !Login(sockTcp, ref pop3LoginState, ref messageList, user,
                                       new Ip(sockObj.RemoteAddress.Address.ToString())))
                                goto END;
                        }
                        else{
                            AuthError(sockTcp, user, paramList[1]);
                            goto END;
                        }
                    }
                    else{
                        goto UNKNOWN;
                    }
                }
                else if (pop3LoginState == Pop3LoginState.Pass){
                    if (cmd != Pop3Cmd.Pass){
                        goto UNKNOWN;
                    }

                    if (paramList.Count < 1){
                        goto FEW;
                    }
                    string pass = paramList[0];

                    var success = Kernel.MailBox.Auth(user, pass); //�F��
                    AutoDeny(success, remoteIp); //�u���[�g�t�H�[�X�΍�
                    if (success){
//�F��
                        if (
                            !Login(sockTcp, ref pop3LoginState, ref messageList, user,
                                   new Ip(sockObj.RemoteAddress.Address.ToString())))
                            goto END;
                    }
                    else{
                        AuthError(sockTcp, user, pass);
                        goto END;
                    }
                }
                else if (pop3LoginState == Pop3LoginState.Login){

                    if (cmd == Pop3Cmd.Dele || cmd == Pop3Cmd.Retr){
                        if (paramList.Count < 1)
                            goto FEW;
                    }
                    if (cmd == Pop3Cmd.Top){
                        if (paramList.Count < 2)
                            goto FEW;
                    }

                    int index = -1; //���[���A��
                    if (cmd != Pop3Cmd.Chps && 1 <= paramList.Count){
                        try{
                            index = Convert.ToInt32(paramList[0]);
                        }
                        catch (Exception){
                            sockTcp.AsciiSend("-ERR Invalid message number.");
                            continue;
                        }

                        index--;
                        if (index < 0 || messageList.Max <= index){
                            sockTcp.AsciiSend(string.Format("-ERR Message {0} does not exist.", index + 1));
                            continue;
                        }
                    }
                    int count = -1; //TOP �s��
                    if (cmd != Pop3Cmd.Chps && 2 <= paramList.Count){
                        try{
                            count = Convert.ToInt32(paramList[1]);
                        }
                        catch (Exception){
                            sockTcp.AsciiSend("-ERR Invalid line number.");
                            continue;
                        }
                        if (count < 0){
                            sockTcp.AsciiSend(string.Format("-ERR Linenumber range over: {0}", count));
                            continue;
                        }
                    }

                    if (cmd == Pop3Cmd.Noop){
                        sockTcp.AsciiSend("+OK");
                        continue;
                    }
                    if (cmd == Pop3Cmd.Stat){
                        sockTcp.AsciiSend(string.Format("+OK {0} {1}", messageList.Count, messageList.Size));
                        continue;
                    }
                    if (cmd == Pop3Cmd.Rset){
                        messageList.Rset();
                        sockTcp.AsciiSend(string.Format("+OK {0} has {1} message ({2} octets).", user, messageList.Count,
                                                        messageList.Size));
                        continue;
                    }
                    if (cmd == Pop3Cmd.Dele){
                        if (messageList[index].Del){
                            sockTcp.AsciiSend(string.Format("-ERR Message {0} has been markd for delete.", index + 1));
                            continue;
                        }
                        messageList[index].Del = true;
                        //Ver5.0.3
                        //sockTcp.AsciiSend(string.Format("+OK {0} octets",messageList.Size),OPERATE_CRLF.YES);
                        sockTcp.AsciiSend(string.Format("+OK {0} octets", messageList[index].Size));
                        continue;
                    }
                    if (cmd == Pop3Cmd.Uidl || cmd == Pop3Cmd.List){
                        if (paramList.Count < 1){
                            sockTcp.AsciiSend(string.Format("+OK {0} message ({1} octets)", messageList.Count,
                                                            messageList.Size));
                            for (int i = 0; i < messageList.Max; i++){
                                if (!messageList[i].Del){
                                    if (cmd == Pop3Cmd.Uidl)
                                        sockTcp.AsciiSend(string.Format("{0} {1}", i + 1, messageList[i].Uid));
                                    else //LIST
                                        sockTcp.AsciiSend(string.Format("{0} {1}", i + 1, messageList[i].Size));
                                }
                            }
                            sockTcp.AsciiSend(".");
                            continue;
                        }
                        if (cmd == Pop3Cmd.Uidl)
                            sockTcp.AsciiSend(string.Format("+OK {0} {1}", index + 1, messageList[index].Uid));
                        else //LIST
                            sockTcp.AsciiSend(string.Format("+OK {0} {1}", index + 1, messageList[index].Size));
                    }
                    if (cmd == Pop3Cmd.Top || cmd == Pop3Cmd.Retr){
                        //OneMessage oneMessage = messageList[index];
                        sockTcp.AsciiSend(string.Format("+OK {0} octets", messageList[index].Size));
                        if (!messageList[index].Send(sockTcp, count)){
//���[���̑��M
                            break;
                        }
                        MailInfo mailInfo = messageList[index].GetMailInfo();
                        Logger.Set(LogKind.Normal, sockTcp, 5, mailInfo.ToString());

                        sockTcp.AsciiSend(".");
                        continue;

                    }
                    if (cmd == Pop3Cmd.Chps){
                        if (!useChps)
                            goto UNKNOWN;
                        if (paramList.Count < 1)
                            goto FEW;

                        var password = paramList[0];

                        //�Œᕶ����
                        var minimumLength = (int) Conf.Get("minimumLength");
                        if (password.Length < minimumLength){
                            sockTcp.AsciiSend("-ERR The number of letter is not enough.");
                            continue;
                        }
                        //���[�U���Ɠ���̃p�X���[�h������Ȃ�
                        if ((bool) Conf.Get("disableJoe")){
                            if (user.ToUpper() == password.ToUpper()){
                                sockTcp.AsciiSend("-ERR Don't admit a JOE.");
                                continue;
                            }
                        }

                        //�K���܂܂Ȃ���΂Ȃ�Ȃ������̃`�F�b�N
                        bool checkNum = false;
                        bool checkSmall = false;
                        bool checkLarge = false;
                        bool checkSign = false;
                        foreach (char c in password){
                            if ('0' <= c && c <= '9')
                                checkNum = true;
                            else if ('a' <= c && c <= 'z')
                                checkSmall = true;
                            else if ('A' <= c && c <= 'Z')
                                checkLarge = true;
                            else
                                checkSign = true;
                        }
                        if (((bool) Conf.Get("useNum") && !checkNum) ||
                            ((bool) Conf.Get("useSmall") && !checkSmall) ||
                            ((bool) Conf.Get("useLarge") && !checkLarge) ||
                            ((bool) Conf.Get("useSign") && !checkSign)){
                            sockTcp.AsciiSend("-ERR A required letter is not included.");
                            continue;
                        }
                        var conf = new Conf(Kernel.ListOption.Get("MailBox"));
                        if(!Chps.Change(user, password, Kernel.MailBox, conf)){
                        //if (!Kernel.MailBox.Chps(user, password, conf)){
                            sockTcp.AsciiSend("-ERR A problem occurred to a mailbox.");
                            continue;
                        }
                        sockTcp.AsciiSend("+OK Password changed.");
                    }
                }
                continue;

                UNKNOWN:
                sockTcp.AsciiSend(string.Format("-ERR Invalid command."));
                continue;

                FEW:
                sockTcp.AsciiSend(string.Format("-ERR Too few arguments for the {0} command.", str));
                continue;

                END:
                sockTcp.AsciiSend(string.Format("+OK Pop Server at {0} signing off.", Kernel.ServerName));
                break;
            }
            Kernel.MailBox.Logout(user);
            if (sockTcp != null)
                sockTcp.Close();

        }

        bool Login(SockTcp sockTcp,ref Pop3LoginState mode,ref MessageList messageList,string user,Ip addr) {
            
            //var folder = Kernel.MailBox.Login(user, addr);
            if(!Kernel.MailBox.Login(user, addr)){
                Logger.Set(LogKind.Secure,sockTcp,1,string.Format("user={0}",user));
                sockTcp.AsciiSend("-ERR Double login");
                return false;
            }
            var folder = string.Format("{0}\\{1}", Kernel.MailBox.Dir, user);
            messageList = new MessageList(folder);//������

            //if (kernel.MailBox.Login(user, addr)) {//POP before SMTP�̂��߂ɁA�Ō�̃��O�C���A�h���X��ۑ�����
            mode = Pop3LoginState.Login;
            Logger.Set(LogKind.Normal,sockTcp,2,string.Format("User {0} from {1}[{2}]",user,sockTcp.RemoteHostname,sockTcp.RemoteAddress.Address));

            // LOGIN
            //dfList = kernel.MailBox.GetDfList(user);
            sockTcp.AsciiSend(string.Format("+OK {0} has {1} message ({2} octets).",user,messageList.Count,messageList.Size));
            return true;
        }
        void AuthError(SockTcp sockTcp,string user,string pass) {

            Logger.Set(LogKind.Secure,sockTcp,3,string.Format("user={0} pass={1}",user,pass));
            // �F�؂̃G���[�͂����ɕԓ���Ԃ��Ȃ�
            var authTimeout = (int)Conf.Get("authTimeout");
            for (int i = 0; i < (authTimeout * 10) && IsLife(); i++) {
                Thread.Sleep(100);
            }
            sockTcp.AsciiSend(string.Format("-ERR Password supplied for {0} is incorrect.",user));
        }

        void AutoDeny(bool success, Ip remoteIp) {
            if (_attackDb == null)
                return;
            //�f�[�^�x�[�X�ւ̓o�^
            if (!_attackDb.IsInjustice(success, remoteIp))
                return;
            //�u���[�g�t�H�[�X�A�^�b�N
            if (!AclList.Append(remoteIp))
                return; //ACL�������ېݒ�(�u������v�ɐݒ肳��Ă���ꍇ�A�@�\���Ȃ�)
            //�ǉ��ɐ��������ꍇ�A�I�v�V���������������
            var d = (Dat)Conf.Get("acl");
            var name = string.Format("AutoDeny-{0}", DateTime.Now);
            var ipStr = remoteIp.ToString();
            d.Add(true, string.Format("{0}\t{1}", name, ipStr));
            Conf.Set("acl", d);
            Conf.Save(Kernel.IniDb);
            //OneOption.SetVal("acl", d);
            //OneOption.Save(OptionIni.GetInstance());
            Logger.Set(LogKind.Secure, null, 9000055, string.Format("{0},{1}", name, ipStr));
        }
        //RemoteServer�ł̂ݎg�p�����
        public override void Append(OneLog oneLog) {

        }

    }
}
