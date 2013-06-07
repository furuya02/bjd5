using System;
using System.IO;
using System.Text;
using Bjd.net;

namespace Bjd.mail {
    //**********************************************************************************
    //メール情報を表現（保持）するクラス
    //**********************************************************************************
    public class MailInfo {
        DateTime _dt;//最終処理時刻

        public MailInfo(string uid, long size, string host, Ip addr, string date, MailAddress from, MailAddress to) {
            Clear();//初期値

            Uid = uid;
            Size = size;
            Host = host;
            Addr = addr;
            Date = date;
            _dt = new DateTime(0);//最初は0で初期化して、とりあえずキューの処理対象になるようにする
            From = from;
            To = to;
        }

        public MailInfo(string fileName) {
            Clear();//初期値
            if (!File.Exists(fileName))
                return;
            try {
                using (var sr = new StreamReader(fileName, Encoding.GetEncoding("ascii"))) {
                    Uid = sr.ReadLine();
                    Size = Convert.ToInt64(sr.ReadLine());
                    Host = sr.ReadLine();
                    Addr = new Ip(sr.ReadLine());
                    Date = sr.ReadLine();
                    RetryCounter = Convert.ToInt32(sr.ReadLine());
                    var ticks = Convert.ToInt64(sr.ReadLine());
                    _dt = new DateTime(ticks);
                    From = new MailAddress(sr.ReadLine());
                    To = new MailAddress(sr.ReadLine());

                    sr.Close();
                }

                int index = fileName.LastIndexOf("DF_");
                if (index != -1)
                    FileName = fileName.Substring(index + 3);
            } catch {
                Clear();//初期値
            }
        }

        //****************************************************************
        //プロパティ
        //****************************************************************
        public string Date { get; private set; }//メール受信日付
        public string Host { get; private set; }
        public Ip Addr { get; private set; }
        public string Uid { get; private set; }
        public string FileName { get; private set; } //DF_ MF_以降のファイル名
        public long Size { get; private set; }
        public MailAddress From { get; private set; }
        public MailAddress To { get; private set; }
        public int RetryCounter { get; private set; }

        //初期値セット
        void Clear() {
            Uid = "";
            Size = 0;
            Host = "";
            Addr = new Ip(IpKind.V4_0);
            Date = "";
            RetryCounter = 0;
            _dt = new DateTime(0);//最初は0で初期化して、とりあえずキューの処理対象になるようにする
            From = new MailAddress("");
            To = new MailAddress("");
            FileName = "";
        }

        //処理対象かどうかの確認
        //最終処理時刻から必要な経過時間が過ぎているかどうかを確認し、処理対象である場合は、カウンタのインクリメントと処理時刻の更新を行う
        public bool IsProcess(double sec, string fileName) {
            if (sec != 0){
                //最小処理時間を経過しないメールは、対象外にする
                var span = DateTime.Now - _dt;
                if (sec > span.TotalSeconds){
                    return false;
                }
            }
            _dt = DateTime.Now;//現在の処理時間を記録する
            RetryCounter++;
            Save(fileName);
            return true;
        }

        public bool Save(string fileName) {
            using (var sw = new StreamWriter(fileName, false, Encoding.GetEncoding("ascii"))) {
                sw.WriteLine(Uid);
                sw.WriteLine(Size.ToString());
                sw.WriteLine(Host);
                sw.WriteLine(Addr.ToString());
                sw.WriteLine(Date);
                sw.WriteLine(RetryCounter.ToString());
                sw.WriteLine(string.Format("{0}", _dt.Ticks));
                sw.WriteLine(From.ToString());
                sw.WriteLine(To.ToString());

                sw.Flush();
                sw.Close();
            }

            var index = fileName.LastIndexOf("DF_");
            if (index != -1){
                FileName = fileName.Substring(index + 3);
            }

            return true;
        }

        public override string ToString() {
            return string.Format("from:{0} to:{1} size:{2} uid:{3}", From, To, Size, Uid);
        }
    }
}
