using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Bjd;
using Bjd.net;
using Bjd.option;

namespace DhcpServer {

    public class Lease : IDisposable {
        //readonly Logger logger;
        readonly int leaseTime;
        readonly string fileName;

        readonly List<OneLease> ar = new List<OneLease>();

        //public Lease(Logger logger, string fileName, Ip startIp, Ip endIp, int leaseTime, Dat2 macAcl) {
        public Lease(string fileName, Ip startIp, Ip endIp, int leaseTime, Dat macAcl) {
            //this.logger = logger;
            this.fileName = fileName;
            this.leaseTime = leaseTime;
            uint start = startIp.AddrV4;
            uint end = endIp.AddrV4;
            int count = 2048;//最大保持数

            for (uint i = start; i <= end && count > 0; i++) {
                Ip ip = new Ip(i);
                ar.Add(new OneLease(ip));//MAC指定なし
                count--;
            }

            foreach (var o in macAcl) {
                if (o.Enable) {//有効なデータだけを対象にする
                    string macStr = o.StrList[0];//MACアドレス(99-99-99-99-99-99)
                    Mac mac = new Mac(macStr);
                    Ip ip = new Ip(o.StrList[1]);//IPアドレス
                    if (ip.ToString() == "255.255.255.255") {
                        ar.Add(new OneLease(ip, mac));//MAC指定ありで全部追加
                    } else {

                        // 基本設定の範囲のテーブルを検索
                        bool find = false;
                        for (int i = 0; i < ar.Count; i++) {
                            if (ar[i].Ip == ip) {
                                ar[i] = new OneLease(ip, mac);//MAC指定ありに変更
                                find = true;
                                break;
                            }
                        }
                        if (!find) { // 基本設定の範囲外の場合
                            ar.Add(new OneLease(ip, mac));//MAC指定ありとして追加
                        }
                    }
                }
            }
            // リース中データの読み込み
            Read();
        }

        public void Dispose() {
            for (int i = 0; i < ar.Count; i++) {
                ar[i].Refresh();
            }
            Save();// リース中のデータを保存
        }
        //MAC指定のみの場合、データベースに存在するかどうかを確認する
        public bool SearchMac(Mac mac) {
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].MacAppointment && ar[i].Mac == mac)
                    return true;
            }
            return false;
        }

        //RELEASE処理
        public Ip Release(Mac mac) {
            // 当該データベースの検索
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].Mac == mac) {
                    ar[i].SetUnuse();
                    Save();// リース中のデータを保存
                    return ar[i].Ip;
                }
            }
            return null;
        }

        //DISCOVER処理
        public Ip Discover(Ip requestIp, uint id, Mac mac) {
            int i = SearchDiscover(requestIp, id, mac);
            if (i != -1) {
                ar[i].SetReserve(id, mac);
                // リクエストされたIP以外が検索された場合もある
                return ar[i].Ip;
            }
            return null;
        }

        //REQUEST処理
        public Ip Request(Ip requestIp, uint id, Mac mac) {

            int i = SearchRequest(requestIp, id);

            if (i != -1) {

                //同一MACですでに使用中のものがあれば破棄する
                for (int n = 0; n < ar.Count; n++) {
                    if (n == i)
                        continue;
                    if (ar[n].Mac == mac)
                        ar[n].SetUnuse();
                }


                ar[i].SetUsed(id, mac, DateTime.Now.AddSeconds(leaseTime));
                Save();// リース中のデータを保存

                // リクエストされたIP以外が検索された場合もある
                return ar[i].Ip;
            }
            return null;

        }

        public int SearchRequest(Ip requestIp, uint id) {


            for (int i = 0; i < ar.Count; i++) {

                if (ar[i].DbStatus == DhcpDbStatus.Reserve && ar[i].Id == id)
                    return i;
                if (ar[i].DbStatus == DhcpDbStatus.Used && ar[i].Ip == requestIp) {
                    return i;
                }
            }
            return -1;
        }

        int SearchDiscover(Ip ip, uint id, Mac mac) {

            //すでにDISCOVERを受けてリザーブ状態のデータがある場合は、同じ答えを返す
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].DbStatus == DhcpDbStatus.Reserve && ar[i].Id == id) {
                    return i;
                }
            }

            //MAC指定のデータを優先して検索する
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].MacAppointment && ar[i].Mac == mac) {
                    if (ar[i].Ip.ToString() == "255.255.255.255") {
                        goto next;//            
                    }
                    return i;
                }
            }

            // 同一MACのデータがあれば、既存のデータを破棄してリース対象とする
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].Mac == mac) {
                    ar[i].SetUnuse();// 依存データをクリア
                    return i;
                }
            }
            //要求ＩＰがあいている場合は、リース対象にする
            for (int i = 0; i < ar.Count; i++) {
                if (!ar[i].MacAppointment && ar[i].DbStatus == DhcpDbStatus.Unused && ar[i].Ip == ip) {
                    return i;
                }
            }
        next:
            //IPはなんでもいいので空いているものを対象にする
            for (int i = 0; i < ar.Count; i++) {
                ar[i].Refresh();//時間超過しているデータは初期化する
                if (!ar[i].MacAppointment && ar[i].DbStatus == DhcpDbStatus.Unused) {
                    return i;
                }
            }
            return -1;
        }
        // リース中のデータの保存
        void Save() {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.ASCII)) {
                for (int i = 0; i < ar.Count; i++) {
                    if (ar[i].DbStatus == DhcpDbStatus.Used) {
                        string str = string.Format("{0}\t{1}\t{2}\t{3}",
                            ar[i].Ip.ToString(),
                            ar[i].Dt.Ticks,
                            ar[i].Id,
                            ar[i].Mac.ToString());
                        sw.WriteLine(str);
                    }
                }
                sw.Flush();
                sw.Close();
            }
        }
        // リース中のデータの読み込み
        void Read() {
            if (!File.Exists(fileName))
                return;
            using (StreamReader sr = new StreamReader(fileName, Encoding.ASCII)) {
                while (true) {
                    string str = sr.ReadLine();
                    if (str == null)
                        break;
                    string[] tmp = str.Split('\t');
                    if (tmp.Length == 4) {
                        try {
                            Ip ip = new Ip(tmp[0]);
                            DateTime dt = new DateTime(Convert.ToInt64(tmp[1]));
                            uint id = Convert.ToUInt32(tmp[2]);
                            Mac mac = new Mac(tmp[3]);
                            for (int i = 0; i < ar.Count; i++) {
                                if (ar[i].Ip == ip) {
                                    if (ar[i].MacAppointment && ar[i].Mac != mac) {
                                        break;
                                    }
                                    ar[i].SetUsed(id, mac, dt);
                                    break;
                                }
                            }
                        } catch {
                            
                        }

                    }
                }
                sr.Close();
            }

        }
        public string GetInfo() {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ar.Count; i++) {
                ar[i].Refresh();//時間超過しているデータは初期化する
                sb.Append(ar[i].ToString() + "\b");
            }
            return sb.ToString();
        }
    }
}
