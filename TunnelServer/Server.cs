using System.Collections.Generic;
using System.Net;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using Bjd.util;

namespace TunnelServer {
    partial class Server : OneServer {

        //�ʏ��ServerThread�̎q�N���X�ƈႢ�A�I�v�V�����̓��X�g�Ŏ󂯎��
        //�e�N���X�́A���̃��X�g��0�Ԗڂ̃I�u�W�F�N�g�ŏ���������
        readonly string _targetServer;
        readonly int _targetPort;
        readonly ProtocolKind _protocolKind;


        //�R���X�g���N�^
        public Server(Kernel kernel,Conf conf,OneBind oneBind)
            : base(kernel, conf, oneBind) {

            _targetServer = (string)Conf.Get("targetServer");
            _targetPort = (int)Conf.Get("targetPort");

            if (_targetServer == "") {
                Logger.Set(LogKind.Error,null, 1, "");
            }
            if (_targetPort == 0) {
                Logger.Set(LogKind.Error, null,2, "");
            }

            _protocolKind = oneBind.Protocol;
        }

        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }

        //�ڑ��P�ʂ̏���
        override protected void OnSubThread(SockObj sockObj) {
            
            if (_protocolKind == ProtocolKind.Tcp) {
                TcpTunnel((SockTcp)sockObj);
            } else {
                UdpTunnel((SockUdp)sockObj);
            }

        }
        void TcpTunnel(SockTcp tcpObj) {

            var client = tcpObj;
            SockTcp server = null;

            //***************************************************************
            // �T�[�o�Ƃ̐ڑ�
            //***************************************************************
            {
                var port = _targetPort;

                //var ipList = new List<Ip>{new Ip(_targetServer)};
                //if (ipList[0].ToString() == "0.0.0.0") {
                //    ipList = Kernel.DnsCache.Get(_targetServer);
                //    if(ipList.Count==0){
                //        Logger.Set(LogKind.Normal,null,4,string.Format("{0}:{1}",_targetServer,_targetPort));
                //        goto end;
                //    }
                //}
                var ipList = Kernel.GetIpList(_targetServer);
                if (ipList.Count == 0){
                    Logger.Set(LogKind.Normal, null, 4, string.Format("{0}:{1}", _targetServer, _targetPort));
                    goto end;
                }
                foreach (var ip in ipList) {
                    server = Inet.Connect(Kernel,ip,port,Timeout,null);
                    if (server != null)
                        break;
                }
                if (server == null) {
                    Logger.Set(LogKind.Normal,server,5,string.Format("{0}:{1}",_targetServer,_targetPort));
                    goto end;
                }
            }
            Logger.Set(LogKind.Normal,server,6,string.Format("TCP {0}:{1} - {2}:{3}",client.RemoteHostname,client.RemoteAddress.Port,_targetServer,_targetPort));

            //***************************************************************
            // �p�C�v
            //***************************************************************
            var tunnel = new Tunnel(Logger,(int)Conf.Get("idleTime"),Timeout);
            tunnel.Pipe(server, client,this);
        end:
            if (client != null)
                client.Close();
            if (server != null)
                server.Close();
        }
        void UdpTunnel(SockUdp udpObj) {
            //int timeout = this.OpBase.ValInt("timeOut");
            var sock = new Dictionary<CS, SockUdp>(2);
            sock[CS.Client] = udpObj;
            sock[CS.Server] = null;

            //***************************************************************
            // �T�[�o�Ƃ̐ڑ�
            //***************************************************************
            {
                int port = _targetPort;
                //var ip = new Ip(_targetServer);
                //if (ip.ToString() == "0.0.0.0") {
                //    try {
                //        var iphe = Dns.GetHostEntry(_targetServer);
                //        if (iphe.AddressList.Length == 0) {
                //            goto end;
                //        }
                //        ip = new Ip(iphe.AddressList[0].ToString());
                //    } catch {//���O�Ɏ��s�����ꍇ
                //        Logger.Set(LogKind.Normal,null, 4, string.Format("{0}:{1}", _targetServer, _targetPort));
                //        goto end;
                //    }
                //}
                Ip ip;
                try{
                    ip = new Ip(_targetServer);
                }catch(ValidObjException){
                    try {
                        var iphe = Dns.GetHostEntry(_targetServer);
                        if (iphe.AddressList.Length == 0) {
                            goto end;
                        }
                        ip = new Ip(iphe.AddressList[0].ToString());
                    } catch {//���O�Ɏ��s�����ꍇ
                        Logger.Set(LogKind.Normal, null, 4, string.Format("{0}:{1}", _targetServer, _targetPort));
                        goto end;
                    }
                }


                sock[CS.Server] = new SockUdp(Kernel,ip, port, null,new byte[0]);
                if(sock[CS.Server].SockState == Bjd.sock.SockState.Error)
                    goto end;
            }
            sock[CS.Server].Send(sock[CS.Client].RecvBuf);//�T�[�o�֑��M
            //if (sock[CS.Server].Recv(Timeout)) {//�T�[�o����̎�M
            var buf = sock[CS.Server].Recv(Timeout);
            if(buf.Length==0){
                sock[CS.Client].Send(buf);//�N���C�A���g�֑��M
            }
            Logger.Set(LogKind.Normal,sock[CS.Server],7,string.Format("UDP {0}:{1} - {2}:{3} {4}byte",sock[CS.Client].RemoteHostname,sock[CS.Client].RemoteAddress.Port,_targetServer,_targetPort,buf.Length));

        end:
            //udpObj.Close();UDP�\�P�b�g(udpObj)�̓N���[���Ȃ̂ŃN���[�Y���Ă�A��������Ȃ���Close()��Ăяo���Ă���͂Ȃ�
            if (sock[CS.Client] != null)
                sock[CS.Client].Close();
            if (sock[CS.Server] != null)
                sock[CS.Server].Close();

        }

        //RemoteServer�ł̂ݎg�p�����
        public override void Append(OneLog oneLog) {

        }

    }
}


