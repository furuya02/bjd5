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

        public List<string> DomainList{get;private set;}
        readonly MailQueue _mailQueue;
        readonly MailSave _mailSave;
        readonly Agent _agent;//キュー処理スレッド
        Fetch _fetch;//自動受信
        public Alias Alias{get;private set;}//エリアス

        readonly Relay _relay;//中継許可

#if ML_SERVER
        readonly MlList _mlList;//MLリスト
#endif

        //コンストラクタ
        public Server(Kernel kernel, Conf conf, OneBind oneBind)
            : base(kernel,conf, oneBind) {

            //Ver5.8.9
            if (kernel.RunMode == RunMode.Normal || kernel.RunMode == RunMode.Service){
                //メールボックスの初期化状態確認
                if (kernel.MailBox == null || !kernel.MailBox.Status){
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
            foreach (var dat in (Dat) Conf.Get("aliasList")){
                if (dat.Enable){
                    var name = dat.StrList[0];
                    var alias = dat.StrList[1];
                    Alias.Add(name,alias,Logger);
                }
            }

            //メールキューの初期化
            _mailQueue = new MailQueue(kernel.ProgDir());


            //SaveMail初期化
            var receivedHeader = (string)Conf.Get("receivedHeader");//Receivedヘッダ文字列
            _mailSave = new MailSave(kernel,kernel.MailBox, Logger, _mailQueue, receivedHeader, DomainList);


            var always = (bool)Conf.Get("always");//キュー常時処理
            _agent = new Agent(kernel, this,Conf,Logger, _mailQueue, always);

            //中継許可の初期化
            _relay = new Relay((Dat)Conf.Get("allowList"), (Dat)Conf.Get("denyList"), (int)Conf.Get("order"), Logger);


            //Ver5.3.3 Ver5.2以前のバージョンのカラムの違いを修正する
            var d = (Dat)Conf.Get("hostList");
            if (d.Count > 0 && d[0].StrList.Count == 6) {
                foreach (var o in d){
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
                string[] tmp = cmdStr.Split('-');
                if (tmp.Length == 4) {
                    var folder = "";
                    var mailInfo = Search(tmp[2], tmp[3], ref folder);
                    if (mailInfo != null) {
                        var emlFileName = string.Format("{0}\\MF_{1}", folder, mailInfo.FileName);
                        var mail = new Mail(Logger);
                        mail.Read(emlFileName);
                        return Inet.FromBytes(mail.GetBytes());
                    }
                    return "ERROR";
                }
            } else if (cmdStr.IndexOf("Cmd-Delete") == 0) {
                if(ThreadBaseKind == ThreadBaseKind.Running){
                    return "running";
                } else {

                    string[] tmp = cmdStr.Split('-');
                    if (tmp.Length == 4) {
                        string folder = "";
                        MailInfo mailInfo = Search(tmp[2], tmp[3], ref folder);
                        if (mailInfo != null) {
                            string fileName = string.Format("{0}\\MF_{1}", folder, mailInfo.FileName);
                            File.Delete(fileName);
                            fileName = string.Format("{0}\\DF_{1}", folder, mailInfo.FileName);
                            File.Delete(fileName);
                            return "success";
                        }
                    }
                }
            }
            return "";
        }
        MailInfo Search(string user, string uid, ref string folder) {
            folder = string.Format("{0}\\{1}", Kernel.MailBox.Dir, user);
            if (user == "QUEUE")
                folder = _mailQueue.Dir;

            foreach (string fileName in Directory.GetFiles(folder, "DF_*")) {
                var mailInfo = new MailInfo(fileName);
                if (mailInfo.Uid == uid) {
                    return mailInfo;
                }
            }
            return null;
        }

        enum SmtpCmd {
            Quit,
            Noop,
            Helo,
            Ehlo,
            Mail,
            Rcpt,
            Rset,
            Data,
            Auth,
            Unknown
        }

        enum SmtpMode {
            Command = 0,
            Data = 1
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

            _fetch = new Fetch(Kernel,this,Conf);
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

            string helo = null;//nullの場合、HELO未受信
            var rcptList = new RcptList();
            MailAddress from = null;//nullの場合、MAILコマンドをまだ受け取っていない
            var smtpAuthServer = new SmtpAuthServer(Logger, Kernel.MailBox, Conf, sockTcp);//SMTP認証オブジェクト

            var mode = SmtpMode.Command;

            var unknownCmdCount = 0;//無効コマンドのカウント

            //データ受信用バッファ
            Mail mail = null;
            //受信サイズ制限
            var sizeLimit = (int)Conf.Get("sizeLimit");

            //Ver5.0.0-b8 Frmo:偽造の拒否
            var useCheckFrom = (bool)Conf.Get("useCheckFrom");

            //ヘッダ置換
            var patternList = new StrList((Dat)Conf.Get("patternList"));
            //ヘッダ追加
            var appendList = new StrList((Dat)Conf.Get("appendList"));

            while (IsLife()) {
                Thread.Sleep(0);
                //string str="";

                if (mode != SmtpMode.Command) {//データモード

                    var lines = new List<byte[]>();//DATA受信バッファ
                    if (!RecvLines(sockTcp, ref lines, sizeLimit)) {
                        //Ver5.0.1
                        //DATA受信中にエラーが発生した場合は、直ちに切断する

                        //Ver5.0.3 552を送り切るまで待機
                        Thread.Sleep(1000);
                        break;
                    }
                    //受信が有効な場合
                    foreach (byte[] line in lines) {
                        if (mail.Init(line)) {
                            //ヘッダ終了時の処理
                            //Ver5.0.0-b8 Frmo:偽造の拒否
                            if (useCheckFrom) {
                                var mailAddress = new MailAddress(mail.GetHeader("From"));
                                //Ver5.4.3
                                if (mailAddress.User == "") {
                                    Logger.Set(LogKind.Secure, sockTcp, 52, string.Format("From:{0}", mailAddress));
                                    sockTcp.AsciiSend("530 There is not an email address in a local user");
                                    mode = SmtpMode.Command;
                                    break;
                                }

                                //ローカルドメインでない場合は拒否する
                                if (!mailAddress.IsLocal(DomainList)) {
                                    Logger.Set(LogKind.Secure, sockTcp, 28, string.Format("From:{0}", mailAddress));
                                    sockTcp.AsciiSend("530 There is not an email address in a local domain");
                                    mode = SmtpMode.Command;
                                    break;
                                }
                                //有効なユーザでない場合拒否する
                                if (!Kernel.MailBox.IsUser(mailAddress.User)) {
                                    Logger.Set(LogKind.Secure, sockTcp, 29, string.Format("From:{0}", mailAddress));
                                    sockTcp.AsciiSend("530 There is not an email address in a local user");
                                    mode = SmtpMode.Command;
                                    break;
                                }
                            }
                        }
                    }
                    if (mode == SmtpMode.Data) {
                        //テンポラリバッファの内容でMailオブジェクトを生成する
                        bool error = false;
                        //ヘッダ変換
                        for (int i = 0; i < patternList.Max; i++) {
                            if (mail.RegexHeader(patternList.Tag(i), patternList.Str(i))) {
                                Logger.Set(LogKind.Normal, sockTcp, 16, string.Format("{0} -> {1}", patternList.Tag(i), patternList.Str(i)));
                            }
                        }
                        //ヘッダの追加
                        for (int i = 0; i < appendList.Max; i++) {
                            mail.AddHeader(appendList.Tag(i), appendList.Str(i));
                            Logger.Set(LogKind.Normal, sockTcp, 17, string.Format("{0}: {1}", appendList.Tag(i), appendList.Str(i)));
                        }
                        foreach (MailAddress to in rcptList) {
                            if (!MailSave(@from, to, mail, sockTcp.RemoteHostname,sockTcp.RemoteIp)) {//MLとそれ以外を振り分けて保存する
                                error = true;
                                break;
                            }
                        }
                        //Ver5.5.6 DATAコマンドでメールを受け取った時点でRCPTリストクリアする
                        rcptList.Clear();

                        sockTcp.AsciiSend(error ? "554 MailBox Error" : "250 OK");
                    }
                    mode = SmtpMode.Command;
                    continue;

                }
                //以下コマンドモード mode == MODE.COMMEND

                var cmdStr = "";
                var paramStr2 = "";
                var str = "";

                if (!RecvCmd(sockTcp, ref str, ref cmdStr, ref paramStr2))
                    break;//切断された
                if (str == "") {
                    Thread.Sleep(100);//受信待機中
                    continue;
                }

                //コマンド文字列の解釈
                var smtpCmd = SmtpCmd.Unknown;
                foreach (SmtpCmd n in Enum.GetValues(typeof(SmtpCmd))) {
                    if (n.ToString().ToUpper() == cmdStr.ToUpper()) {
                        smtpCmd = n;
                        break;
                    }
                }
                if (smtpCmd == SmtpCmd.Unknown) {//無効コマンド

                    //SMTP認証
                    string ret = smtpAuthServer.Set(str);
                    if (ret != null) {
                        sockTcp.AsciiSend(ret);
                        continue;
                    }


                    sockTcp.AsciiSend(string.Format("500 command not understood: {0}", str));

                    //Ver5.4.7
                    unknownCmdCount++;
                    if (unknownCmdCount > 10) {
                        Logger.Set(LogKind.Secure, sockTcp, 54, string.Format("unknownCmdCount={0}", unknownCmdCount));
                        break;

                    }

                    continue;
                }
                //パラメータ分離
                var paramList = new List<string>();
                if (paramStr2 != null) {
                    foreach (var s in paramStr2.Split(new char[2] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries)) {
                        paramList.Add(s.Trim(' '));
                    }
                }

                //QUITはいつでも受け付ける
                if (smtpCmd == SmtpCmd.Quit) {
                    sockTcp.AsciiSend("221 closing connection");
                    break;
                }
                if (smtpCmd == SmtpCmd.Noop) {
                    sockTcp.AsciiSend("250 OK");
                    continue;
                }
                if (smtpCmd == SmtpCmd.Rset) {
                    from = null;
                    rcptList.Clear();

                    sockTcp.AsciiSend("250 Reset state");
                    continue;
                }


                if (smtpCmd == SmtpCmd.Helo || smtpCmd == SmtpCmd.Ehlo) {
                    if (helo != null) {//HELO/EHLOは１回しか受け取らない
                        sockTcp.AsciiSend(string.Format("503 {0} Duplicate HELO/EHLO", Kernel.ServerName));
                        continue;
                    }
                    if (paramList.Count < 1) {
                        sockTcp.AsciiSend(string.Format("501 {0} requires domain address", smtpCmd.ToString().ToUpper()));
                        continue;
                    }
                    helo = paramList[0];
                    //Ver5.4.1
                    //this.Logger.Set(LogKind.Normal,sockTcp,1,string.Format("{0} {1} from {2}[{3}]",cmd,helo,remoteInfo.Host,remoteInfo.Addr));
                    Logger.Set(LogKind.Normal, sockTcp, 1, string.Format("{0} {1} from {2}[{3}]", smtpCmd.ToString().ToUpper(), helo, sockObj.RemoteHostname, sockTcp.RemoteAddress));

                    if (smtpCmd == SmtpCmd.Ehlo) {
                        //Ver5.4.1
                        //sockTcp.AsciiSend(string.Format("250-{0} Helo {1}[{2}], Pleased to meet you.", kernel.ServerName, remoteInfo.Host, remoteInfo.Addr), OPERATE_CRLF.YES);
                        sockTcp.AsciiSend(string.Format("250-{0} Helo {1}[{2}], Pleased to meet you.", Kernel.ServerName, sockObj.RemoteHostname, sockObj.RemoteAddress));
                        sockTcp.AsciiSend("250-8BITMIME");
                        sockTcp.AsciiSend(string.Format("250-SIZE={0}", sizeLimit));

                        string ret = smtpAuthServer.EhloStr();//SMTP認証に関するhelp文字列の取得
                        if (ret != null)
                            sockTcp.AsciiSend(ret);

                        sockTcp.AsciiSend("250 HELP");
                    } else {
                        //sockTcp.AsciiSend(string.Format("250 {0} Helo {1}[{2}], Pleased to meet you.", kernel.ServerName, remoteInfo.Host, remoteInfo.Addr), OPERATE_CRLF.YES);
                        sockTcp.AsciiSend(string.Format("250 {0} Helo {1}[{2}], Pleased to meet you.", Kernel.ServerName, sockObj.RemoteHostname, sockObj.RemoteAddress));
                    }
                    continue;
                }
                if (smtpCmd == SmtpCmd.Auth) {
                    //AUTHコマンドに対する処理
                    sockTcp.AsciiSend(smtpAuthServer.SetType(paramList));
                    continue;
                }
                if (smtpCmd == SmtpCmd.Mail) {
                    if (!smtpAuthServer.Finish) {
                        sockTcp.AsciiSend("530 Authentication required.");
                        continue;
                    }
                    if (paramList.Count < 2) {
                        sockTcp.AsciiSend("501 Syntax error in parameters scanning \"\"");
                        continue;
                    }

                    if (paramList[0].ToUpper() != "FROM") {
                        sockTcp.AsciiSend("501 Syntax error in parameters scanning \"MAIL\"");
                        continue;
                    }

                    //Ver5.6.0 \bをエラーではじく
                    if (paramList[1].IndexOf('\b') != -1) {
                        sockTcp.AsciiSend("501 Syntax error in parameters scanning \"From\"");
                        continue;
                    }
                    var mailAddress = new MailAddress(paramList[1]);
                    
                    if (mailAddress.User == "" && mailAddress.Domain == "") {
                        //空白のFROM(MAIN From:<>)を許可するかどうかをチェックする
                        var useNullFrom = (bool)Conf.Get("useNullFrom");
                        if (!useNullFrom) {
                            sockTcp.AsciiSend("501 Syntax error in parameters scanning \"From\"");
                            continue;
                        }
                    } else {
                        if (mailAddress.User == "") {
                            sockTcp.AsciiSend("501 Syntax error in parameters scanning \"MailAddress\"");
                            continue;
                        }
                        //ドメイン名の無いFROMを許可するかどうかのチェック
                        var useNullDomain = (bool)Conf.Get("useNullDomain");
                        if (!useNullDomain && mailAddress.Domain == "") {
                            sockTcp.AsciiSend(string.Format("553 {0}... Domain part missing", paramList[1]));
                            continue;
                        }
                    }
                    from = mailAddress;//MAILコマンドを取得完了（""もあり得る）
                    sockTcp.AsciiSend(string.Format("250 {0}... Sender ok", paramList[1]));
                    continue;
                }
                if (smtpCmd == SmtpCmd.Rcpt) {
                    if (paramList.Count < 2) {
                        sockTcp.AsciiSend("501 Syntax error in parameters scanning \"\"");
                        continue;
                    }

                    if (from == null) {//RCPTの前にMAILコマンドが必要
                        sockTcp.AsciiSend("503 Need MAIL before RCPT");
                        continue;
                    }

                    //RCPT の後ろが　FROM:メールアドレスになっているかどうかを確認する
                    if (paramList[0].ToUpper() != "TO") {
                        sockTcp.AsciiSend("501 Syntax error in parameters scanning \"RCPT\"");
                        continue;
                    }
                    if (0 <= paramList[1].IndexOf('!')) {
                        str = string.Format("553 5.3.0 {0}... UUCP addressing is not supported", paramList[1]);
                        Logger.Set(LogKind.Secure, sockTcp, 18, str);
                        sockTcp.AsciiSend(str);
                        continue;
                    }

                    //Ver5.6.0 \bをエラーではじく
                    if (paramList[1].IndexOf('\b') != -1) {
                        sockTcp.AsciiSend("501 Syntax error in parameters scanning \"From\"");
                        continue;
                    }
                    var mailAddress = new MailAddress(paramList[1]);
                    if (mailAddress.User == "") {
                        sockTcp.AsciiSend("501 Syntax error in parameters scanning \"MailAddress\"");
                        continue;
                    }
                    if (mailAddress.Domain == "")//ドメイン指定の無い場合は、自ドメイン宛と判断する
                        mailAddress = new MailAddress(mailAddress.User, DomainList[0]);

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
                        //PopBeforeSmtpで認証されているかどうかのチェック
                        if (!CheckPopBeforeSmtp(sockObj.RemoteIp)) {
                            //Allow及びDenyリストで中継（リレー）が許可されているかどうかのチェック
                            //if (!CheckAllowDenyList(sockObj.RemoteIp)) {
                            if (!_relay.IsAllow(sockObj.RemoteIp)) {
                                sockTcp.AsciiSend(string.Format("553 {0}... Relay operation rejected", mailAddress));
                                continue;
                            }
                        }
                    }
                    //メールアドレスをRCPTリストへ追加する
                    rcptList.Add(mailAddress);
                    sockTcp.AsciiSend(string.Format("250 {0}... Recipient ok", mailAddress));
                    continue;
                }
                if (smtpCmd == SmtpCmd.Data) {
                    if (from == null) {
                        sockTcp.AsciiSend("503 Need MAIL command");
                        continue;
                    }
                    if (rcptList.Count == 0) {
                        sockTcp.AsciiSend("503 Need RCPT (recipient)");
                        continue;
                    }

                    rcptList = Alias.Reflection(rcptList,Logger);

                    sockTcp.AsciiSend("354 Enter mail,end with \".\" on a line by ltself");
                    mode = SmtpMode.Data;

                    if (mail != null) {
                        mail.Dispose();
                    }
                    mail = new Mail(Logger);//受信バッファの初期化
                }
            }
            if (sockTcp != null)
                sockTcp.Close();

            if (mail != null) {
                mail.Dispose();
            }
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

        //DATAで送られてくるデータを受信する
        public bool RecvLines(SockTcp sockTcp, ref List<byte[]> lines, long sizeLimit) {


            var dtLast = DateTime.Now;//受信が20秒無かった場合は、処理を中断する
            long linesSize = 0;//受信バッファのデータ量（受信サイズ制限に使用する）
            var keep = new byte[0];
            while (IsLife() && dtLast.AddSeconds(20) > DateTime.Now) {
                var len = sockTcp.Length();
                if (len == 0)
                    continue;
                var buf = sockTcp.Recv(len, Timeout,this);
                if (buf == null)
                    break;//切断された
                dtLast = DateTime.Now;
                linesSize += buf.Length;//受信データ量

                //受信サイズ制限
                if (sizeLimit != 0) {
                    if (sizeLimit < linesSize / 1024) {
                        Logger.Set(LogKind.Secure, sockTcp, 7, string.Format("Limit:{0}KByte", sizeLimit));
                        sockTcp.AsciiSend("552 Requested mail action aborted: exceeded storage allocation");
                        return false;
                    }
                }

                //繰越がある場合
                if (keep.Length != 0) {
                    var tmp = new byte[buf.Length + keep.Length];
                    Buffer.BlockCopy(keep, 0, tmp, 0, keep.Length);
                    Buffer.BlockCopy(buf, 0, tmp, keep.Length, buf.Length);
                    buf = tmp;
                    keep = new byte[0];
                }

                int start = 0;
                for (int end = 0; ; end++) {
                    if (buf[end] == '\n') {
                        if (1 <= end && buf[end - 1] == '\r') {
                            var tmp = new byte[end - start + 1];//\r\nを削除しない
                            Buffer.BlockCopy(buf, start, tmp, 0, end - start + 1);//\r\nを削除しない
                            lines.Add(tmp);
                            start = end + 1;
                        }
                    }
                    if (end >= buf.Length - 1) {
                        if (0 < (end - start + 1)) {
                            //改行が検出されていないので、繰越す
                            keep = new byte[end - start + 1];
                            Buffer.BlockCopy(buf, start, keep, 0, end - start + 1);
                        }
                        break;
                    }
                }

                //データ終了
                //if(lines[lines.Count - 1][0] == '.' && lines[lines.Count - 1][1] == '\r' && lines[lines.Count - 1][2] == '\n') {
                //Ver5.1.5
                if (lines.Count >= 1 && lines[lines.Count - 1].Length >= 3) {
                    if (lines[lines.Count - 1][0] == '.' && lines[lines.Count - 1][1] == '\r' && lines[lines.Count - 1][2] == '\n') {
                        lines.RemoveAt(lines.Count - 1);//最終行の「.\r\n」は、破棄する
                        return true;
                    }
                }
            }
            return false;
        }

      //PopBeforeSmtpで認証されているかどうかのチェック
        bool CheckPopBeforeSmtp(Ip addr) {
            var usePopBeforeSmtp = (bool)Conf.Get("usePopBeforeSmtp");
            if (usePopBeforeSmtp) {
                var span = DateTime.Now - Kernel.MailBox.LastLogin(addr);//最終ログイン時刻からの経過時間を取得
                var sec = (int)span.TotalSeconds;//経過秒
                if (0 < sec && sec < (int)Conf.Get("timePopBeforeSmtp")) {
                    return true;//認証されている
                }
            }
            return false;
        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }
}