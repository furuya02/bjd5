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

        private readonly int _sec; //タイムアウト
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
                //220受信
                if (!RecvStatus(220)) {
                    return false;
                }
                Status = SmtpClientStatus.Helo;
                return true;
            }
            SetLastError("Faild in SmtpClient Connect()");
            return false;
        }


        public bool Helo(){
            //切断中の場合はエラー
            if (Status != SmtpClientStatus.Helo) {
                SetLastError("Fail SmtpClient Login() [State != SmtpClientStatus.Helo]");
                return false;
            }
            //HELO送信
            if (!SendCmd(String.Format("HELO 1"))) {
                return false;
            }
            //250受信
            if (!RecvStatus(250)) {
                return false;
            }
            Status = SmtpClientStatus.Transaction;
            return true;
        }

        public bool AuthLogin(String user,String pass) {
            //トランザクションでない場合エラー
            if (Status != SmtpClientStatus.Transaction) {
                SetLastError("Fail SmtpClient AuthLogin() [State != SmtpClientStatus.Transaction]");
                return false;
            }
            //AUTH送信
            if (!SendCmd(String.Format("AUTH LOGIN"))) {
                return false;
            }
            //334受信
            if (!RecvStatus(334)) {
                return false;
            }
            //ユーザ名送信
            if (!SendCmd(String.Format(Base64.Encode(user)))) {
                return false;
            }
            //334受信
            if (!RecvStatus(334)) {
                return false;
            }
            //パスワード送信
            if (!SendCmd(String.Format(Base64.Encode(pass)))) {
                return false;
            }
            //235受信
            if (!RecvStatus(235)) {
                return false;
            }
            Status = SmtpClientStatus.Transaction;
            return true;
        }

        public bool AuthPlain(String user, String pass) {
            //トランザクションでない場合エラー
            if (Status != SmtpClientStatus.Transaction) {
                SetLastError("Fail SmtpClient AuthLogin() [State != SmtpClientStatus.Transaction]");
                return false;
            }
            //AUTH送信
            if (!SendCmd(String.Format("AUTH PLAIN"))) {
                return false;
            }
            //334受信
            if (!RecvStatus(334)) {
                return false;
            }
            //ユーザ名送信
            var str = String.Format("{0}\0{1}\0{2}", user, user, pass);
            if (!SendCmd(String.Format(Base64.Encode(str)))) {
                return false;
            }
            //235受信
            if (!RecvStatus(235)) {
                return false;
            }
            Status = SmtpClientStatus.Transaction;
            return true;
        }

        public bool Mail(String mailAddress) {
            //トランザクションでない場合エラー
            if (Status != SmtpClientStatus.Transaction) {
                SetLastError("Fail SmtpClient Mail() [State != SmtpClientStatus.Transaction]");
                return false;
            }
            //MAIL送信
            if (!SendCmd(String.Format("MAIL FROM: {0}",mailAddress))) {
                return false;
            }
            //250受信
            if (!RecvStatus(250)) {
                return false;
            }
            Status = SmtpClientStatus.Transaction;
            return true;
        }

        public bool Rcpt(String mailAddress) {
            //トランザクションでない場合エラー
            if (Status != SmtpClientStatus.Transaction) {
                SetLastError("Fail SmtpClient Rcpt() [State != SmtpClientStatus.Transaction]");
                return false;
            }
            //RCPT送信
            if (!SendCmd(String.Format("RCPT TO: {0}", mailAddress))) {
                return false;
            }
            //250受信
            if (!RecvStatus(250)) {
                return false;
            }
            Status = SmtpClientStatus.Transaction;
            return true;
        }


        public bool Quit() {
            //トランザクションでない場合エラー
            if (Status == SmtpClientStatus.Idle) {
                SetLastError("Fail SmtpClient Quit() [State == SmtpClientStatus.Idle]");
                return false;
            }
            //QUIT送信
            if (!SendCmd("QUIT")) {
                return false;
            }
            //221受信
            if (!RecvStatus(221)) {
                return false;
            }
            //切断
            _sockTcp.Close();
            _sockTcp = null;
            Status = SmtpClientStatus.Idle;
            return true;
        }

        bool RecvStatus(int no) {
            while (_iLife.IsLife()){
                var buf = _sockTcp.LineRecv(_sec, _iLife);
                if (buf == null) {
                    SetLastError("Timeout in SmtpClient RecvStatus()");
                    break;
                }
                var str = Encoding.ASCII.GetString(buf);
                if (str.Length < 3) {
                    SetLastError("str.Length<3 in SmtpClient RecvStatus()");
                    break;
                }
                if (str.Length > 3 && str[4] == '-') {
                    continue;
                }
                int result;
                if (!Int32.TryParse(str.Substring(0, 3), out result)){
                    SetLastError("Faild TryPatse() in SmtpClient RecvStatus()");
                    break;
                }
                if (result == no){
                    return true;
                }
                SetLastError(str);
                break;
            }
            ConfirmConnect();//接続確認
            return false;
        }
        
        bool SendCmd(string cmdStr) {
            //AsciiSendは、内部でCRLFを追加する
            if (cmdStr.Length + 2 != _sockTcp.AsciiSend(cmdStr)) {
                SetLastError(String.Format("Faild in SmtpClient SendCmd({0})", cmdStr));
                ConfirmConnect();//接続確認
                return false;
            }
            return true;
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
