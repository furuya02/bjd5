
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using Bjd.util;

namespace SmtpServer {
    partial class Server : OneServer {

        public List<string> DomainList { get; private set; }
        readonly MailQueue _mailQueue;
        readonly MailSave _mailSave;
        readonly Agent _agent;//キュー処理スレッド
        Fetch _fetch;//自動受信
        public Alias Alias { get; private set; }//エリアス

        readonly Relay _relay;//中継許可
        private readonly PopBeforeSmtp _popBeforeSmtp;
        private readonly SmtpAuthUserList _smtpAuthUserList;
        private readonly SmtpAuthRange _smtpAuthRange;
        //ヘッダ置換
        private readonly ChangeHeader _changeHeader;



#if ML_SERVER
        readonly MlList _mlList;//MLリスト
#endif

        //コンストラクタ
        public Server(Kernel kernel, Conf conf, OneBind oneBind)
            : base(kernel, conf, oneBind) {

            //Ver5.8.9
            if (kernel.RunMode == RunMode.Normal || kernel.RunMode == RunMode.Service) {
                //メールボックスの初期化状態確認
                if (kernel.MailBox == null || !kernel.MailBox.Status) {
                    Logger.Set(LogKind.Error, null, 4, "");
                    return; //初期化失敗(サーバは機能しない)
                }
            }

            //ドメイン名のリスト整備
            DomainList = new List<string>();
            foreach (var s in ((string)Conf.Get("domainName")).Split(',')) {
                DomainList.Add(s);
            }
            if (DomainList.Count == 0) {
                Logger.Set(LogKind.Error, null, 3, "");
                return;//初期化失敗(サーバは機能しない)
            }

            //エリアス初期化
            Alias = new Alias(DomainList, kernel.MailBox);
            foreach (var dat in (Dat)Conf.Get("aliasList")) {
                if (dat.Enable) {
                    var name = dat.StrList[0];
                    var alias = dat.StrList[1];
                    Alias.Add(name, alias, Logger);
                }
            }

            //メールキューの初期化
            _mailQueue = new MailQueue(kernel.ProgDir());


            //SaveMail初期化
            var receivedHeader = (string)Conf.Get("receivedHeader");//Receivedヘッダ文字列
            _mailSave = new MailSave(kernel, kernel.MailBox, Logger, _mailQueue, receivedHeader, DomainList);


            var always = (bool)Conf.Get("always");//キュー常時処理
            _agent = new Agent(kernel, this, Conf, Logger, _mailQueue, always);

            //中継許可の初期化
            _relay = new Relay((Dat)Conf.Get("allowList"), (Dat)Conf.Get("denyList"), (int)Conf.Get("order"), Logger);

            //PopBeforeSmtp
            _popBeforeSmtp = new PopBeforeSmtp((bool)conf.Get("usePopBeforeSmtp"), (int)conf.Get("timePopBeforeSmtp"), kernel.MailBox);


            //usePopAccountがfalseの時、内部でmailBoxが無効化される
            _smtpAuthUserList = new SmtpAuthUserList((bool)Conf.Get("usePopAcount"), Kernel.MailBox, (Dat)Conf.Get("esmtpUserList"));
            _smtpAuthRange = new SmtpAuthRange((Dat)Conf.Get("range"), (int)Conf.Get("enableEsmtp"), Logger);

            //ヘッダ置換
            _changeHeader = new ChangeHeader((Dat)Conf.Get("patternList"), (Dat)Conf.Get("appendList"));

            //Ver5.3.3 Ver5.2以前のバージョンのカラムの違いを修正する
            var d = (Dat)Conf.Get("hostList");
            if (d.Count > 0 && d[0].StrList.Count == 6) {
                foreach (var o in d) {
                    o.StrList.Add("False");
                }
                conf.Set("hostList", d);
                conf.Save(kernel.IniDb);
            }



#if ML_SERVER
            _mlList = new MlList(kernel,this,_mailSave, DomainList);
#endif
        }



        //リモート操作（データの取得）
        override public string Cmd(string cmdStr) {

            if (!Kernel.MailBox.Status)
                return "";

            if (cmdStr == "Refresh-MailBox") {
                //キュー一覧
                var sb = new StringBuilder();
                var files = Directory.GetFiles(_mailQueue.Dir, "DF_*");
                Array.Sort(files);
                foreach (string fileName in files) {
                    var mailInfo = new MailInfo(fileName);
                    sb.Append(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "$queue", mailInfo.Uid, mailInfo.From, mailInfo.To, mailInfo.Size.ToString(), mailInfo.Date));
                    sb.Append('\b');
                }
                //ユーザメール一覧
                foreach (var user in Kernel.MailBox.UserList) {
                    var folder = string.Format("{0}\\{1}", Kernel.MailBox.Dir, user);
                    files = Directory.GetFiles(folder, "DF_*");
                    Array.Sort(files);
                    foreach (string fileName in files) {
                        var mailInfo = new MailInfo(fileName);
                        sb.Append(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", user, mailInfo.Uid, mailInfo.From, mailInfo.To, mailInfo.Size.ToString(), mailInfo.Date));
                        sb.Append('\b');
                    }
                }
                return sb.ToString();
            } else if (cmdStr.IndexOf("Cmd-View") == 0) {
                var tmp = cmdStr.Split('-');
                if (tmp.Length == 4) {
                    var folder = "";
                    var mailInfo = Search(tmp[2], tmp[3], ref folder);
                    if (mailInfo != null) {
                        var emlFileName = string.Format("{0}\\MF_{1}", folder, mailInfo.FileName);
                        var mail = new Mail();
                        mail.Read(emlFileName);
                        return Inet.FromBytes(mail.GetBytes());
                    }
                    return "ERROR";
                }
            } else if (cmdStr.IndexOf("Cmd-Delete") == 0) {
                if (ThreadBaseKind == ThreadBaseKind.Running) {
                    return "running";
                }
                var tmp = cmdStr.Split('-');
                if (tmp.Length == 4) {
                    string folder = "";
                    var mailInfo = Search(tmp[2], tmp[3], ref folder);
                    if (mailInfo != null) {
                        string fileName = string.Format("{0}\\MF_{1}", folder, mailInfo.FileName);
                        File.Delete(fileName);
                        fileName = string.Format("{0}\\DF_{1}", folder, mailInfo.FileName);
                        File.Delete(fileName);
                        return "success";
                    }
                }
            }
            return "";
        }
        MailInfo Search(string user, string uid, ref string folder) {
            folder = string.Format("{0}\\{1}", Kernel.MailBox.Dir, user);
            if (user == "QUEUE")
                folder = _mailQueue.Dir;

            foreach (var fileName in Directory.GetFiles(folder, "DF_*")) {
                var mailInfo = new MailInfo(fileName);
                if (mailInfo.Uid == uid) {
                    return mailInfo;
                }
            }
            return null;
        }

        new public void Dispose() {
#if ML_SERVER
            _mlList.Dispose();
#endif
            base.Dispose();
        }
        override protected bool OnStartServer() {
            if (_agent != null)
                _agent.Start();

            _fetch = new Fetch(Kernel, this, Conf);
            _fetch.Start();
            return true;
        }
        override protected void OnStopServer() {
            if (_agent != null)
                _agent.Stop();

            if (_fetch != null) {
                _fetch.Stop();
                _fetch = null;
            }
        }
        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {

            var sockTcp = (SockTcp)sockObj;

            //グリーティングメッセージの表示
            sockTcp.AsciiSend("220 " + Kernel.ChangeTag((string)Conf.Get("bannerMessage")));

            var checkParam = new CheckParam((bool)Conf.Get("useNullFrom"), (bool)Conf.Get("useNullDomain"));
            var session = new Session();

            SmtpAuth smtpAuth = null;

            var useEsmtp = (bool)Conf.Get("useEsmtp");
            if (useEsmtp) {
                if (_smtpAuthRange.IsHit(sockTcp.RemoteIp)) {
                    var usePlain = (bool)Conf.Get("useAuthPlain");
                    var useLogin = (bool)Conf.Get("useAuthLogin");
                    var useCramMd5 = (bool)Conf.Get("useAuthCramMD5");

                    smtpAuth = new SmtpAuth(_smtpAuthUserList, usePlain, useLogin, useCramMd5);
                }
            }

            //受信サイズ制限
            var sizeLimit = (int)Conf.Get("sizeLimit");

            //Ver5.0.0-b8 Frmo:偽造の拒否
            var useCheckFrom = (bool)Conf.Get("useCheckFrom");


            while (IsLife()) {
                Thread.Sleep(0);

                var cmd = recvCmd(sockTcp);
                if (cmd == null){
                    break;//切断された
                }
                if (cmd.Str == "") {
                    Thread.Sleep(100);//受信待機中
                    continue;
                }
                var smtpCmd = new SmtpCmd(cmd);

                if (smtpCmd.Kind == SmtpCmdKind.Unknown) {//無効コマンド

                    //SMTP認証
                    if (smtpAuth != null) {
                        if (!smtpAuth.IsFinish) {
                            var ret = smtpAuth.Job(smtpCmd.Str);
                            if (ret != null) {
                                sockTcp.AsciiSend(ret);
                                continue;
                            }
                        }
                    }
                    sockTcp.AsciiSend(string.Format("500 command not understood: {0}", smtpCmd.Str));
                    //無効コマンドが10回続くと不正アクセスとして切断する
                    session.UnknownCmdCounter++;
              
                    if (session.UnknownCmdCounter > 10) {
                        Logger.Set(LogKind.Secure, sockTcp, 54, string.Format("unknownCmdCount={0}", session.UnknownCmdCounter));
                        break;

                    }
                    continue;
                }
                session.UnknownCmdCounter = 0; //不正でない場合クリアする


                //QUIT・NOOP・RSETはいつでも受け付ける
                if (smtpCmd.Kind == SmtpCmdKind.Quit) {
                    sockTcp.AsciiSend("221 closing connection");
                    break;
                }
                if (smtpCmd.Kind == SmtpCmdKind.Noop) {
                    sockTcp.AsciiSend("250 OK");
                    continue;
                }
                if (smtpCmd.Kind == SmtpCmdKind.Rset) {
                    session.Rest();
                    sockTcp.AsciiSend("250 Reset state");
                    continue;
                }

                //下記のコマンド以外は、SMTP認証の前には使用できない
                if (smtpCmd.Kind != SmtpCmdKind.Noop && smtpCmd.Kind != SmtpCmdKind.Helo && smtpCmd.Kind != SmtpCmdKind.Ehlo && smtpCmd.Kind != SmtpCmdKind.Rset) {
                    if (smtpAuth != null) {
                        if (!smtpAuth.IsFinish) {
                            sockTcp.AsciiSend("530 Authentication required.");
                            continue;
                        }
                    }
                }

                if (smtpCmd.Kind == SmtpCmdKind.Helo || smtpCmd.Kind == SmtpCmdKind.Ehlo) {
                    if (session.Hello != null) {//HELO/EHLOは１回しか受け取らない
                        sockTcp.AsciiSend(string.Format("503 {0} Duplicate HELO/EHLO", Kernel.ServerName));
                        continue;
                    }
                    if (smtpCmd.ParamList.Count < 1) {
                        sockTcp.AsciiSend(string.Format("501 {0} requires domain address", smtpCmd.Kind.ToString().ToUpper()));
                        continue;
                    }
                    session.Hello = smtpCmd.ParamList[0];
                    Logger.Set(LogKind.Normal, sockTcp, 1, string.Format("{0} {1} from {2}[{3}]", smtpCmd.Kind.ToString().ToUpper(), session.Hello, sockObj.RemoteHostname, sockTcp.RemoteAddress));

                    if (smtpCmd.Kind == SmtpCmdKind.Ehlo) {
                        sockTcp.AsciiSend(string.Format("250-{0} Helo {1}[{2}], Pleased to meet you.", Kernel.ServerName, sockObj.RemoteHostname, sockObj.RemoteAddress));
                        sockTcp.AsciiSend("250-8BITMIME");
                        sockTcp.AsciiSend(string.Format("250-SIZE={0}", sizeLimit));
                        if (smtpAuth != null) {
                            string ret = smtpAuth.EhloStr();//SMTP認証に関するhelp文字列の取得
                            if (ret != null) {
                                sockTcp.AsciiSend(ret);
                            }
                        }
                        sockTcp.AsciiSend("250 HELP");
                    } else {
                        sockTcp.AsciiSend(string.Format("250 {0} Helo {1}[{2}], Pleased to meet you.", Kernel.ServerName, sockObj.RemoteHostname, sockObj.RemoteAddress));
                    }
                    continue;
                }

                if (smtpCmd.Kind == SmtpCmdKind.Mail) {
                    if (!checkParam.Mail(smtpCmd.ParamList)){
                        sockTcp.AsciiSend(checkParam.Message);
                        continue;
                    }

                    session.From = new MailAddress(smtpCmd.ParamList[1]);//MAILコマンドを取得完了（""もあり得る）
                    sockTcp.AsciiSend(string.Format("250 {0}... Sender ok", smtpCmd.ParamList[1]));
                    continue;
                }
                if (smtpCmd.Kind == SmtpCmdKind.Rcpt) {

                    if (session.From == null) {//RCPTの前にMAILコマンドが必要
                        sockTcp.AsciiSend("503 Need MAIL before RCPT");
                        continue;
                    }

                    if (!checkParam.Rcpt(smtpCmd.ParamList)) {
                        sockTcp.AsciiSend(checkParam.Message);
                        continue;
                    }
            
                    var mailAddress = new MailAddress(smtpCmd.ParamList[1]);

                    if (mailAddress.Domain == "") {//ドメイン指定の無い場合は、自ドメイン宛と判断する
                        mailAddress = new MailAddress(mailAddress.User, DomainList[0]);
                    }

                    //自ドメイン宛かどうかの確認
                    if (mailAddress.IsLocal(DomainList)) {
                        //Ver5.0.0-b4 エリアスで指定したユーザ名の確認
                        if (!Alias.IsUser(mailAddress.User)) {
                            //有効なユーザかどうかの確認
                            if (!Kernel.MailBox.IsUser(mailAddress.User)) {
                                //Ver_Ml
                                //有効なメーリングリスト名かどうかの確認

                                //**********************************************************************
                                //Ver_Ml
                                //**********************************************************************
#if ML_SERVER
                                if(!_mlList.IsUser(mailAddress)){
                                    this.Logger.Set(LogKind.Secure,sockTcp,6,mailAddress.User);
                                    sockTcp.AsciiSend(string.Format("550 {0}... User unknown",mailAddress.User),OperateCrlf.Yes);
                                    continue;
                                }
#else
                                Logger.Set(LogKind.Secure, sockTcp, 6, mailAddress.User);
                                sockTcp.AsciiSend(string.Format("550 {0}... User unknown", mailAddress.User));
                                continue;
#endif
                                //**********************************************************************

                            }
                        }
                    } else {//中継（リレー）が許可されているかどうかのチェック
                        if (!_popBeforeSmtp.Auth(sockObj.RemoteIp)) {
                            //Allow及びDenyリストで中継（リレー）が許可されているかどうかのチェック
                            if (!_relay.IsAllow(sockObj.RemoteIp)) {
                                sockTcp.AsciiSend(string.Format("553 {0}... Relay operation rejected", mailAddress));
                                continue;
                            }
                        }
                    }
                    //メールアドレスをRCPTリストへ追加する
                    session.RcptList.Add(mailAddress);
                    sockTcp.AsciiSend(string.Format("250 {0}... Recipient ok", mailAddress));
                    continue;
                }
                if (smtpCmd.Kind == SmtpCmdKind.Data) {
                    if (session.From == null) {
                        sockTcp.AsciiSend("503 Need MAIL command");
                        continue;
                    }
                    if (session.RcptList.Count == 0) {
                        sockTcp.AsciiSend("503 Need RCPT (recipient)");
                        continue;
                    }

                    sockTcp.AsciiSend("354 Enter mail,end with \".\" on a line by ltself");
                    
                    var data = new Data(sizeLimit);
                    if(!data.Recv(sockTcp,20,Logger,this)){
                        Thread.Sleep(1000);
                        break;
                    }
                
                    //以降は、メール受信完了の場合

                    if (useCheckFrom) {//Frmo:偽造の拒否
                        var mailAddress = new MailAddress(data.Mail.GetHeader("From"));
                        if (mailAddress.User == "") {
                            Logger.Set(LogKind.Secure, sockTcp, 52, string.Format("From:{0}", mailAddress));
                            sockTcp.AsciiSend("530 There is not an email address in a local user");
                            continue;
                        }

                        //ローカルドメインでない場合は拒否する
                        if (!mailAddress.IsLocal(DomainList)) {
                            Logger.Set(LogKind.Secure, sockTcp, 28, string.Format("From:{0}", mailAddress));
                            sockTcp.AsciiSend("530 There is not an email address in a local domain");
                            continue;
                        }
                        //有効なユーザでない場合拒否する
                        if (!Kernel.MailBox.IsUser(mailAddress.User)) {
                            Logger.Set(LogKind.Secure, sockTcp, 29, string.Format("From:{0}", mailAddress));
                            sockTcp.AsciiSend("530 There is not an email address in a local user");
                            continue;
                        }
                    }
                    
                    session.RcptList = Alias.Reflection(session.RcptList, Logger);

                    //ヘッダの変換及び追加
                    _changeHeader.Exec(data.Mail, Logger);

                    //テンポラリバッファの内容でMailオブジェクトを生成する
                    var error = false;
                    foreach (var to in session.RcptList) {
                        if (!MailSave(session.From, to, data.Mail, sockTcp.RemoteHostname, sockTcp.RemoteIp)) {//MLとそれ以外を振り分けて保存する
                            error = true;
                            break;
                        }
                    }
                    sockTcp.AsciiSend(error ? "554 MailBox Error" : "250 OK");
                    session.RcptList.Clear();
                }
            }
            if (sockTcp != null)
                sockTcp.Close();

            session.Dispose();
        }

        //メール保存(MLとそれ以外を振り分ける)
        public bool MailSave(MailAddress from, MailAddress to, Mail mail, string host, Ip addr) {
#if ML_SERVER
            if (_mlList.IsUser(to)) {
                var mlEnvelope = new MlEnvelope(from, to, host, addr);
                return _mlList.Job(mlEnvelope,mail);
            } else {
#endif
            return _mailSave.Save(from, to, mail, host, addr);
#if ML_SERVER
            }
#endif
        }


        //OneFetchで使用されているが、最終的にData.Recvに統合する
        //DATAで送られてくるデータを受信する
//        public bool RecvLines2(SockTcp sockTcp, ref List<byte[]> lines, long sizeLimit) {
//
//
//            var dtLast = DateTime.Now;//受信が20秒無かった場合は、処理を中断する
//            long linesSize = 0;//受信バッファのデータ量（受信サイズ制限に使用する）
//            var keep = new byte[0];
//            while (IsLife() && dtLast.AddSeconds(20) > DateTime.Now) {
//                var len = sockTcp.Length();
//                if (len == 0)
//                    continue;
//                var buf = sockTcp.Recv(len, Timeout, this);
//                if (buf == null)
//                    break;//切断された
//                dtLast = DateTime.Now;
//                linesSize += buf.Length;//受信データ量
//
//                //受信サイズ制限
//                if (sizeLimit != 0) {
//                    if (sizeLimit < linesSize / 1024) {
//                        Logger.Set(LogKind.Secure, sockTcp, 7, string.Format("Limit:{0}KByte", sizeLimit));
//                        sockTcp.AsciiSend("552 Requested mail action aborted: exceeded storage allocation");
//                        return false;
//                    }
//                }
//
//                //繰越がある場合
//                if (keep.Length != 0) {
//                    var tmp = new byte[buf.Length + keep.Length];
//                    Buffer.BlockCopy(keep, 0, tmp, 0, keep.Length);
//                    Buffer.BlockCopy(buf, 0, tmp, keep.Length, buf.Length);
//                    buf = tmp;
//                    keep = new byte[0];
//                }
//
//                int start = 0;
//                for (int end = 0; ; end++) {
//                    if (buf[end] == '\n') {
//                        if (1 <= end && buf[end - 1] == '\r') {
//                            var tmp = new byte[end - start + 1];//\r\nを削除しない
//                            Buffer.BlockCopy(buf, start, tmp, 0, end - start + 1);//\r\nを削除しない
//                            lines.Add(tmp);
//                            start = end + 1;
//                        }
//                    }
//                    if (end >= buf.Length - 1) {
//                        if (0 < (end - start + 1)) {
//                            //改行が検出されていないので、繰越す
//                            keep = new byte[end - start + 1];
//                            Buffer.BlockCopy(buf, start, keep, 0, end - start + 1);
//                        }
//                        break;
//                    }
//                }
//
//                //データ終了
//                //if(lines[lines.Count - 1][0] == '.' && lines[lines.Count - 1][1] == '\r' && lines[lines.Count - 1][2] == '\n') {
//                //Ver5.1.5
//                if (lines.Count >= 1 && lines[lines.Count - 1].Length >= 3) {
//                    if (lines[lines.Count - 1][0] == '.' && lines[lines.Count - 1][1] == '\r' && lines[lines.Count - 1][2] == '\n') {
//                        lines.RemoveAt(lines.Count - 1);//最終行の「.\r\n」は、破棄する
//                        return true;
//                    }
//                }
//            }
//            return false;
//        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }
}