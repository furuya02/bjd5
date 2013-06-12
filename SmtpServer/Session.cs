using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.log;
using Bjd.mail;
using Bjd.sock;

namespace SmtpServer {

    //セッションごとの情報
    public class Session :IDisposable{

        public String Hello { get; set; } //nullの場合、HELO未受信
        public RcptList RcptList { get; set; }
        public MailAddress From { get; set; }//nullの場合、MAILコマンドをまだ受け取っていない
        public SessionMode Mode { get; private set; }
        public Mail Mail { get; private set; } //データ受信用
        public int UnknownCmdCounter { get; set; }//無効コマンドのカウント
        
        public Session(){
            Hello = null;
            From = null;
            Mail = null;
            Mode = SessionMode.Command;
            RcptList = new RcptList();
            UnknownCmdCounter = 0;
        }

        public void SetMode(SessionMode mode){
            Mode = mode;
        }

//        //１行送信
//        public void StringSend(string str) {
//            SockCtrl.StringSend(str, "ascii");
//        }
        public void Dispose(){
            if (Mail != null){
                Mail.Dispose();
            }
        }
        //Dataコマンドの際に初期化される
        public void InitMail(){
            if (Mail != null) {
                Mail.Dispose();
            }
            Mail = new Mail();
        }
    }

}
