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
    class PopClient : LastError,IDisposable{
        //private readonly InetKind _inetKind;
        private readonly int _port;
        private readonly Ip _ip;
        private readonly ILife _iLife;

        private int _sec; //タイムアウト
        private SockTcp _sockTcp;

        public PopClientStatus Status { get; private set; }

        //public PopClient(InetKind inetKind,Ip addr,int port,int sec,ILife iLife){
        public PopClient(Ip ip,int port,int sec,ILife iLife){
            //_inetKind = inetKind;
            _ip = ip;
            _port = port;
            _sec = sec;
            _iLife = iLife;

            Status = PopClientStatus.Idle;

        }
        
        //接続
        public bool Connect(){
            if (Status != PopClientStatus.Idle){
                SetLastError("Fail PopClient Connect() [State != PopClientStatus.Idle]");
                return false;
            }
            if (_ip.InetKind == InetKind.V4){
                _sockTcp = Inet.Connect(new Kernel(), _ip, _port, _sec+3, null);
            } else{
                _sockTcp = Inet.Connect(new Kernel(), _ip, _port, _sec+3, null);
            }
            if (_sockTcp.SockState == SockState.Connect){
                //+OK受信
                if (!RecvStatus()) {
                    return false;
                }
                Status = PopClientStatus.Authorization;
                return true;
            }
            SetLastError("Faild in PopClient Connect()");
            return false;
        }

        //ログイン
        public bool Login(String user, String pass) {
            //切断中の場合はエラー
            if (Status != PopClientStatus.Authorization) {
                SetLastError("Fail PopClient Login() [State != PopClientStatus.Authorization]");
                return false;
            }
            //USER送信
            if (!SendCmd(String.Format("USER {0}",user))) {
                return false;
            }
            //+OK受信
            if (!RecvStatus()) {
                return false;
            }
            //PASS送信
            if (!SendCmd(String.Format("PASS {0}", pass))) {
                return false;
            }
            //+OK受信
            if (!RecvStatus()) {
                return false;
            }
            Status = PopClientStatus.Transaction;
            return true;
        }

        public bool Quit(){
            //切断中の場合はエラー
            if (Status == PopClientStatus.Idle) {
                SetLastError("Fail PopClient Quit() [State == PopClientStatus.Idle]");
                return false;
            }
            //QUIT送信
            if (!SendCmd("QUIT")){
                return false;
            }
            //+OK受信
            if (!RecvStatus()){
                return false;
            }
            //切断
            _sockTcp.Close();
            _sockTcp = null;
            Status = PopClientStatus.Idle;
            return true;
        }

        public bool Uidl(List<String> lines) {
            lines.Clear();

            //切断中の場合はエラー
            if (Status != PopClientStatus.Transaction) {
                SetLastError("Fail PopClient Uidl() [Status != PopClientStatus.Transaction]");
                return false;
            }
            //QUIT送信
            if (!SendCmd("UIDL")) {
                return false;
            }
            //+OK受信
            if (!RecvStatus()){
                return false;
            }
            //.までの行を受信
            var buf = RecvData();
            if (buf == null){
                return false;
            }
            var s = Encoding.ASCII.GetString(buf);
            if (s.Length >= 5){
                //<CR><LF>.<CR><LF>を削除
                lines.AddRange(Inet.GetLines(s.Substring(0, s.Length - 5)));
            }
            return true;
        }

        public bool Retr(int n, Mail mail) {
            //切断中の場合はエラー
            if (Status != PopClientStatus.Transaction) {
                SetLastError("Fail PopClient Uidl() [Status != PopClientStatus.Transaction]");
                return false;
            }
            //RETR送信
            if (!SendCmd(string.Format("RETR {0}",n+1))) {
                return false;
            }
            //+OK受信
            if (!RecvStatus()) {
                return false;
            }
            //.までの行を受信
            var buf = RecvData();
            if (buf == null) {
                return false;
            }
            var tmp = new byte[buf.Length-3];
            Buffer.BlockCopy(buf,0,tmp,0,buf.Length-3);
            mail.Init2(tmp);
            
            return true;

        }
        
        public bool Dele(int n) {
            //切断中の場合はエラー
            if (Status != PopClientStatus.Transaction) {
                SetLastError("Fail PopClient Uidl() [Status != PopClientStatus.Transaction]");
                return false;
            }
            //DELE送信
            if (!SendCmd(string.Format("DELE {0}", n + 1))) {
                return false;
            }
            //+OK受信
            if (!RecvStatus()) {
                return false;
            }
            return true;
        }


        //.行までを受信する
        byte [] RecvData(){
            var dt = DateTime.Now;
            var line = new byte[0];

            while (_iLife.IsLife()){
                var now = DateTime.Now;
                if (dt.AddSeconds(_sec) < now) {
                    return null; //タイムアウト
                }
                var len = _sockTcp.Length();
                if (len == 0) {
                    continue;
                }
                var buf = _sockTcp.Recv(len, _sec, _iLife);
                if (buf == null) {
                    return null; //切断された
                }
                dt = now;

                var tmp = new byte[line.Length + buf.Length];
                Buffer.BlockCopy(line, 0, tmp, 0, line.Length);
                Buffer.BlockCopy(buf, 0,tmp,line.Length, buf.Length);
                line = tmp;
                if (line.Length >= 3){
                    if (line[line.Length - 1] == '\n' && line[line.Length - 2] == '\r' && line[line.Length - 3] == '.'){
                        return line;
                   }
                }
            }
            return null;
        }

        

        bool SendCmd(string cmdStr){
            //AsciiSendは、内部でCRLFを追加する
            if (cmdStr.Length + 2 != _sockTcp.AsciiSend(cmdStr)) {
                SetLastError(String .Format("Faild in PopClient SendCmd({0})",cmdStr));
                ConfirmConnect();//接続確認
                return false;
            }
            return true;
        }

        bool RecvStatus(){

            var buf = _sockTcp.LineRecv(_sec, _iLife);
            if (buf == null){
                SetLastError("Timeout in PopClient RecvStatus()");
                ConfirmConnect();//接続確認
                return false;
            }
            var str = Encoding.ASCII.GetString(buf);
            if (str.ToUpper().IndexOf("+OK") == 0){
                return true;
            }
            SetLastError("Not Found +OK in PopClient RecvStatus()");
            ConfirmConnect();//接続確認
            return false;
        }
        //接続確認
        void ConfirmConnect() {
            //既に切断されている場合
            if (_sockTcp.SockState != SockState.Connect) {
                Status = PopClientStatus.Idle;
            }
        }

        public void Dispose(){
            if (_sockTcp != null){
                _sockTcp.Close();
                _sockTcp = null;
            }
        }

    }
}
