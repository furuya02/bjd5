using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SmtpServer {
    class FetchDb {
        readonly string _fileName;
        readonly List<OneFetchDb> _ar = new List<OneFetchDb>();

        public FetchDb(string dir,string hostName,string userName) {
            
            //Ver5.7.1 ユーザ名に\が含まれているとき例外が発生する問題に対処
//            if(userName.IndexOf('\\')!=0){
//                userName = userName.Replace('\\', '_');
//            }

            //Ver5.8.9
            //ファイル名に使用できない文字を取得
            foreach (var c in Path.GetInvalidFileNameChars()){
                if (userName.IndexOf(c) != 0){
                    userName = userName.Replace(c, '_');
                }
            }

            _fileName = string.Format("{0}\\fetch.{1}.{2}.db", dir,hostName,userName);

            
            if (File.Exists(_fileName)) {
                using (var sr = new StreamReader(_fileName, Encoding.ASCII)) {
                    try {
                        while (true) {
                            string str = sr.ReadLine();
                            if (str == null)
                                break;
                            _ar.Add(new OneFetchDb(str));
                        }
                    } catch (Exception){
                    }
                    sr.Close();
                }
            }
        }

        public void Save() {
            using (var sw = new StreamWriter(_fileName, false, Encoding.ASCII)) {
                foreach (OneFetchDb oneFetchDb in _ar) {
                    sw.WriteLine(oneFetchDb.ToString());
                }
                sw.Flush();
                sw.Close();
            }
        }

        //データベースの検索
        public int IndexOf(string uid) {
            for (var i = 0; i < _ar.Count; i++) {
                if (_ar[i].Uid == uid)
                    return i;//既に受信完了している
            }
            return -1;
        }

        //データベースへの追加
        public bool Add(string uid) {
            if (IndexOf(uid) != -1)
                return false;
            _ar.Add(new OneFetchDb(uid, DateTime.Now));
            return true;
        }

        //データベースの削除
        public bool Del(string uid) {
            var index = IndexOf(uid);
            if (index == -1)
                return false;
            _ar.RemoveAt(index);
            return true;
        }

        //サーバに残す時間を過ぎたかどうかの判断
        public bool IsPast(string uid, int keepTime) {
            var index = IndexOf(uid);
            if (index != -1) {
                DateTime d = _ar[index].Dt.AddMinutes(keepTime);
                if (d < DateTime.Now)
                    return true;
            }
            return false;
        }

        class OneFetchDb {
            public OneFetchDb(string uid, DateTime dt) {
                Uid = uid;
                Dt = dt;
            }

            public OneFetchDb(string str) {
                var tmp = str.Split('\t');
                if (tmp.Length == 2) {
                    Uid = tmp[0];
                    Dt = new DateTime(Convert.ToInt64(tmp[1]));
                }
            }

            public string Uid { get; private set; }
            public DateTime Dt { get; private set; }//取得時刻

            public override string ToString() {
                return string.Format("{0}\t{1}", Uid, Dt.Ticks);
            }
        }
    }
}