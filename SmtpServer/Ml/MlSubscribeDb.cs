using System;
using System.Collections.Generic;
using System.IO;
using Bjd;
using Bjd.mail;

namespace SmtpServer {
    class MlSubscribeDb : IDisposable {
        readonly string _fileName;
        readonly List<OneSubscribe> _ar = new List<OneSubscribe>();
        readonly Random _random = new Random();

        static readonly object SyncObj = new object();
        readonly double _effectiveMsec;//有効時間(msec)

        public MlSubscribeDb(string manageDir, string mlName,double effectiveMsec) {
            _effectiveMsec = effectiveMsec;                  
            _fileName = string.Format("{0}\\{1}.subscribe.db", manageDir,mlName);
            if (!File.Exists(_fileName))
                return;
            using (var sr = new StreamReader(_fileName)) {
                try {
                    while (true) {
                        var str = sr.ReadLine();
                        if (str == null)
                            break;
                        var oneSubscribe = new OneSubscribe(null, null, null);
                        if (oneSubscribe.FromString(str)) {
                            _ar.Add(oneSubscribe);
                        }
                    }
                } catch (Exception){
                        
                }
                sr.Close();
            }
        }
        public void Dispose(){
            if (!File.Exists(_fileName))
                return;
            using (var sw = new StreamWriter(_fileName)) {
                foreach (var oneSubscribe in _ar) {
                    sw.WriteLine(oneSubscribe.ToString());
                }
                sw.Close();
            }
        }

        public void Remove() {
            if(File.Exists(_fileName)){
                File.Delete(_fileName);
            }
        }

        public OneSubscribe Add(MailAddress mailAddress, string name) {
            lock (SyncObj) {
                var confirmStr = string.Format("{0:D20}.{1:D5}", DateTime.Now.Ticks, _random.Next(99999));
                var oneSubscribe = new OneSubscribe(mailAddress, name, confirmStr);
                _ar.Add(oneSubscribe);
                return oneSubscribe;
            }
        }
        public bool Del(MailAddress mailAddress) {
            lock (SyncObj) {
                for (var i = 0; i < _ar.Count; i++) {
                    if (!mailAddress.Compare(_ar[i].MailAddress))
                        continue;
                    _ar.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        //見つからないとき、nullを返す
        public OneSubscribe Search(MailAddress mailAddress) {
            lock (SyncObj) {
                for (var i = 0; i < _ar.Count; i++) {
                    if (!mailAddress.Compare(_ar[i].MailAddress))
                        continue;
                    if (_ar[i].Dt.AddMilliseconds(_effectiveMsec) > DateTime.Now) {
                        return _ar[i];//経過時間内のデータなので有効
                    }
                    _ar.RemoveAt(i);//経過時間を超えた情報は削除される
                    return null;
                }
                return null;
            }
        }
    }
}