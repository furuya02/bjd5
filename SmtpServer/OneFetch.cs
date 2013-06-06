using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace SmtpServer {
    class OneFetch {
        readonly Kernel _kernel;
        readonly Server _server;
        DateTime _dt = new DateTime(0);//最終処理時間
        readonly int _timeout;
        readonly FetchOption _fetchOption;
        readonly int _sizeLimit;


        

        public OneFetch(Kernel kernel, Server server, FetchOption fetchOption, int timeout, int sizeLimit) {
            _kernel = kernel;
            _server = server;
            _timeout = timeout;
            _sizeLimit = sizeLimit;
            _fetchOption = fetchOption;
        }

        public void Job(DateTime now, ILife iLife) {
            if (_dt.AddMinutes(_fetchOption.Interval) > now)//受信間隔を過ぎたかどうかの判断
                return;
            _server.Logger.Set(LogKind.Normal, null, 23, string.Format("{0}:{1} USER:{2} => LOCAL:{3}", _fetchOption.Host, _fetchOption.Port, _fetchOption.User, _fetchOption.LocalUser));
            Ssl ssl = null;
            //var ip = new Ip(_fetchOption.Host);
            //if (ip.ToString() == "0.0.0.0") {
            //    var tmp = Lookup.QueryA(_fetchOption.Host);
            //    if (tmp.Count > 0)
            //        ip = new Ip(tmp[0]);
            //}
            Ip ip=null;
            try{
                ip = new Ip(_fetchOption.Host);
            }catch(ValidObjException){
                var tmp = Lookup.QueryA(_fetchOption.Host);
                try{
                    if (tmp.Count > 0)
                        ip = new Ip(tmp[0]);
                }catch(ValidObjException){
                    //ERROR
                }
            }
            int timeout=3;
            var tcpObj = Inet.Connect(_kernel,ip, _fetchOption.Port, timeout,ssl);
            if (tcpObj == null) {
                _server.Logger.Set(LogKind.Error, null, 24, string.Format("{0}:{1} USER:{2} => LOCAL:{3}", _fetchOption.Host, _fetchOption.Port, _fetchOption.User, _fetchOption.LocalUser));
                goto end;
            }
            Recv(tcpObj, _server.Logger,iLife);
        end:
            _dt = DateTime.Now;//最終処理時刻の更新
        }

        enum FetchState {
            User = 1,
            Pass = 2,
            Uidl = 3,
            Uidl2 = 4,
            Retr = 5,
            Retr2 = 6,
            Dele = 7,
            Job = 8,
            Quit = 9
        }
        void Recv(SockTcp sockTcp, Logger logger, ILife iLife) {
            //var fetchDb = new FetchDb(string.Format("{0}\\fetch.{1}.{2}.db", _kernel.ProgDir(), _fetchOption.Host, _fetchOption.User));
            var fetchDb = new FetchDb(_kernel.ProgDir(), _fetchOption.Host, _fetchOption.User);
            var fetchState = FetchState.User;
            var remoteUidList = new List<string>();
            var getList = new List<int>();//取得するメールのリスト
            var delList = new List<int>();//削除するメールのリストs

            //ターゲット
            var n = 0;
            //メール受信バッファ
            var mail = new Mail(logger);
            var delJob = false;//削除リスト生成が終わったかどうか（削除リストdelListはgetListの処理が全部終わってから作成される）

            while (iLife.IsLife()) {
                if (fetchState == FetchState.Retr2) { //Ver5.1.4 データ受信
                    var lines = new List<byte[]>();//DATA受信バッファ
                    if (!_server.RecvLines(sockTcp, ref lines, _sizeLimit)) {
                        //DATA受信中にエラーが発生した場合は、直ちに切断する
                        Thread.Sleep(1000);
                        break;
                    }
                    //受信が有効な場合
                    foreach (byte[] line in lines) {
                        if (mail.Init(line)) {
                            //ヘッダ終了時の処理
                        }
                    }
                    var from = new MailAddress(mail.GetHeader("From"));
                    //MailAddress to = new MailAddress(mail.GetHeader("To"));
                    //Ver5.6.0 string dateStr = mail.GetHeader("Date");
                    mail.ConvertHeader("X-UIDL", remoteUidList[n]);
                    var remoteAddr = sockTcp.RemoteIp;
                    var remoteHost = _kernel.DnsCache.GetHostName(remoteAddr.IPAddress, logger);
                        
                    //Ver5.6.0 MailInfo mailInfo = new MailInfo(remoteUidList[n], mail.Length, remoteHost, remoteAddr, dateStr, from, to);
                    //Ver5.6.0 if (!kernel.MailBox.Save(fetchOption.LocalUser, mail, mailInfo))
                    //Ver5.6.0     break;
                    //Ver5.6.0
                    var rcptList = new RcptList();
                    rcptList.Add(new MailAddress(_fetchOption.LocalUser, _server.DomainList[0]));
                    var error = false;
                    foreach (var to in _server.Alias.Reflection(rcptList,_server.Logger)) {
                        if (_server.MailSave(@from, to, mail, remoteHost, remoteAddr))
                            continue;
                        error = true;
                        break;
                    }
                    if (error) {
                        break;
                    }


                    fetchDb.Add(remoteUidList[n]);
                    fetchState = FetchState.Job;
                    //continue;
                } else {
                    //********************************************************************
                    // サーバからのレスポンス(+OK -ERR)受信
                    //********************************************************************
                    var data = sockTcp.LineRecv(_timeout,iLife);
                    if (data == null) //切断された
                        break;
                    if (data.Length == 0) {
                        Thread.Sleep(10);
                        continue;
                    }
                    //recvBuf = Inet.TrimCRLF(recvBuf);//\r\nの排除
                    var recvStr = Encoding.ASCII.GetString(data);
                    recvStr = Inet.TrimCrlf(recvStr);//\r\nの排除
                    //********************************************************************
                    // 受信したレスポンスコードによる処置
                    //********************************************************************
                    if (fetchState == FetchState.User || fetchState == FetchState.Pass || fetchState == FetchState.Uidl || fetchState == FetchState.Retr || fetchState == FetchState.Dele) {
                        if (recvStr.IndexOf("+OK") != 0)
                            break;//エラー発生
                    }
                    //********************************************************************
                    // 各モードごとの動作
                    //********************************************************************
                    if (fetchState == FetchState.User) {
                        sockTcp.AsciiSend(string.Format("USER {0}", _fetchOption.User));
                        fetchState = FetchState.Pass;
                    } else if (fetchState == FetchState.Pass) {
                        sockTcp.AsciiSend(string.Format("PASS {0}", _fetchOption.Pass));
                        fetchState = FetchState.Uidl;
                    } else if (fetchState == FetchState.Uidl) {
                        sockTcp.AsciiSend("UIDL");
                        n = 0;
                        fetchState = FetchState.Uidl2;
                    } else if (fetchState == FetchState.Retr) {
                        fetchState = FetchState.Retr2;
                    } else if (fetchState == FetchState.Dele) {
                        fetchDb.Del(remoteUidList[n]);
                        fetchState = FetchState.Job;
                    } else if (fetchState == FetchState.Uidl2) {
                        if (recvStr.IndexOf("+OK") != 0) {
                            if (recvStr == ".") {
                                fetchState = FetchState.Job;
                            } else {
                                var tmp = recvStr.Split(' ');
                                if (tmp.Length != 2)
                                    break;
                                var uid = tmp[1];

                                remoteUidList.Add(uid);
                                //既に受信が完了しているかどうかデータベースで確認する
                                if (fetchDb.IndexOf(uid) == -1) {
                                    //存在しない場合
                                    getList.Add(n);//受信対象とする
                                }
                                n++;
                            }
                        }
                        //} else if (mode == MODE.RETR2) {
                        //    if (recvStr == ".") {
                        //        MailAddress from = new MailAddress(mail.GetHeader("From"));
                        //        MailAddress to = new MailAddress(mail.GetHeader("To"));
                        //        string dateStr = mail.GetHeader("Date");
                        //        mail.ConvertHeader("X-UIDL",remoteUidList[n]);
                        //        Ip remoteAddr = new Ip(sockTcp.RemoteEndPoint.Address.ToString());
                        //        string remoteHost = kernel.dnsCache.Get(remoteAddr.IPAddress);
                        //        MailInfo mailInfo = new MailInfo(remoteUidList[n],mail.Length,remoteHost,remoteAddr,dateStr,from,to);
                        //        if (!kernel.MailBox.Save(localUser,mail,mailInfo))
                        //            break;
                        //        fetchDb.Add(remoteUidList[n]);
                        //        mode = MODE.JOB;
                        //    } else {
                        //        mail.Init(data);
                        //    }
                    } else if (fetchState == FetchState.Quit) {
                        break;
                    }
                }
                if (fetchState == FetchState.Job) {
                    if (getList.Count == 0 && !delJob) { //削除リスト生成がまだ処理されていない場合
                        delJob = true;
                        if (_fetchOption.Synchronize == 0) { //サーバに残す
                            for (var i = 0; i < remoteUidList.Count; i++) {
                                //保存期間が過ぎているかどうかを確認する
                                if (fetchDb.IsPast(remoteUidList[i], _fetchOption.KeepTime)) { //サーバに残す時間（分）
                                    delList.Add(i);
                                }
                            }
                        } else if (_fetchOption.Synchronize == 1) { //メールボックスと同期する
                            //メールボックスの状態を取得する
                            var localUidList = new List<string>();
                            var folder = string.Format("{0}\\{1}", _kernel.MailBox.Dir, _fetchOption.LocalUser);
                            foreach (var fileName in Directory.GetFiles(folder, "DF_*")) {
                                var mailInfo = new MailInfo(fileName);
                                localUidList.Add(mailInfo.Uid);
                            }
                            //メールボックスに存在しない場合、削除対象になる
                            for (var i = 0; i < remoteUidList.Count; i++) {
                                if (localUidList.IndexOf(remoteUidList[i]) == -1) {
                                    delList.Add(i);
                                }
                            }
                        } else if (_fetchOption.Synchronize == 2) { //サーバから削除
                            //受信完了リストに存在する場合、削除対象になる
                            for (var i = 0; i < remoteUidList.Count; i++) {
                                if (fetchDb.IndexOf(remoteUidList[i]) != -1) {
                                    delList.Add(i);
                                }
                            }
                        }
                    }

                    //getListが存在する場合
                    if (getList.Count > 0) {
                        n = getList[0];
                        getList.RemoveAt(0);
                        sockTcp.AsciiSend(string.Format("RETR {0}", n + 1));
                        mail = new Mail(_server.Logger);
                        fetchState = FetchState.Retr;
                    } else if (delList.Count > 0) {
                        n = delList[0];
                        delList.RemoveAt(0);
                        sockTcp.AsciiSend(string.Format("DELE {0}", n + 1));
                        fetchState = FetchState.Dele;
                    } else {
                        sockTcp.AsciiSend("QUIT");
                        fetchState = FetchState.Quit;
                    }
                }
            }
            fetchDb.Save();
        }
    }
}
