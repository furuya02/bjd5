using System.Collections.Generic;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using Bjd.util;

namespace ProxyFtpServer {

    public partial class Server : OneServer {
        private const int DataPortMin = 10000;
        private const int DataPortMax = 11000;
        int _dataPort;

        public Server(Kernel kernel, Conf conf, OneBind oneBind)
            : base(kernel, conf, oneBind) {

            _dataPort = DataPortMin;

        }

        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }
        //�ڑ��P�ʂ̏���
        override protected void OnSubThread(SockObj sockObj) {

            var timeout = (int)Conf.Get("timeOut");

            var client = (SockTcp)sockObj;
            SockTcp server = null;

            var user = "";//���[�U��
            string pass;//�p�X���[�h
            var hostName = "";//�z�X�g��

            //***************************************************************
            //�O�����i�ڑ���E���[�U���E�p�X���[�h�̎擾)
            //***************************************************************
            {
                var str = string.Format("220 {0} {1}", Define.ApplicationName(), Define.Copyright());
                client.AsciiSend(str);

                var cmdStr = "";
                var paramStr = "";
                //wait USER user@hostName
                if (!WaitLine(client, ref cmdStr, ref paramStr)) {
                    goto end;
                }
                if (cmdStr.ToUpper() != "USER")
                    goto end;

                //paramStr = "user@hostName"
                if (paramStr != null) {
                    //string[] tmp = paramStr.Split('@');
                    //if(tmp.Length == 2) {
                    //    user = tmp[0];//���[�U���擾
                    //    hostName = tmp[1];//�z�X�g���擾
                    //}
                    var i = paramStr.LastIndexOf('@');
                    if (i != -1) {
                        user = paramStr.Substring(0, i);//���[�U���擾
                        hostName = paramStr.Substring(i + 1);//�z�X�g���擾
                    }
                }
                if (hostName == "") {
                    Logger.Set(LogKind.Error, sockObj, 8, "");
                    goto end;
                }

                client.AsciiSend("331 USER OK enter password");

                //wait PASS password
                if (!WaitLine(client, ref cmdStr, ref paramStr)) {
                    goto end;
                }
                if (cmdStr.ToUpper() != "PASS")
                    goto end;
                //paramStr = "password"
                pass = paramStr;//�p�X���[�h�擾
            }
            //***************************************************************
            // �T�[�o�Ƃ̐ڑ�
            //***************************************************************
            {
                const int port = 21;

                //var ipList = new List<Ip>{new Ip(hostName)};
                //if (ipList[0].ToString() == "0.0.0.0") {
                //    ipList = Kernel.DnsCache.Get(hostName);
                //    if (ipList.Count == 0) {
                //        goto end;
                //    }
                //}
                var ipList = Kernel.GetIpList(hostName);
                if (ipList.Count == 0) {
                    goto end;
                }

                Ssl ssl = null;

                foreach (var ip in ipList) {
                    server = Inet.Connect(Kernel,ip, port,Timeout, ssl);
                    if (server != null)
                        break;
                }
                if (server == null)
                    goto end;
            }
            //***************************************************************
            //�㏈���i���[�U���E�p�X���[�h�̑��M)
            //***************************************************************
            {
                var cmdStr = "";
                var paramStr = "";
                //wait 220 welcome
                while (cmdStr != "220") {
                    if (!WaitLine(server, ref cmdStr, ref paramStr)) {
                        goto end;
                    }
                }
                server.AsciiSend(string.Format("USER {0}", user));
                //wait 331 USER OK enter password
                while (cmdStr != "331") {
                    if (!WaitLine(server, ref cmdStr, ref paramStr)) {
                        goto end;
                    }
                }
                server.AsciiSend(string.Format("PASS {0}", pass));
                if (!WaitLine(server, ref cmdStr, ref paramStr)) {
                    goto end;
                }
                client.AsciiSend(string.Format("{0} {1}", cmdStr, paramStr));
            }

            //***************************************************************
            // �p�C�v
            //***************************************************************
            var ftpTunnel = new FtpTunnel(Kernel, Logger, (int)Conf.Get("idleTime"), _dataPort, timeout);
            //Ver5.0.5
            //ftpTunnel.BytePipe(ref life, server,client);
            ftpTunnel.Pipe(server, client,this);
            _dataPort = ftpTunnel.Dispose();
            if (_dataPort > DataPortMax)
                _dataPort = DataPortMin;
        end:
            client.Close();
            if (server != null)
                server.Close();
        }
        //RemoteServer�ł̂ݎg�p�����
        public override void Append(OneLog oneLog) {

        }

    }

}
