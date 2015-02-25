using System;
using System.Collections.Generic;
using System.IO;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Debug = System.Diagnostics.Debug;

namespace SmtpServer {

    class OneFetchJob:LastError,IDisposable{
        private readonly OneFetch _oneFetch;//オプション
        private readonly Kernel _kernel;

        private readonly MailSave _mailSave;
        DateTime _dt = new DateTime(0);//最終処理時間
        private readonly int _timeout;
        private readonly int _sizeLimit;
        private readonly String _domainName;

        public OneFetchJob(Kernel kernel, MailSave mailSave,String domainName,OneFetch oneFetch, int timeout, int sizeLimit) {
            _kernel = kernel;
            _mailSave = mailSave;
            _domainName = domainName;
            _oneFetch = oneFetch;
            _timeout = timeout;
            _sizeLimit = sizeLimit;
        }

        //RETRの後のメールの保存が完成したら、Job2をこちらに乗せ換えられる
        public bool Job(Logger logger,DateTime now,ILife iLife){
            Debug.Assert(logger != null, "logger != null");

            var fetchDb = new FetchDb(_kernel.ProgDir(), _oneFetch.Name);
            var remoteUidList = new List<String>();
            var getList = new List<int>();//取得するメールのリスト
            var delList = new List<int>();//削除するメールのリスト

            //受信間隔を過ぎたかどうかの判断
            if (_dt.AddMinutes(_oneFetch.Interval) > now){
                return false;
            }

            logger.Set(LogKind.Normal, null, 1, _oneFetch.ToString());
            if (_oneFetch.Ip == null) {
                logger.Set(LogKind.Error, null, 2, "");
                return false;
            }
            var popClient = new PopClient(_kernel,_oneFetch.Ip,_oneFetch.Port,_timeout,iLife);
            //接続
            if (!popClient.Connect()){
                logger.Set(LogKind.Error, null, 3, popClient.GetLastError());
                return false;
            }
            //ログイン
            if (!popClient.Login(_oneFetch.User, _oneFetch.Pass)){
                logger.Set(LogKind.Error, null, 4, popClient.GetLastError());
                return false;
            }
            //UID
            var lines = new List<String>();
            if (!popClient.Uidl(lines)) {
                logger.Set(LogKind.Error, null, 5, popClient.GetLastError());
                return false;
            }
            for (int i=0;i<lines.Count;i++){
                var tmp = lines[i].Split(' ');
                if (tmp.Length == 2){
                    var uid = tmp[1];
                    remoteUidList.Add(uid);

                    //既に受信が完了しているかどうかデータベースで確認する
                    if (fetchDb.IndexOf(uid) == -1) {
                        //存在しない場合
                        getList.Add(i);//受信対象とする
                    }
                }
            }
            if (_oneFetch.Synchronize == 0) { //サーバに残す
                for (var i = 0; i < remoteUidList.Count; i++) {
                    if (_oneFetch.KeepTime != 0){ //保存期間0の時は、削除しない
                        //保存期間が過ぎているかどうかを確認する
                        if (fetchDb.IsPast(remoteUidList[i], _oneFetch.KeepTime * 60)) { //サーバに残す時間（分）
                            delList.Add(i);
                        }
                    }
                }
            } else if (_oneFetch.Synchronize == 1) { //メールボックスと同期する
                //メールボックスの状態を取得する
                var localUidList = new List<string>();
                var folder = string.Format("{0}\\{1}", _kernel.MailBox.Dir, _oneFetch.LocalUser);
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
            } else if (_oneFetch.Synchronize == 2) { //サーバから削除
                //受信完了リストに存在する場合、削除対象になる
                for (var i = 0; i < remoteUidList.Count; i++) {
                    if (fetchDb.IndexOf(remoteUidList[i]) != -1) {
                        delList.Add(i);
                    }
                }
            }
            //RETR
            for (int i = 0; i < getList.Count;i++ ){
                var mail = new Mail();
                if (!popClient.Retr(getList[i], mail)) {
                    logger.Set(LogKind.Error, null, 6, popClient.GetLastError());
                    return false;
                }
                //Ver5.9.8
                var fromStr = mail.GetHeader("From");
                if (fromStr == null){
                    fromStr = string.Format("{0}@{1}_{2}", _oneFetch.User, _oneFetch.Host, _oneFetch.Port);
                }

                var from = new MailAddress(fromStr);
                mail.ConvertHeader("X-UIDL", remoteUidList[i]);
                var remoteAddr = _oneFetch.Ip;
                var remoteHost = _oneFetch.Host;

                var rcptList = new List<MailAddress>();
                rcptList.Add(new MailAddress(_oneFetch.LocalUser, _domainName));

                if (_mailSave != null){
                    if (!_mailSave.Save(from, rcptList, mail, remoteHost, remoteAddr)){
                        return false;
                    }
                }

                fetchDb.Add(remoteUidList[i]);
            }
            //DELE
            for (int i = 0; i < delList.Count;i++ ){
                if (!popClient.Dele(delList[i])){
                    logger.Set(LogKind.Error, null, 7, popClient.GetLastError());
                    return false;
                }
                fetchDb.Del(remoteUidList[i]);
            }
            //QUIT
            if (!popClient.Quit()){
                logger.Set(LogKind.Error, null, 5, popClient.GetLastError());
                return false;
            }
            fetchDb.Save();
            _dt = DateTime.Now;//最終処理時刻の更新
            return true;
        }

        public void Dispose(){
            ;
        }
    }
}
