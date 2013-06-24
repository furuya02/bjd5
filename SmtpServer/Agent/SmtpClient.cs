using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace SmtpServer {
    class SmtpClient : LastError, IDisposable {
        
        private readonly int _port;
        private readonly Ip _ip;
        private readonly ILife _iLife;

        private int _sec; //タイムアウト
        private SockTcp _sockTcp;

        public SmtpClientStatus Status { get; private set; }
        
        public SmtpClient(Ip ip, int port, int sec, ILife iLife){
            _ip = ip;
            _port = port;
            _sec = sec;
            _iLife = iLife;

            Status = SmtpClientStatus.Idle;

        }

        //接続
        public bool Connect() {
            if (Status != SmtpClientStatus.Idle) {
                SetLastError("Fail SmtpClient Connect() [State != SmtpClientStatus.Idle]");
                return false;
            }
            if (_ip.InetKind == InetKind.V4) {
                _sockTcp = Inet.Connect(new Kernel(), _ip, _port, _sec + 3, null);
            } else {
                _sockTcp = Inet.Connect(new Kernel(), _ip, _port, _sec + 3, null);
            }
            if (_sockTcp.SockState == SockState.Connect) {
                //+OK受信
                if (!RecvStatus()) {
                    return false;
                }
                Status = SmtpClientStatus.Authorization;
                return true;
            }
            SetLastError("Faild in SmtpClient Connect()");
            return false;
        }


        public object Login(string user1, string s){
            throw new NotImplementedException();
        }

        bool RecvStatus() {

            var buf = _sockTcp.LineRecv(_sec, _iLife);
            if (buf == null) {
                SetLastError("Timeout in SmtpClient RecvStatus()");
                ConfirmConnect();//接続確認
                return false;
            }
            var str = Encoding.ASCII.GetString(buf);
            if (str.ToUpper().IndexOf("+OK") == 0) {
                return true;
            }
            SetLastError("Not Found +OK in SmtpClient RecvStatus()");
            ConfirmConnect();//接続確認
            return false;
        }
        
        //接続確認
        void ConfirmConnect() {
            //既に切断されている場合
            if (_sockTcp.SockState != SockState.Connect) {
                Status = SmtpClientStatus.Idle;
            }
        }

        public void Dispose() {
            if (_sockTcp != null) {
                _sockTcp.Close();
                _sockTcp = null;
            }
        }

    }
}
