using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Bjd;
using Bjd.mail;
using Bjd.sock;
using Bjd.util;

namespace SmtpServer {


    class SmtpClient {

        public SmtpClient() {
            LastLog = new List<string>();//失敗時の最後の送信記録

        }
        public List<string> LastLog { get; private set; }

        enum State {
            //Before=0,
            Ehlo=1,
            Helo=2,
            Mail=3,
            Rcpt=4,
            Data=5,
            Send=6,
            Quit=7
        }

        //string esmtpUserがnullでない場合、SMTP認証を使用する
        public SmtpClientResult Send(SockTcp sockTcp,string serverName,Mail mail,MailAddress from,MailAddress to,string authUser,string authPass,ILife iLife) {
            
            var state = State.Ehlo;
            const int timeout = 3;
            var result = SmtpClientResult.Faild;
            //AUTH_STATE authState = AUTH_STATE.LOGIN;

            var smtpAuthClient = new SmtpAuthClient(authUser,authPass);

            LastLog.Clear();//送信失敗時の記録はクリアする

            while (iLife.IsLife()) {
                //********************************************************************
                // サーバからのレスポンスコード(response)受信
                //********************************************************************
                int response;
                //var recvBuf = sockTcp.LineRecv(timeout,OperateCrlf.No,ref life);
                //Ver5.7.3 タイムアウトが早すぎて、返事の遅いサーバでエラーとなってしまう
                var recvBuf = sockTcp.LineRecv(timeout+30, iLife);
                if (recvBuf == null) {
                    //送信失敗時の最後の送受信記録
                    LastLog.Add(sockTcp.LastLineSend);
                    //LastLog.Add(recvStr);
                    break;
                }
                if(recvBuf.Length==0){
                    Thread.Sleep(10);
                    continue;
                }
                recvBuf = Inet.TrimCrlf(recvBuf);//\r\nの排除
                var recvStr = Encoding.ASCII.GetString(recvBuf);

                if (state == State.Ehlo) {
                    smtpAuthClient.Ehlo(recvStr);//AUTHの対応状況を取得
                }

                if (recvStr[3] == '-') {
                    //string paramStr = recvStr.Substring(4);
                    continue;
                }
                if (recvStr.IndexOf(' ') == 3) {
                    response = Convert.ToInt32(recvStr.Substring(0, 3));
                } else {
                    //送信失敗時の最後の送受信記録
                    LastLog.Add(sockTcp.LastLineSend);
                    LastLog.Add(recvStr);
                    break;
                }
                //********************************************************************
                // 受信したレスポンスコード(response)による状態(mode)の変更
                //********************************************************************
                if (response == 220) {
                    state = State.Ehlo;
                } else if (response == 221) {
                    if (state == State.Quit)
                        break;
                } else if (response == 250) {
                    if (state == State.Ehlo || state == State.Helo) {
                        state = State.Mail;
                    } else if (state == State.Mail) {
                        state = State.Rcpt;
                    } else if (state == State.Rcpt) {
                        state = State.Data;
                    } else if (state == State.Send) {
                        result = SmtpClientResult.Success;//送信成功
                        state = State.Quit;
                    }
                } else if (response == 354) {
                    if (state == State.Data)
                        state = State.Send;
                } else if (response / 100 == 5) {
                    // 転送にSMTP認証を必要としない場合、EHLOに失敗したらHELOで再接続を試みる
                    //if (Mode == 1 && TryEhlo && SmtpAuthClient == NULL) {
                    if (state == State.Ehlo) {
                        state = State.Helo;//HELOで500を受け取った場合はエラー処理に回る
                    } else {//送信失敗
                        
                        //送信失敗時の最後の送受信記録
                        LastLog.Add(sockTcp.LastLineSend);
                        LastLog.Add(recvStr);

                        result = SmtpClientResult.ErrorCode;//エラーコード受信
                        
                        state = State.Quit;
                    }
                }
                //SMTP認証
                var ret = smtpAuthClient.Set(recvStr);
                if (ret != null) {
                    sockTcp.AsciiSend(ret);
                    continue;
                }

                //********************************************************************
                // 状態(mode)ごとの処理
                //********************************************************************
                if (state == State.Ehlo) {
                    sockTcp.AsciiSend(string.Format("EHLO {0}",serverName));
                }else if (state == State.Helo) {
                    sockTcp.AsciiSend(string.Format("HELO {0}",serverName));
                } else if (state == State.Mail) {
                    //Ver5.0.0-a24
                    //sockTcp.AsciiSend(string.Format("MAIL From:{0}",from),OPERATE_CRLF.YES);
                    sockTcp.AsciiSend(string.Format("MAIL From: <{0}>",from));
                } else if(state == State.Rcpt) {
                    //Ver5.0.0-a24
                    //sockTcp.AsciiSend(string.Format("RCPT To:{0}",to),OPERATE_CRLF.YES);
                    sockTcp.AsciiSend(string.Format("RCPT To: <{0}>",to));
                } else if(state == State.Data) {
                    sockTcp.AsciiSend("DATA");
                } else if (state == State.Send) {
                    if (mail == null) {
                        
                        //送信失敗時の最後の送受信記録
                        LastLog.Add(sockTcp.LastLineSend);
                        LastLog.Add(recvStr);

                        break;//エラー発生
                    }
                    const int count = -1; //count 送信する本文の行数（-1の場合は全部）
                    mail.Send(sockTcp,count);
                    sockTcp.AsciiSend(".");
                } else if (state == State.Quit) {
                    sockTcp.AsciiSend("QUIT");
                }
            }
            return result;
        }
    }

}






