using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.mail;
using Bjd.sock;

namespace SmtpServer {

    //セッションごとの情報
    public class Session {

        public String Hello { get; set; } //nullの場合、HELO未受信
        public RcptList RcptList { get; set; }
        public MailAddress From { get; set; }//nullの場合、MAILコマンドをまだ受け取っていない
        public SessionMode Mode { get; private set; }
        
        private SockTcp _sockTcp;

        public Session(SockTcp sockTcp){
            Hello = null;
            From = null;
            Mode = SessionMode.Command;
            _sockTcp = sockTcp;
            RcptList = new RcptList();

        }

        public void SetMode(SessionMode mode){
            Mode = mode;
        }

//        //１行送信
//        public void StringSend(string str) {
//            SockCtrl.StringSend(str, "ascii");
//        }
    }

}
