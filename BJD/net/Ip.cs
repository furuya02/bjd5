using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Linq;
using Bjd.util;

namespace Bjd.net {

    public class Ip : ValidObj{
        
        //プロパティ・変数
        public InetKind InetKind { get; private set; }
        public List<byte> IpV4 { get;private set; }
        public List<byte> IpV6 { get;private set; }
        public bool Any { get; private set; }
        //public bool Status{ get; private set;}
        public int ScopeId { get; private set; }
        
        //デフォルト値の初期化
        void Init(InetKind inetKind) {
            InetKind = inetKind;
            if(inetKind == InetKind.V4)
                IpV4 = new List<byte>{ 0, 0, 0, 0 };
            else
                IpV6 = new List<byte> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Any = false;
        }
        //コンストラクタ(隠蔽)
        private Ip() {}

        //コンストラクタ
        public Ip(string ipStr){
            Init(ipStr);
        }



        private void Init(string ipStr){
            Init(InetKind.V4);//デフォルト値での初期化

            if (ipStr == null){
                ThrowException("null"); //例外終了
                return;
            }

            if (ipStr == "INADDR_ANY") {//IPV4
                Any = true;
            }else if (ipStr == "IN6ADDR_ANY_INIT") {//IPV6
                InetKind = InetKind.V6;
                Any = true;
            }else if (ipStr.IndexOf('.') > 0) {//IPV4
                //名前で指定された場合は、例外に頼らずここで処理する（高速化）
                foreach (var c in ipStr.Where(c => c != '.' && (c < '0' || '9' < c))){
                    ThrowException(ipStr); //例外終了
                }
                var tmp = ipStr.Split('.');
                Init(InetKind.V4);//デフォルト値での初期化
                try {
                    if (tmp.Length == 4) {

                        for (var i = 0; i < 4; i++) {
                            IpV4[i] = Convert.ToByte(tmp[i]);
                        }
                    } else if (tmp.Length == 3) {//ネットアドレスでnewされた場合
                        for (var i = 0; i < 3; i++)
                            IpV4[i] = Convert.ToByte(tmp[i]);
                        IpV4[3] = 0;
                    } else {
                        ThrowException(ipStr); //例外終了
                    }
                } catch {
                    ThrowException(ipStr); //例外終了
                }

                for (int i = 0; i < 4; i++){
                    if (IpV4[i] < 0 || 255 < IpV4[i])
                        ThrowException(ipStr); //例外終了
                }

            } else if(ipStr.IndexOf(':') >= 0) {//IPV6

                Init(InetKind.V6);//デフォルト値での初期化

                //Ver5.1.2 もし、[xxxx:xxxx:xxxx:xxxx:xxxx:xxxx]で囲まれている場合は、除去する
                var tmp = ipStr.Split(new[] { '[',']' });
                if(tmp.Length == 3) {
                    ipStr = tmp[1];
                }
                //Ver5.4.9 %の付いたV6アドレスに対応
                var index = ipStr.IndexOf('%');
                if (index >= 0) {
                    try {
                        ScopeId = Int32.Parse(ipStr.Substring(index + 1));
                    } catch {
                        ScopeId = 0;
                    }
                    ipStr = ipStr.Substring(0, index);
                }
                tmp = ipStr.Split(':');

                var n = ipStr.IndexOf("::");
                if(0 <= n) {
                    var sb = new StringBuilder();
                    sb.Append(ipStr.Substring(0,n));
                    for(var i = tmp.Length;i < 8;i++)
                        sb.Append(":");
                    sb.Append(ipStr.Substring(n));
                    tmp = sb.ToString().Split(':');
                }
                if(tmp.Length != 8)
                    ThrowException(ipStr); //例外終了
                for (var i = 0; i < 8; i++) {
                    if (tmp[i].Length > 4) {
                        ThrowException(ipStr); //例外終了
                    }

                    if (tmp[i] == "") {
                        IpV6[i * 2] = 0;
                        IpV6[i * 2 + 1] = 0;
                    }else{
                        UInt16 u = Convert.ToUInt16(tmp[i], 16);
                        byte[] b = BitConverter.GetBytes(u);
                        IpV6[i * 2] = b[1];
                        IpV6[i * 2 + 1] = b[0];
                    }
                }
            } else {
                ThrowException(ipStr); //例外終了
            }
        }
        //ホストバイトオーダのデータで初期化する
        public Ip(uint ip) {

            Init(InetKind.V4);//デフォルト値での初期化

            byte[] tmp = BitConverter.GetBytes(ip);
            for (int i=0;i<4;i++)
                IpV4[i] = tmp[3-i];
            if(IpV4.Max()==0)
                Any = true;
        }
        //ホストバイトオーダのデータで初期化する
        public Ip(UInt64 h, UInt64 l) {

            Init(InetKind.V6);//デフォルト値での初期化

            byte [] b = BitConverter.GetBytes(h);
            for (int i = 0; i < 8; i++) {
                IpV6[7 - i] = b[i];
            }
            b = BitConverter.GetBytes(l);
            for (int i = 0; i < 8; i++) {
                IpV6[15 - i] = b[i];
            }
        }
        // IpKindによるコンストラクタ
        public Ip(IpKind ipKind) {
            String ipStr = "";
            switch (ipKind) {
                case IpKind.V4_0:
                    ipStr = "0.0.0.0";
                    break;
                case IpKind.V4_255:
                    ipStr = "255.255.255.255";
                    break;
                case IpKind.V6_0:
                    ipStr = "::";
                    break;
                case IpKind.V6_FF:
                    ipStr = "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF";
                    break;
                case IpKind.InAddrAny:
                    ipStr = "INADDR_ANY";
                    break;
                case IpKind.In6AddrAnyInit:
                    ipStr = "IN6ADDR_ANY_INIT";
                    break;
                case IpKind.V4Localhost:
                    ipStr = "127.0.0.1";
                    break;
                case IpKind.V6Localhost:
                    ipStr = "::1";
                    break;
                default:
                    //定義が不足している場合
                    Util.RuntimeException(String.Format("Ip(IpKind) ipKind={0}", ipKind));
                    break;
            }
            try {
                Init(ipStr);
            } catch (ValidObjException) {
                //ここで例外が発生するのは、設計上の問題
                Util.RuntimeException(ipStr);
            }
        }

        
        public override bool Equals(object o) {
            if (((Ip)o).InetKind == InetKind) {
                if (InetKind == InetKind.V4) {
                    if (IpV4 == null && ((Ip)o).IpV4 == null)
                        return true;
                    return IpV4 != null && !IpV4.Where((t, i) => ((Ip) o).IpV4[i] != t).Any();
                }
                if (IpV6 == null && ((Ip)o).IpV6 == null)
                    return true;

                if (ScopeId != ((Ip)o).ScopeId) {
                    return false;
                }

                return IpV6 != null && !IpV6.Where((t, i) => ((Ip) o).IpV6[i] != t).Any();
            }
            return false;
        }
        public override int GetHashCode() {
            return base.GetHashCode();
        }
        
        ////Ver5.2.6 null判定追加
        //// 「==」演算子のオーバーロード
        //public static bool operator ==(Ip a, Ip b) {
        //    try {
        //        return a.Equals(b);
        //    } catch {
        //        return false;
        //    }
        //}
        //Ver5.4.1で修正
        public static bool operator ==(Ip a, Ip b) {
            if (ReferenceEquals(a, b)) {
                return true;
            }
            if (((object)a == null) || ((object)b == null)) {
                return false;
            }
            return a.Equals(b);
        }

        //// 「!=」演算子のオーバーロード
        public static bool operator !=(Ip a, Ip b) {
            return !(a == b);
        }

        //1回だけ省略表記を使用する
        private enum State {
            Unused, //未使用
            Using, //使用中
            Finish, //使用済
        }

        override public string ToString(){
            CheckInitialise();

            if (InetKind == InetKind.V4) {
                if (Any)
                    return "INADDR_ANY";
                return string.Format("{0}.{1}.{2}.{3}",IpV4[0],IpV4[1],IpV4[2],IpV4[3]);
            }
            if (Any)
                return "IN6ADDR_ANY_INIT";
                
            if (IpV6.Max() == 0)
                return "::0";

            var sb = new StringBuilder();
            var state = State.Unused;
            for(var i=0;i<8;i++){
                var h  = Convert.ToUInt16(IpV6[i*2]);
                var l  = Convert.ToUInt16(IpV6[i*2+1]);
                var u = (UInt16)((h<<8) | l);
                if (u == 0) {
                    if (state == State.Unused) { // 未使用の場合
                        state = State.Using; // 使用中に設定する
                        sb.Append(":");
                    } else if (state == State.Finish) { // 使用済の場合、0を表記する
                        sb.AppendFormat(":{0:x}", u);
                    }

                    //if (flg == 0) {//未使用の場合
                    //    flg = 1;//使用中に設定する
                    //    sb.Append(":");
                    //} else if (flg == 1) {//使用中の場合
                    //    //処理なし
                    //} else {//使用済の場合、0を表記する
                    //    sb.AppendFormat(":{0:x}", u);
                    //}
                } else{
                    if (state == State.Using) { // 使用中の場合は
                        state = State.Finish; // 使用済に設定する
                    }
                    sb.Append(i == 0 ? String.Format("{0:x}", u) : String.Format(":{0:x}", u));

                    //if (flg == 1) {//使用中の場合は
                    //    flg = 2;//使用済に設定する
                    //}
                    //sb.AppendFormat(i == 0 ? "{0:x}" : ":{0:x}", u);
                }
            }
            if (state == State.Using) { // 使用中で終了した場合は:を足す
                sb.Append(":");
            }
            //Ver5.4.9
            if (ScopeId != 0) {
                sb.AppendFormat("%{0}", ScopeId);
            }
            return sb.ToString();
        }

        //ネットワークバイトオーダ
        public byte[] NetBytes(){
            CheckInitialise();
            return InetKind == InetKind.V4 ? IpV4.ToArray() : IpV6.ToArray();
        }

        //ホストバイトオーダ
        public uint AddrV4 {
            get{
                CheckInitialise();
                if (InetKind == InetKind.V4) {
                    var tmp = new byte[4];
                    tmp[3] = IpV4[0];
                    tmp[2] = IpV4[1];
                    tmp[1] = IpV4[2];
                    tmp[0] = IpV4[3];
                    return BitConverter.ToUInt32(tmp,0);
                }
                return 0;
            }
        }
        //ホストバイトオーダ
        public UInt64 AddrV6H {
            get{
                CheckInitialise();
                if (InetKind == InetKind.V6) {
                    var tmp = new byte[8];
                    tmp[7] = IpV6[0];
                    tmp[6] = IpV6[1];
                    tmp[5] = IpV6[2];
                    tmp[4] = IpV6[3];
                    tmp[3] = IpV6[4];
                    tmp[2] = IpV6[5];
                    tmp[1] = IpV6[6];
                    tmp[0] = IpV6[7];
                    return BitConverter.ToUInt64(tmp, 0);
                }
                return 0;
            }
        }
        //ホストバイトオーダ
        public UInt64 AddrV6L {
            get{
                CheckInitialise();
                if (InetKind == InetKind.V6) {
                    var tmp = new byte[8];
                    tmp[7] = IpV6[8];
                    tmp[6] = IpV6[9];
                    tmp[5] = IpV6[10];
                    tmp[4] = IpV6[11];
                    tmp[3] = IpV6[12];
                    tmp[2] = IpV6[13];
                    tmp[1] = IpV6[14];
                    tmp[0] = IpV6[15];
                    return BitConverter.ToUInt64(tmp, 0);
                }
                return 0;
            }
        }
        public IPAddress IPAddress {
            get {
                CheckInitialise();
                if (Any)
                    return (InetKind == InetKind.V4) ? IPAddress.Any : IPAddress.IPv6Any;
                var ipaddress = new IPAddress(NetBytes());
                if (ScopeId != 0) {
                    ipaddress.ScopeId = ScopeId;
                }
                return ipaddress;
            }
        }

        //共通初期化
        protected override void Init(){
            Init(InetKind.V4);
        }
    }
}
