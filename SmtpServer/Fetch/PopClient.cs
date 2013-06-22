using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace SmtpServer {
    class PopClient : LastError,IDisposable{
        private readonly InetKind _inetKind;
        private readonly int _port;
        private readonly Ip _addr;
        private readonly ILife _iLife;

        private int _sec; //タイムアウト
        private SockTcp _sockTcp;

        public PopClientStatus Status { get; private set; }

        public PopClient(InetKind inetKind,Ip addr,int port,int sec,ILife iLife){
            _inetKind = inetKind;
            _addr = addr;
            _port = port;
            _sec = sec;
            _iLife = iLife;

            Status = PopClientStatus.Idle;

        }
        
        //接続
        public bool Connect(){
            if (Status != PopClientStatus.Idle){
                SetLastError("State!=PopClientStatus.Idle");
                return false;
            }
            if (_inetKind == InetKind.V4){
                _sockTcp = Inet.Connect(new Kernel(), _addr, _port, _sec+3, null);
            } else{
                _sockTcp = Inet.Connect(new Kernel(), _addr, _port, _sec+3, null);
            }
            if (_sockTcp.SockState == SockState.Connect){
                //+OK受信
                if (!RecvStatus()) {
                    return false;
                }
                Status = PopClientStatus.Authorization;
                return true;
            }
            SetLastError("Inet.Connect() faild.");
            return false;
        }

        //ログイン
        public bool Login(String user, String pass) {
            //切断中の場合はエラー
            if (Status != PopClientStatus.Authorization) {
                SetLastError("State != PopClientStatus.Authorization");
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
                SetLastError("State==PopClientStatus.Idle");
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
        

        bool SendCmd(string cmdStr){
            //AsciiSendは、内部でCRLFを追加する
            if (cmdStr.Length + 2 != _sockTcp.AsciiSend(cmdStr)) {
                SetLastError(string.Format("ERROR Send() {0}",cmdStr));
                ConfirmConnect();//接続確認
                return false;
            }
            return true;
        }

        bool RecvStatus(){

            var buf = _sockTcp.LineRecv(_sec, _iLife);
            if (buf == null){
                SetLastError("Recv() timeout.");
                ConfirmConnect();//接続確認
                return false;
            }
            var str = Encoding.ASCII.GetString(buf);
            if (str.ToUpper().IndexOf("+OK") == 0){
                return true;
            }
            SetLastError("faild receive +OK");
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
            _sockTcp.Close();
            _sockTcp = null;
        }
    }
}
