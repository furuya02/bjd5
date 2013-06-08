using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.mail;
using Bjd.sock;

namespace SmtpServer {
    public enum SmtpMode {
        Command = 0,
        Data = 1
    }

    //セッションごとの情報
    public class Session {

        public String Hello { get; set; } //nullの場合、HELO未受信
        public RcptList RcptList { get; set; }
        public MailAddress From { get; set; }//nullの場合、MAILコマンドをまだ受け取っていない
        public SmtpMode Mode { get; set; }
        
        private SockTcp _sockTcp;

        public Session(SockTcp sockTcp){
            Hello = null;
            From = null;
            Mode = SmtpMode.Command;
            _sockTcp = sockTcp;

        }

//        //１行送信
//        public void StringSend(string str) {
//            SockCtrl.StringSend(str, "ascii");
//        }
    }

}
