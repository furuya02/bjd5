using System;
using System.Collections.Generic;
using Bjd.mail;

namespace SmtpServer {

    //セッションごとの情報
    public class Session{

        public String Hello { get; private set; } //nullの場合、HELO未受信
        public List<MailAddress> To { get; private set; }
        public MailAddress From { get; private set; }//nullの場合、MAILコマンドをまだ受け取っていない
        public int UnknownCmdCounter { get; set; }//無効コマンドのカウント
        
        public Session(){
            Hello = null;
            From = null;
            To = new List<MailAddress>();
            UnknownCmdCounter = 0;
        }

        //HELO/EHLOコマンド
        public void Helo(string helo){
            Hello = helo;
        }

        //RESTコマンド
        public void Rest(){
            From = null;
            To.Clear();
        }
        //MAILコマンド
        public void Mail(MailAddress mailAddress) {
            //セッション初期化
            Rest();

            From = mailAddress;
        }
        //RCPTコマンド
        public void Rcpt(MailAddress mailAddress) {
            To.Add(mailAddress);
        }

    }

}
