using System;
using System.Collections.Generic;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;

namespace SmtpServer {
    class OneAgent : ThreadBase {
        readonly Conf _conf;
        readonly Logger _logger;
        readonly MailQueue _mailQueue;
        readonly OneQueue _oneQueue;

        readonly SmtpClient2 _smtpClient2;

        //暫定
        private readonly Kernel _kernel;
        private readonly Server _server;


        public OneAgent(Kernel kernel, Server server,Conf conf,Logger logger, MailQueue mailQueue, OneQueue oneQueue)
            : base(kernel.CreateLogger("OneAgent",true,null)) {
            _conf = conf;
            _logger = logger;
            _mailQueue = mailQueue;
            _oneQueue = oneQueue;
            _smtpClient2 = new SmtpClient2();

            //暫定
            _kernel = kernel;
            _server = server;

        }

        override protected bool OnStartThread() {
            return true;
        }

        override protected void OnStopThread() {
        }

        override protected void OnRunThread() {
            //ログ用文字列の生成
            string mailStr = string.Format("{0} retry:{1}",
                _oneQueue.MailInfo,
                _oneQueue.MailInfo.RetryCounter);

            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;

            //開始ログ
            _logger.Set(LogKind.Normal, null, 10, mailStr);

            //ここで、1通のメールを１回処理する
            //成功か失敗かの処理は、この関数の最後にもってくるべき？
            //失敗の理由によって、再試行にまわしたり、リターンメールを作成したり・・・・
            //リターンのリターンはループの危険性がある

            var retryMax = (int)_conf.Get("retryMax");//リトライ回数
            var deleteTarget = false;//処理対象のメールを削除するかどうかのフラグ

            //0.サーバ検索が終わっていない状態からスタート
            var result = SmtpClientResult.Faild;

            //Ver5.7.3 無効データの削除
            if(_oneQueue.MailInfo.To.Domain==""){
                deleteTarget = true;
                goto end;
            }

            //サーバ（アドレス）検索
            List<OneSmtpServer> smtpServerList = GetSmtpServerList(_oneQueue.MailInfo.To.Domain);
            if (smtpServerList.Count == 0) {
                //サーバ（アドレス）検索失敗（リトライ対象）
                _logger.Set(LogKind.Error, null, 12, string.Format("domain={0}", _oneQueue.MailInfo.To.Domain));//失敗
            } else {
                //送信処理
                foreach (OneSmtpServer oneSmtpServer in smtpServerList) {
                    Ssl ssl = null;
                    if (oneSmtpServer.Ssl) {
                        //クライアント用SSLの初期化
                        //ssl = new Ssl(server.Logger,oneSmtpServer.TargetServer);
                        ssl = new Ssl(oneSmtpServer.TargetServer);
                    }
                    var timeout = 5;
                    var tcpObj = Inet.Connect(_kernel,oneSmtpServer.Ip, oneSmtpServer.Port, timeout,ssl);
                    if (tcpObj == null) {
                        //serverMain.Logger.Set(LogKind.Error, xx, string.Format("to={0} address={1}", oneQueue.MailInfo.To.ToString(), ip.IpStr));
                        continue;
                    }
                    //Ver5.9.8
                    if (tcpObj.SockState != SockState.Connect){
                        _logger.Set(LogKind.Error, tcpObj, 56, tcpObj.GetLastEror());//失敗
                        break;
                    }

                    string esmtpUser = null;
                    string esmtpPass = null;
                    if (oneSmtpServer.UseSmtp) {
                        esmtpUser = oneSmtpServer.User;
                        esmtpPass = oneSmtpServer.Pass;
                    }
                    result = _smtpClient2.Send(tcpObj, _kernel.ServerName, _oneQueue.Mail(_mailQueue), _oneQueue.MailInfo.From, _oneQueue.MailInfo.To, esmtpUser, esmtpPass, this);
                    tcpObj.Close();

                    if (result == SmtpClientResult.Success) {
                        //送信成功
                        _logger.Set(LogKind.Normal, tcpObj, 11, mailStr);//成功
                        deleteTarget = true;
                        break;
                    }
                    if (result == SmtpClientResult.ErrorCode) {
                        //明確なエラーの発生
                        _logger.Set(LogKind.Error, tcpObj, 14, mailStr);//失敗
                        break;
                    }
                }
                if (result == SmtpClientResult.Faild) {
                    _logger.Set(LogKind.Error, null, 13, mailStr);//失敗
                }
            }

            //エラーコードが返された場合及びリトライ回数を超えている場合、リターンメールを作成する
            if (result == SmtpClientResult.ErrorCode || retryMax <= _oneQueue.MailInfo.RetryCounter) {
                var from = new MailAddress((string)_conf.Get("errorFrom"));
                var to = _oneQueue.MailInfo.From;

                //Ver_Ml
                //メール本体からＭＬメールかどうかを確認する
                //List-Software: BlackJumboDog Ver 5.0.0-b13
                //List-Owner: <mailto:1ban-admin@example.com>
                var orgMail = _oneQueue.Mail(_mailQueue);
                var listSoftware = orgMail.GetHeader("List-Software");
                if (listSoftware != null && listSoftware.IndexOf(Define.ApplicationName()) == 0) {
                    var listOwner = orgMail.GetHeader("List-Owner");
                    if (listOwner != null) {
                        //<mailto:1ban-admin@example.com>
                        listOwner = listOwner.Trim(new char[] { '<', '>' });
                        //mailto:1ban-admin@example.com
                        var admin = listOwner.Substring(7);
                        //1ban-admin@example.com
                        to = new MailAddress(admin);//宛先をＭＬ管理者に変更する
                    }
                }
                const string reason = "550 Host unknown";
                var mail = MakeErrorMail(from, to, reason, _smtpClient2.LastLog);
                _logger.Set(LogKind.Normal, null, 15, string.Format("from:{0} to:{1}", from, to));
                if (_server.MailSave2(from, to, mail, _oneQueue.MailInfo.Host, _oneQueue.MailInfo.Addr)) {
                    deleteTarget = true; //メール削除
                }
            }
end:
            if (deleteTarget)
                _oneQueue.Delete(_mailQueue); //メール削除
        }

        public override string GetMsg(int no){
            throw new NotImplementedException();
        }

        //送信先ドメイン名から送信先サーバのアドレスリストを取得する
        //「ホスト設定」にヒットした場合は、その設定に従う
        List<OneSmtpServer> GetSmtpServerList(string domainName) {
            var smtpServerList = new List<OneSmtpServer>();
            //規定値
            var targetServer = "";
            int port;//ポート
            var useSmtp = false;//SMTP認証（なし）
            string user;//認証用ユーザ名（なし）
            string pass;//認証用パスワード（なし）
            var ssl = false;//SSL接続

            //送り先ドメインが「ホスト設定」で定義されているかどうかの確認
            foreach (var o in (Dat)_conf.Get("hostList")) {
                if (o.Enable) { //有効なデータだけを対象にする
                    var targetDomain = o.StrList[0];
                    var isHit = false;
                    if (targetDomain == "*") { //すべてヒット
                        isHit = true;
                    } else {
                        //*以降を削除
                        int index = targetDomain.IndexOf('*');
                        if (0 <= index) {
                            targetDomain = targetDomain.Substring(0, index);
                        }
                        if (domainName.ToUpper().IndexOf(targetDomain.ToUpper()) == 0) {
                            isHit = true;
                        }
                    }
                    if (isHit) {
                        targetServer = o.StrList[1];
                        port = Convert.ToInt32(o.StrList[2]);
                        useSmtp = Convert.ToBoolean(o.StrList[3]);
                        user = o.StrList[4];
                        pass = Crypt.Decrypt(o.StrList[5]);
                        ssl = Convert.ToBoolean(o.StrList[6]);
                        
                        //var ip = new Ip(targetServer);
                        //if (ip.ToString() != "0.0.0.0") {
                        //    smtpServerList.Add(new OneSmtpServer(targetServer, ip, port, useSmtp, user, pass, ssl));
                        //} else {
                        //    var tmp = Lookup.QueryA(targetServer);
                        //    foreach (var s in tmp) {
                        //        smtpServerList.Add(new OneSmtpServer(targetServer, new Ip(s), port, useSmtp, user, pass, ssl));
                        //    }
                        //}
                        try{
                            var ip = new Ip(targetServer);
                            smtpServerList.Add(new OneSmtpServer(targetServer, ip, port, useSmtp, user, pass, ssl));
                        } catch (ValidObjException) {
                            var tmp = Lookup.QueryA(targetServer);
                            try{
                                foreach (var s in tmp) {
                                    smtpServerList.Add(new OneSmtpServer(targetServer, new Ip(s), port, useSmtp, user, pass, ssl));
                                }
                            }catch(ValidObjException){
                                
                            }
                        }
                        return smtpServerList;
                    }
                }
            }

            targetServer = "";
            port = 25;
            user = "";
            pass = "";
            ssl = false;

            var hostList = new List<string>();
            var dnsServerList = Lookup.DnsServer();
            foreach (var dnsServer in dnsServerList) {
                hostList = Lookup.QueryMx(domainName, dnsServer);
                if (hostList.Count > 0)
                    break;
            }

            foreach (var host in hostList) {
                var tmp = Lookup.QueryA(host);
                foreach (string s in tmp) {
                    smtpServerList.Add(new OneSmtpServer(targetServer, new Ip(s), port, useSmtp, user, pass, ssl));
                }
            }

            if (smtpServerList.Count == 0) {
                if (!(bool)_conf.Get("mxOnly")) {
                    var tmp = Lookup.QueryA(domainName);
                    
                    foreach (var s in tmp) {
                        smtpServerList.Add(new OneSmtpServer(targetServer, new Ip(s), port, useSmtp, user, pass, ssl));
                    }
                }
            }
            return smtpServerList;
        }

        //エラーメールの作成
        Mail MakeErrorMail(MailAddress from, MailAddress to, string reason, List<string> lastLog) {
            var mail = new Mail();
            const string boundaryStr = "BJD-Boundary";

            mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("From: Mail Delivery Subsystem <{0}>\r\n", @from)));
            mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("To: {0}\r\n", to)));
            mail.AppendLine(Encoding.ASCII.GetBytes("Subject: Returned mail: see transcript for details\r\n"));
            mail.AppendLine(Encoding.ASCII.GetBytes("MIME-Version: 1.0\r\n"));

            mail.AppendLine(Encoding.ASCII.GetBytes("Content-Type: multipart/mixed;\r\n"));
            mail.AppendLine(Encoding.ASCII.GetBytes(string.Format(" boundary=\"{0}\"\r\n", boundaryStr)));
            mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));//ヘッダ終了

            mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("--{0}\r\n", boundaryStr)));
            mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));
            mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("The original message was received at {0}\r\n", _oneQueue.MailInfo.Date)));
            mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("from {0}[{1}]\r\n", _oneQueue.MailInfo.Host, _oneQueue.MailInfo.Addr)));
            mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));

            if (lastLog.Count >= 2) {
                mail.AppendLine(Encoding.ASCII.GetBytes("    ----- The following addresses had parmanent fatal errors -----\r\n"));
                mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("<{0}>\r\n", _oneQueue.MailInfo.To)));
                mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("   (reason:: {0})\r\n", lastLog[1])));
                mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));
                mail.AppendLine(Encoding.ASCII.GetBytes("    ----- Transcript of session follws -----\r\n"));
                mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("... while talking to {0}\r\n", _oneQueue.MailInfo.To.Domain)));
                mail.AppendLine(Encoding.ASCII.GetBytes(string.Format(">>> {0}\r\n", lastLog[0])));
                mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("<<< {0}\r\n", lastLog[1])));
                mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));
            } else {
                mail.AppendLine(Encoding.ASCII.GetBytes("    ----- The following addresses had parmanent fatal errors -----\r\n"));
                mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("<{0}>\r\n", _oneQueue.MailInfo.To)));
                mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("   (reason:: {0})\r\n", reason)));
                mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));
                mail.AppendLine(Encoding.ASCII.GetBytes("    ----- Transcript of session follws -----\r\n"));
                mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("{0}\r\n", reason)));
                mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));
            }

            mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("--{0}\r\n", boundaryStr)));
            mail.AppendLine(Encoding.ASCII.GetBytes("Content-Type: message/rfc822\r\n"));
            mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));

            //string str = oneQueue.Mail(mailQueue).ToString();//メール本体
            //mail.AppendLine(Encoding.ASCII.GetBytes(str));
            //Ver5.0.0_Ml
            mail.AppendLine(_oneQueue.Mail(_mailQueue).GetBytes());//メール本体

            mail.AppendLine(Encoding.ASCII.GetBytes(string.Format("--{0}--\r\n", boundaryStr)));

            return mail;
        }
        /*
        //エラーメールの作成
        Mail MakeErrorMail(MailAddress from,MailAddress to,string reason,List<string> lastLog) {
        StringBuilder sb = new StringBuilder();
        string boundaryStr = "BJD-Boundary";
        sb.Append(string.Format("From: Mail Delivery Subsystem <{0}>\r\n",from.ToString()));
        sb.Append(string.Format("To: {0}\r\n",to.ToString()));
        sb.Append("Subject: Returned mail: see transcript for details\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: multipart/mixed;\r\n");
        sb.Append(string.Format(" boundary=\"{0}\"\r\n",boundaryStr));
        sb.Append("\r\n");//ヘッダ終了
        sb.Append(string.Format("--{0}\r\n",boundaryStr));
        sb.Append("\r\n");
        sb.Append(string.Format("The original message was received at {0}\r\n",oneQueue.MailInfo.Date));
        sb.Append(string.Format("from {0}[{1}]\r\n",oneQueue.MailInfo.Host,oneQueue.MailInfo.Addr));
        sb.Append("\r\n");
        if(lastLog.Count >= 2) {
        sb.Append("    ----- The following addresses had parmanent fatal errors -----\r\n");
        sb.Append(string.Format("<{0}>\r\n",oneQueue.MailInfo.To.ToString()));
        sb.Append(string.Format("   (reason:: {0})\r\n",lastLog[1]));
        sb.Append("\r\n");
        sb.Append("    ----- Transcript of session follws -----\r\n");
        sb.Append(string.Format("... while talking to {0}\r\n",oneQueue.MailInfo.To.Domain));
        sb.Append(string.Format(">>> {0}\r\n",lastLog[0]));
        sb.Append(string.Format("<<< {0}\r\n",lastLog[1]));
        sb.Append("\r\n");
        } else {
        sb.Append("    ----- The following addresses had parmanent fatal errors -----\r\n");
        sb.Append(string.Format("<{0}>\r\n",oneQueue.MailInfo.To.ToString()));
        sb.Append(string.Format("   (reason:: {0})\r\n",reason));
        sb.Append("\r\n");
        sb.Append("    ----- Transcript of session follws -----\r\n");
        sb.Append(string.Format("{0}\r\n",reason));
        sb.Append("\r\n");
        }
        sb.Append(string.Format("--{0}\r\n",boundaryStr));
        sb.Append("Content-Type: message/rfc822\r\n");
        sb.Append("\r\n");
        sb.Append(oneQueue.Mail(mailQueue).ToString());//メール本体
        sb.Append(string.Format("--{0}--\r\n",boundaryStr));
        return new Mail(sb.ToString());
        }
        */
    }
}
