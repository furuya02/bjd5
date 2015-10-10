using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.remote;
using Bjd.server;
using Bjd.sock;
using Bjd.util;


namespace RemoteServer {

    partial class Server : OneServer {
        readonly Queue<OneRemoteData> _queue = new Queue<OneRemoteData>();

        //�R���X�g���N�^
        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel,conf,oneBind) {

        }
        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }
        //�ڑ��P�ʂ̏���
        SockTcp _sockTcp;//�����Ő錾����ꍇ�A�}���`�X���b�h�ł͎g�p�ł��Ȃ�
        override protected void OnSubThread(SockObj sockObj) {
            _sockTcp = (SockTcp)sockObj;

            //*************************************************************
            // �p�X���[�h�F��
            //*************************************************************
            var password = (string)Conf.Get("password");
            if (password == ""){
                Logger.Set(LogKind.Normal,_sockTcp,5,"");
            }else{//�p�X���[�h�F�؂��K�v�ȏꍇ
                var challengeStr = Inet.ChallengeStr(10);//�`�������W������̐���

                RemoteData.Send(_sockTcp, RemoteDataKind.DatAuth, challengeStr);

                //�p�X���[�h�̉����҂�
                var success = false;//Ver5.0.0-b14
                while (IsLife() && _sockTcp.SockState == Bjd.sock.SockState.Connect) {
                    var o = RemoteData.Recv(_sockTcp,this);
                    if(o!=null){
                        if (o.Kind == RemoteDataKind.CmdAuth) {

                            //�n�b�V��������̍쐬�iMD5�j
                            var md5Str = Inet.Md5Str(password + challengeStr);
                            if(md5Str != o.Str) {
                                Logger.Set(LogKind.Secure,_sockTcp,4,"");

                                //DOS�΍� 3�b�Ԃ͎��̐ڑ���󂯕t���Ȃ�
                                //for (int i = 0; i < 30 && life; i++) {
                                //    Thread.Sleep(100);
                                //}
                                //tcpObj.Close();//���̐ڑ��͔j�������
                                //return;
                            } else {
                                success = true;//Ver5.0.0-b14
                            }
                            break;
                        }
                    }else{
                        Thread.Sleep(500);
                    }
                }
		        //Ver5.0.0-b14
		        if(!success) {
                    //�F�؎��s�i�p�X���[�h�L�����Z���E�p�X���[�h�Ⴂ�E�����ؒf�j
                    //DOS�΍� 3�b�Ԃ͎��̐ڑ���󂯕t���Ȃ�
                    for(var i = 0;i < 30 && IsLife();i++) {
                        Thread.Sleep(100);
                    }
                    _sockTcp.Close();//���̐ڑ��͔j�������
                    return;
                }
            }

            //*************************************************************
            // �F�؊���
            //*************************************************************
            
            Logger.Set(LogKind.Normal,_sockTcp,1,string.Format("address={0}",_sockTcp.RemoteAddress.Address));

            //�o�[�W����/���O�C�������̑��M
            RemoteData.Send(_sockTcp, RemoteDataKind.DatVer, Kernel.Ver.VerData());

            //kernel.LocalAddress��Remote���Ő�������
            RemoteData.Send(_sockTcp, RemoteDataKind.DatLocaladdress, LocalAddress.GetInstance().RemoteStr());

            //�I�v�V�����̑��M
            var optionFileName = string.Format("{0}\\Option.ini",Kernel.ProgDir());
            string optionStr;
            using (var sr = new StreamReader(optionFileName, Encoding.GetEncoding("Shift_JIS"))) {
                optionStr = sr.ReadToEnd();
                sr.Close();
            }
            RemoteData.Send(_sockTcp, RemoteDataKind.DatOption, optionStr);
            Kernel.RemoteConnect = new Bjd.remote.RemoteConnect(_sockTcp);//�����[�g�N���C�A���g�ڑ��J�n
            Kernel.View.SetColor();//�E�C���h�F�̏�����

            while (IsLife() && _sockTcp.SockState == Bjd.sock.SockState.Connect) {
                var o = RemoteData.Recv(_sockTcp,this);
                if (o==null)
                    continue;
                //�R�}���h�́A���ׂăL���[�Ɋi�[����
                _queue.Enqueue(o);
                if (_queue.Count == 0) {
                    GC.Collect();
                    Thread.Sleep(500);
                } else {
                    Cmd(_queue.Dequeue());
                }
            }

            Kernel.RemoteConnect = null;//�����[�g�N���C�A���g�ڑ��I��

            Logger.Set(LogKind.Normal, _sockTcp, 2, string.Format("address={0}", _sockTcp.RemoteAddress.Address));
            Kernel.View.SetColor();//�E�C���h�F�̏�����

            _sockTcp.Close();

        }

        void Cmd(OneRemoteData o) {
            //�T�[�r�X����Ăяo���ꂽ�ꍇ�́A�R���g���[�������͂Ȃ��̂�Invoke�͂��Ȃ�
            //if (mainForm != null && mainForm.InvokeRequired) {
            //    mainForm.Invoke(new MethodInvoker(() => Cmd(remoteObj)));
            //} else {
                switch (o.Kind) {
                    case RemoteDataKind.CmdRestart:
                        //�������g�i�X���b�h�j���~���邽�ߔ񓯊��Ŏ��s����
                        Kernel.Menu.EnqueueMenu("StartStop_Restart", false/*synchro*/);
                        break;
                    case RemoteDataKind.CmdTool:
                        var tmp = (o.Str).Split(new[] { '-' }, 2);
                        if (tmp.Length == 2) {
                            var nameTag = tmp[0];
                            var cmdStr = tmp[1];

                            var buffer = "";

                            if (nameTag == "BJD") {
                                buffer = Kernel.Cmd(cmdStr);//�����[�g����i�f�[�^�擾�j
                            } else {
                                var server = Kernel.ListServer.Get(nameTag);
                                if (server != null) {
                                    buffer = server.Cmd(cmdStr);//�����[�g����i�f�[�^�擾�j
                                }
                            }
                            RemoteData.Send(_sockTcp, RemoteDataKind.DatTool, cmdStr + "\t" + buffer);
                        }
                        break;
                    case RemoteDataKind.CmdBrowse:
                        var lines = Kernel.GetBrowseInfo(o.Str);
                        RemoteData.Send(_sockTcp, RemoteDataKind.DatBrowse, lines);
                        break;
                    case RemoteDataKind.CmdOption:
                        //string optionStr = remoteObj.STR;
                        //Option.ini��㏑������

                        //�N���C�A���g�ŃI�v�V������ύX���ăT�[�o���֑����Ă��邪���f����Ă��Ȃ��l�q
                        //c:\out�ŃN���C�A���g�𗧂��グ�A�uFTP�T�[�o�g�p����v�ɂ��ĕύX���đ����Ă݂�
                        //    �ύX���ꂽ��e���A�����ɓ������Ă��邩�ǂ�����m�F����


                        var optionFileName = string.Format("{0}\\Option.ini", Kernel.ProgDir());
                        using (var sw = new StreamWriter(optionFileName, false, Encoding.GetEncoding("Shift_JIS"))) {
                            sw.Write(o.Str);
                            sw.Close();
                        }
                        Kernel.ListInitialize();//Option.ini��ǂݍ���
                        
                        //Ver5.8.6 Java fix �V����Def����ǂݍ��񂾃I�v�V�������������ꍇ�ɁA���̃I�v�V������ۑ����邽��
                        Kernel.ListOption.Save(Kernel.IniDb);


                        //�������g�i�X���b�h�j���~���邽�ߔ񓯊��Ŏ��s����
                        Kernel.Menu.EnqueueMenu("StartStop_Reload",false/*synchro*/);
                        break;
                    case RemoteDataKind.CmdTrace:
                        Kernel.RemoteConnect.OpenTraceDlg = (o.Str=="1");
                        break;
            //    }
            }
        }


        //���O��Append�C�x���g�Ń����[�g�N���C�A���g�փ��O�𑗐M����
        public override void Append(OneLog oneLog) {
            RemoteData.Send(_sockTcp, RemoteDataKind.DatLog, oneLog.ToString());
        }
    }
}
