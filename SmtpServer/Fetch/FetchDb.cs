using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmtpServer {
    class FetchDb{

        readonly List<OneFetchDb> _ar = new List<OneFetchDb>();

        public String FileName { get; private set; }

        public FetchDb(string dir,string name) {
            
            //ファイル名に使用できない文字をアンスコに変更
            foreach (var c in Path.GetInvalidFileNameChars()){
                name = name.Replace(c, '_');
            }
            FileName = string.Format("{0}\\fetch.{1}.db", dir,name);

            Read();
        }
        void Read(){
            if (File.Exists(FileName)) {
                using (var sr = new StreamReader(FileName, Encoding.ASCII)) {
                    try {
                        while (true) {
                            string str = sr.ReadLine();
                            if (str == null)
                                break;
                            _ar.Add(new OneFetchDb(str));
                        }
                    } catch (Exception) {
                    }
                    sr.Close();
                }
            }
        }

        public void Save() {
            using (var sw = new StreamWriter(FileName, false, Encoding.ASCII)) {
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
        public bool IsPast(string uid, int sec) {
            var index = IndexOf(uid);
            if (index != -1) {
                var d = _ar[index].Dt.AddSeconds(sec);
                if (d < DateTime.Now)
                    return true;
            }
            return false;
        }
     
        private class OneFetchDb{
            public string Uid { get; private set; }
            public DateTime Dt { get; private set; } //取得時刻
            
            public OneFetchDb(String uid, DateTime dt){
                Uid = uid;
                Dt = dt;
            }

            public OneFetchDb(string str){
                var tmp = str.Split('\t');
                if (tmp.Length == 2){
                    Uid = tmp[0];
                    Dt = new DateTime(Convert.ToInt64(tmp[1]));
                }
            }

            public override string ToString(){
                return string.Format("{0}\t{1}", Uid, Dt.Ticks);
            }
        }

    }
}
