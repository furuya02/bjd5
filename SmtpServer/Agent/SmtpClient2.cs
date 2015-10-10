using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.sock;
using Bjd.util;

namespace SmtpServer {


    class SmtpClient2 {

        public SmtpClient2() {
            LastLog = new List<string>();//���s���̍Ō�̑��M�L�^

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

        //string esmtpUser��null�łȂ��ꍇ�ASMTP�F�؂�g�p����
        public SmtpClientResult Send(SockTcp sockTcp,string serverName,Mail mail,MailAddress from,MailAddress to,string authUser,string authPass,ILife iLife) {
            
            var state = State.Ehlo;
            const int timeout = 3;
            var result = SmtpClientResult.Faild;
            //AUTH_STATE authState = AUTH_STATE.LOGIN;

            var smtpAuthClient = new SmtpAuthClient(authUser,authPass);

            LastLog.Clear();//���M���s���̋L�^�̓N���A����

            while (iLife.IsLife()) {
                //********************************************************************
                // �T�[�o����̃��X�|���X�R�[�h(response)��M
                //********************************************************************
                int response;
                //var recvBuf = sockTcp.LineRecv(timeout,OperateCrlf.No,ref life);
                //Ver5.7.3 �^�C���A�E�g���������āA�Ԏ��̒x���T�[�o�ŃG���[�ƂȂ��Ă��܂�
                var recvBuf = sockTcp.LineRecv(timeout+30, iLife);
                if (recvBuf == null) {
                    //���M���s���̍Ō�̑���M�L�^
                    LastLog.Add(sockTcp.LastLineSend);
                    //LastLog.Add(recvStr);
                    break;
                }
                if(recvBuf.Length==0){
                    Thread.Sleep(10);
                    continue;
                }
                recvBuf = Inet.TrimCrlf(recvBuf);//\r\n�̔r��
                var recvStr = Encoding.ASCII.GetString(recvBuf);

                if (state == State.Ehlo) {
                    smtpAuthClient.Ehlo(recvStr);//AUTH�̑Ή��󋵂�擾
                }

                if (recvStr[3] == '-') {
                    //string paramStr = recvStr.Substring(4);
                    continue;
                }
                if (recvStr.IndexOf(' ') == 3) {
                    response = Convert.ToInt32(recvStr.Substring(0, 3));
                } else {
                    //���M���s���̍Ō�̑���M�L�^
                    LastLog.Add(sockTcp.LastLineSend);
                    LastLog.Add(recvStr);
                    break;
                }
                //********************************************************************
                // ��M�������X�|���X�R�[�h(response)�ɂ����(mode)�̕ύX
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
                        result = SmtpClientResult.Success;//���M����
                        state = State.Quit;
                    }
                } else if (response == 354) {
                    if (state == State.Data)
                        state = State.Send;
                } else if (response / 100 == 5) {
                    // �]����SMTP�F�؂�K�v�Ƃ��Ȃ��ꍇ�AEHLO�Ɏ��s������HELO�ōĐڑ�����݂�
                    //if (Mode == 1 && TryEhlo && SmtpAuthClient == NULL) {
                    if (state == State.Ehlo) {
                        state = State.Helo;//HELO��500��󂯎�����ꍇ�̓G���[�����ɉ��
                    } else {//���M���s
                        
                        //���M���s���̍Ō�̑���M�L�^
                        LastLog.Add(sockTcp.LastLineSend);
                        LastLog.Add(recvStr);

                        result = SmtpClientResult.ErrorCode;//�G���[�R�[�h��M
                        
                        state = State.Quit;
                    }
                }
                //SMTP�F��
                var ret = smtpAuthClient.Set(recvStr);
                if (ret != null) {
                    sockTcp.AsciiSend(ret);
                    continue;
                }

                //********************************************************************
                // ���(mode)���Ƃ̏���
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
                        
                        //���M���s���̍Ō�̑���M�L�^
                        LastLog.Add(sockTcp.LastLineSend);
                        LastLog.Add(recvStr);

                        break;//�G���[����
                    }
                    const int count = -1; //count ���M����{���̍s���i-1�̏ꍇ�͑S���j
                    if (!mail.Send(sockTcp, count)){
                        //_logger.Set(LogKind.Error, null, 9000058, ex.Message);                        
                        //mail.GetLastError()�𖢏���
                        break;//�G���[����

                    }
                    sockTcp.AsciiSend(".");
                } else if (state == State.Quit) {
                    sockTcp.AsciiSend("QUIT");
                }
            }
            return result;
        }
    }

}






