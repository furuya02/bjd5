using System;
using System.Collections.Generic;
using System.Threading;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;


//TCP�ɂ��v���L�V�̃x�[�X�N���X
namespace Bjd {

    public class Tunnel {
        //�\�P�b�g
        protected Dictionary<CS, SockTcp> Sock = new Dictionary<CS, SockTcp>(2);

        //�o�b�t�@�i�f�t�H���g�� byte[] �j
        readonly Dictionary<CS, byte[]> _byteBuf = new Dictionary<CS, byte[]>(2);
        readonly Dictionary<CS, string> _strBuf = new Dictionary<CS, string>(2);


        //�A�C�h��(���j�@0�̏ꍇ�A�A�C�h�������͖����ɂȂ�
        protected int IdleTime;
        protected Logger Logger;
        protected int Timeout;
        private DateTime _dt;

        //�A�C�h�������p�̃^�C�}������
        public void ResetIdle(){
            //�A�C�h�������L���̏ꍇ
            if (IdleTime != 0) {
                _dt = DateTime.Now.AddMinutes(IdleTime);
            }
        }
        //�A�C�h������ �^�C���A�E�g�̊m�F
        private bool IsTimeout() {
            if (IdleTime != 0){
                if (_dt < DateTime.Now){
                    return true;
                }
            }
            return false;
        }



        public Tunnel(Logger logger,int idleTime,int timeout) {

            _byteBuf[CS.Client] = new byte[0];
            _byteBuf[CS.Server] = new byte[0];
            _strBuf[CS.Client] = "";
            _strBuf[CS.Server] = "";

            IdleTime = idleTime;
            Logger = logger;
            Timeout = timeout;
        }
        public void Pipe(SockTcp server, SockTcp client,ILife iLife) {

            Sock[CS.Client] = client;
            Sock[CS.Server] = server;

            //�A�C�h�������p�̃^�C�}������
            ResetIdle();

            var cs = CS.Server;
            while(iLife.IsLife()) {
                cs = Reverse(cs);//�T�[�o���ƃN���C�A���g�����݂ɏ�������
                Thread.Sleep(1);

                // �N���C�A���g�̐ؒf�̊m�F
                if(Sock[CS.Client].SockState != SockState.Connect) {

                    //Ver5.2.8
                    //�N���C�A���g���ؒf���ꂽ�ꍇ�ł�A�T�[�o�����ڑ����ő��M����ׂ��f�[�^���c���Ă���ꍇ�͏�����p������
                    if (Sock[CS.Server].SockState == SockState.Connect && Sock[CS.Client].Length() != 0) {
                        
                    } else {
                        Logger.Set(LogKind.Detail, Sock[CS.Server], 9000043, "close client");
                        break;
                    }
                }

                //*******************************************************
                //��������f�[�^���������Ă��Ȃ��ꍇ�̏���
                //*******************************************************
                if(Sock[CS.Client].Length() == 0 && Sock[CS.Server].Length() == 0 && _byteBuf[CS.Client].Length == 0 && _byteBuf[CS.Server].Length == 0) {

                    // �T�[�o�̐ؒf�̊m�F
                    if(Sock[CS.Server].SockState != SockState.Connect) {

                        //���M����ׂ��f�[�^���Ȃ��A�T�[�o���ؒf���ꂽ�ꍇ�́A�����I��
                        Logger.Set(LogKind.Detail,Sock[CS.Server],9000044,"close server");
                        break;
                    }

                    Thread.Sleep(100);

                    //�A�C�h������ �^�C���A�E�g�̊m�F
                    if(IsTimeout()){
                        Logger.Set(LogKind.Normal,Sock[CS.Server],9000019,string.Format("option IDLETIME={0}min",IdleTime));
                        break;
                    }
                } else {
                    //�A�C�h�������p�̃^�C�}������
                    ResetIdle();
                }

                //*******************************************************
                // ��M���� 
                //*******************************************************
                if(_byteBuf[cs].Length == 0) { //�o�b�t�@����̎�������������
                    //�������ׂ��f�[�^���̎擾
                    var len = Sock[cs].Length();
                    if(len > 0) {
                        const int sec = 10; //��M�o�C�g�����킩���Ă���̂ŁA�����ł̃^�C���A�E�g�l�͂��܂�Ӗ�������
                        var b = Sock[cs].Recv(len,sec,iLife);
                        if(b != null){
                            //Assumption() ��M���̏���
                            _byteBuf[cs] = Bytes.Create(_byteBuf[cs],Assumption(b,iLife));
                        }
                    }
                }
                //*******************************************************
                // ���M����
                //*******************************************************
                if(_byteBuf[cs].Length != 0) { //�o�b�t�@�Ƀf�[�^�������Ă���ꍇ������������

                    var c = Sock[Reverse(cs)].SendUseEncode(_byteBuf[cs]);
                    if(c == _byteBuf[cs].Length) {
                        _byteBuf[cs] = new byte[0];
                    } else {
                        Logger.Set(LogKind.Error,server,9000020,string.Format("sock.Send() return {0}",c));
                        break;
                    }
                }
            }
        }

        //��M���̏���
        //��M������e�ɂ���ď�����s���K�v������ꍇ�́A���̃��\�b�h��I�[�o�[���C�h����
        virtual protected byte [] Assumption(byte [] buf,ILife iLife) {
            //�f�t�H���g�ł͏����Ȃ�
            return buf;
        }

        CS Reverse(CS cs){
            return cs == CS.Client ? CS.Server : CS.Client;
        }
    }

}
