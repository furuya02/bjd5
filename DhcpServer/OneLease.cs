using System;
using Bjd;
using Bjd.net;

namespace DhcpServer {
    public class OneLease {
        public OneLease(Ip ip) {
            MacAppointment = false;//MAC指定なし
            Init();

            Ip = ip;
        }

        public OneLease(Ip ip, Mac mac) {
            MacAppointment = true;//MAC指定あり
            Init();

            Ip = ip;
            Mac = mac;
        }

        //****************************************************************
        //プロパティ
        //****************************************************************
        public Ip Ip { get; private set; }
        public bool MacAppointment { get; private set; }//MAC指定
        public DateTime Dt { get; private set; }//有効時刻 
        public uint Id { get; private set; }
        public Mac Mac { get; private set; }//MACアドレス （macAppointment==trueの時変更不可）
        public DhcpDbStatus DbStatus { get; private set; }

        void Init() {
            DbStatus = DhcpDbStatus.Unused;
            Id = 0;
            Dt = new DateTime(0);
            if (!MacAppointment)//MAC指定無しの場合、MACも初期化する
                Mac = new Mac("ff-ff-ff-ff-ff-ff");
        }

        //UNUSEDの設定
        public void SetUnuse() {
            Init();
        }

        //USEDの設定
        public void SetUsed(uint id, Mac mac, DateTime dt) {
            DbStatus = DhcpDbStatus.Used;
            Id = id;
            Mac = mac;
            Dt = dt;
        }

        //RESERVEの設定
        public void SetReserve(uint id, Mac mac) {
            DbStatus = DhcpDbStatus.Reserve;
            Id = id;
            Dt = DateTime.Now.AddSeconds(5);//５秒間有効
            Mac = mac;
        }

        //有効時刻を過したものはクリアする
        public void Refresh() {
            if (DbStatus != DhcpDbStatus.Unused) {
                if (Dt.Ticks < DateTime.Now.Ticks) {
                    Init();
                }
            }
        }

        //ToString()を戻すためのコンストラクタ
        public OneLease(string str) {
            string[] tmp = str.Split('\t');
            if (tmp.Length != 5) {
                Init();
                return;
            }
            Ip = new Ip(tmp[0]);
            DbStatus = (DhcpDbStatus)(Convert.ToInt32(tmp[1]));
            long ticks = (Convert.ToInt64(tmp[2]));
            Dt = new DateTime(ticks);
            Mac = new Mac(tmp[3]);
            MacAppointment = Convert.ToBoolean(tmp[4]);
        }

        public override string ToString() {
            return string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                Ip//IPアドレス（変更不可）
                                 ,
                (int)DbStatus//状態
                                 ,
                Dt.Ticks.ToString()//有効時刻
                                 ,
                //Ver5.8.4 Java fix Mac//MACアドレス （macAppointment==trueの時変更不可）
                Mac.ToString()//MACアドレス （macAppointment==trueの時変更不可）
                                 ,
                MacAppointment.ToString());
        }
    }
}
