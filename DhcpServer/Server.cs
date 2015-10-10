using System;
using System.Net;
using Bjd;
using Bjd.ctrl;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;

namespace DhcpServer {

    public partial class Server : OneServer {
        readonly Lease _lease;//�f�[�^�x�[�X
        readonly Object _lockObj = new object();//�r������I�u�W�F�N�g

        readonly string _serverAddress;//�T�[�o�A�h���X

        readonly Ip _maskIp; //�}�X�N
        readonly Ip _gwIp;   //�Q�[�g�E�G�C
        readonly Ip _dnsIp0; //�c�m�r�i�v���C�}���j
        readonly Ip _dnsIp1; //�c�m�r�i�Z�J���_���j
        readonly int _leaseTime;//���[�X����
        readonly string _wpadUrl;//WPAD

        //�R���X�g���N�^
        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel, conf, oneBind) {

                //�I�v�V�����̓ǂݍ���
                _maskIp = (Ip)Conf.Get("maskIp");
                _gwIp = (Ip)Conf.Get("gwIp");
                _dnsIp0 = (Ip)Conf.Get("dnsIp0");
                _dnsIp1 = (Ip)Conf.Get("dnsIp1");
                _leaseTime = (int)Conf.Get("leaseTime");
            if (_leaseTime <= 0) 
                _leaseTime = 86400;
            if ((bool)Conf.Get("useWpad")) {
                _wpadUrl = (string)Conf.Get("wpadUrl");
            }

            
            //DB����
            string fileName = string.Format("{0}\\lease.db", kernel.ProgDir());
            var startIp = (Ip)Conf.Get("startIp");
            var endIp = (Ip)Conf.Get("endIp");
            _macAcl = (Dat)Conf.Get("macAcl");
            //�ݒ肪�����ꍇ�́A���Dat�𐶐�����
            if (_macAcl == null){
                _macAcl = new Dat(new CtrlType[]{CtrlType.TextBox,CtrlType.AddressV4, CtrlType.TextBox});
            }

            //Ver5.6.8
            //�J�������u���O�i�\����)�v�𑝂₵�����Ƃɂ��݊����ێ�
            if (_macAcl.Count > 0) {
                foreach (OneDat t in _macAcl){
                    if (t.StrList.Count == 2) {
                        t.StrList.Add(string.Format("host_{0}",t.StrList[1]));
                    }
                }
            }
            _lease = new Lease(fileName, startIp, endIp, _leaseTime, _macAcl);
            
            //�T�[�o�A�h���X�̏�����
            _serverAddress = Define.ServerAddress();

        }


        //�����[�g����i�f�[�^�̎擾�j
        public override String Cmd(string cmdStr) {
            if (cmdStr == "Refresh-Lease") {
                return _lease.GetInfo();
            }
            return "";
        }



        new public void Dispose() {
            _lease.Dispose();
            
            base.Dispose();
        }
        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }
        //�ڑ��P�ʂ̏���
        override protected void OnSubThread(SockObj sockObj) {

            var sockUdp = (SockUdp)sockObj;
            if (sockUdp.RemoteAddress.Port != 68) {// �ڑ����|�[�g�ԍ���68�ȊO�́ADHCP�p�P�b�g�ł͂Ȃ��̂Ŕj������
                return;
            }

            //�p�P�b�g�̓Ǎ�(��M�p�P�b�grp)            
            var rp = new PacketDhcp();
            if (!rp.Read(sockUdp.RecvBuf)) 
                return; //�f�[�^��߂Ɏ��s�����ꍇ�́A�����Ȃ�

            if (rp.Opcode != 1) 
                return;//OpCode���u�v���v�Ŗ����ꍇ�́A��������
            
            //���M���u���[�h�L���X�g�ɐݒ肷��
            var ep = new IPEndPoint(IPAddress.Broadcast, 68);
            sockUdp.RemoteAddress = ep;

            //********************************************************
            // MAC����
            //********************************************************
            if ((bool)Conf.Get("useMacAcl")) {// MAC���䂪�L���ȏꍇ
                if (!_lease.SearchMac(rp.Mac)) {
                    Logger.Set(LogKind.Secure,sockUdp,1,rp.Mac.ToString());
                    return;
                }
            }

            // �r������ (�f�[�^�x�[�X�����̂���)
            lock (_lockObj) {

                //�T�[�o�A�h���X
                Ip serverIp = rp.ServerIp;
                if (serverIp.AddrV4 == 0) {
                    serverIp = new Ip(_serverAddress);
                }
                //���N�G�X�g�A�h���X
                Ip requestIp = rp.RequestIp;

                //this.Logger.Set(LogKind.Detail,sockUdp,3,string.Format("{0} {1} {2}",rp.Mac,requestIp.ToString(),rp.Type.ToString()));
                Log(sockUdp, 3, rp.Mac, requestIp, rp.Type);

                if (rp.Type == DhcpType.Discover) {// ���o

                    requestIp = _lease.Discover(requestIp, rp.Id, rp.Mac);
                    if(requestIp!=null){
                        // OFFER���M
                        var sp = new PacketDhcp(rp.Id,requestIp,serverIp,rp.Mac,DhcpType.Offer,_leaseTime,_maskIp,_gwIp,_dnsIp0,_dnsIp1,_wpadUrl);
                        Send(sockUdp,sp);
                    }
                } else if (rp.Type == DhcpType.Request) {// �v��

                    requestIp = _lease.Request(requestIp, rp.Id, rp.Mac);
                    if (requestIp != null) {

                        if (serverIp.ToString() == _serverAddress) {// ���T�[�o����
                            // ACK���M
                            var sp = new PacketDhcp(rp.Id,requestIp,serverIp,rp.Mac,DhcpType.Ack,_leaseTime,_maskIp,_gwIp,_dnsIp0,_dnsIp1,_wpadUrl);
                            Send(sockUdp,sp);

                            //this.Logger.Set(LogKind.Normal,sockUdp,5,string.Format("{0} {1} {2}",rp.Mac,requestIp.ToString(),rp.Type.ToString()));
                            Log(sockUdp, 5, rp.Mac, requestIp, rp.Type);
                        } else {
                            _lease.Release(rp.Mac);//����������
                        }

                    } else {
                        // NACK���M
                        var sp = new PacketDhcp(rp.Id,requestIp,serverIp,rp.Mac,DhcpType.Nak,_leaseTime,_maskIp,_gwIp,_dnsIp0,_dnsIp1,_wpadUrl);
                        Send(sockUdp,sp);
                    }
                } else if (rp.Type == DhcpType.Release) {// �J��
                    requestIp = _lease.Release(rp.Mac);//�J��
                    if(requestIp!=null)
                        //this.Logger.Set(LogKind.Normal,sockUdp,6,string.Format("{0} {1} {2}",rp.Mac,requestIp.ToString(),rp.Type.ToString()));
                        Log(sockUdp, 6, rp.Mac, requestIp, rp.Type);
                } else if (rp.Type == DhcpType.Infrm) {// ���
                    // ACK���M
                    //Send(sockUdp,sp);
                }
            }// �r������
        }
        //���X�|���X�p�P�b�g�̑��M
        void Send(SockUdp sockUdp,PacketDhcp sp) {
            
            //���M
            sockUdp.Send(sp.GetBuffer());
            //this.Logger.Set(LogKind.Detail,sockUdp,4,string.Format("{0} {1} {2}",sp.Mac,(sp.RequestIp == null) ? "0.0.0.0" : sp.RequestIp.ToString(),sp.Type.ToString()));
            Log(sockUdp, 4, sp.Mac,sp.RequestIp,sp.Type);
        }

        void Log(SockUdp sockUdp,int messageNo, Mac mac,Ip ip,DhcpType type) {
            string macStr = mac.ToString();
            foreach (var m in _macAcl) {
                if (m.StrList[0].ToUpper() == mac.ToString()) {
                    macStr = string.Format("{0}({1})",mac,m.StrList[2]);
                    break;
                }
            }
            Logger.Set(LogKind.Detail, sockUdp, messageNo, string.Format("{0} {1} {2}", macStr, (ip == null) ? "0.0.0.0" : ip.ToString(), type.ToString()));
        }

        //RemoteServer�ł̂ݎg�p�����
        public override void Append(OneLog oneLog) {

        }

    }
}
