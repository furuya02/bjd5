using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.mail;
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

        
        //RecvStatusの内部で受け取ったメッセージ
        //通常は使用しないが、CRAM-MD5の時にチャレンジ文字列が必要なため設定している
        private String _recvStr;

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
                SetLastError("Connect() Status != Idle");
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
                SetLastError("Helo() Status != Helo");
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



        public bool Auth(SmtpClientAuthKind kind,String user, String pass) {
            //トランザクションでない場合エラー
            if (Status != SmtpClientStatus.Transaction) {
                SetLastError("Auth() Status != Transaction");
                return false;
            }
            //AUTH送信
            var authStr="";
            switch (kind){
                case SmtpClientAuthKind.Login:
                    authStr = "AUTH LOGIN";
                    break;
                case SmtpClientAuthKind.Plain:
                    authStr = "AUTH PLAIN";
                    break;
                case SmtpClientAuthKind.CramMd5:
                    authStr = "AUTH CRAM-MD5";
                    break;
            }
            if (!SendCmd(authStr)){
                return false;
            }
            //334受信
            if (!RecvStatus(334)){
                return false;
            }
            //ユーザ・パスワード送信
            switch (kind){
                case SmtpClientAuthKind.Plain:
                    //ユーザ名送信
                    var str = String.Format("{0}\0{1}\0{2}", user, user, pass);
                    if (!SendCmd(String.Format(Base64.Encode(str)))) {
                        return false;
                    }
                    break;
                case SmtpClientAuthKind.Login:
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
                    break;
                case SmtpClientAuthKind.CramMd5:
                    //MD5送信
                    var timestamp = _recvStr.Split(' ')[1];
                    var s = string.Format("{0} {1}", user, Md5.Hash(pass, Base64.Decode(timestamp)));
                    if (!SendCmd(Base64.Encode(s))) {
                        return false;
                    }
                    break;

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
                SetLastError("Mail() Status != Transaction");
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
                SetLastError("Rcpt() Status != Transaction");
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

        public bool Data(Mail mail) {
            //トランザクションでない場合エラー
            if (Status != SmtpClientStatus.Transaction) {
                SetLastError("Data() Status != Transaction");
                return false;
            }
            //DATA送信
            if (!SendCmd("DATA")) {
                return false;
            }
            //354受信
            if (!RecvStatus(354)) {
                return false;
            }
            var lines = Inet.GetLines(mail.GetBytes());
            foreach (var l in lines){

                //ドットのみの行の場合、ドットを追加する
                if (l.Length == 3 && l[0] == '.' && l[1] == '\r' && l[2] == '\n'){
                    var buf = new byte[1]{l[0]};
                    _sockTcp.Send(buf);
                }
                if (l.Length != _sockTcp.Send(l)){
                    SetLastError(String.Format("Faild in SmtpClient Data()"));
                    ConfirmConnect();//接続確認
                    return false;
                }
            }
            //最終行が改行で終わっているかどうかの確認
            var last = lines[lines.Count - 1];
            if (last.Length < 2 || last[last.Length - 2] != '\r' || last[last.Length - 1] != '\n'){
                SendCmd("");//改行を送る
            }
            if (!SendCmd(".")){
                return false;
            }
            //250受信
            if (!RecvStatus(250)) {
                return false;
            }
            return true;
        }

        public bool Quit() {
            //トランザクションでない場合エラー
            if (Status == SmtpClientStatus.Idle) {
                SetLastError("Quit() Status == Idle");
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
            while (_iLife.IsLife()) {
                var buf = _sockTcp.LineRecv(_sec, _iLife);
                if (buf == null) {
                    SetLastError("Timeout in SmtpClient RecvStatus()");
                    break;
                }
                _recvStr = Encoding.ASCII.GetString(buf);
                if (_recvStr.Length < 3) {
                    SetLastError("str.Length<3 in SmtpClient RecvStatus()");
                    break;
                }
                if (_recvStr.Length > 3 && _recvStr[4] == '-') {
                    continue;
                }
                int result;
                if (!Int32.TryParse(_recvStr.Substring(0, 3), out result)) {
                    SetLastError("Faild TryPatse() in SmtpClient RecvStatus()");
                    break;
                }
                if (result == no) {
                    return true;
                }
                SetLastError(_recvStr);
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
