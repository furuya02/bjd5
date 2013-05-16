using System;
using System.Linq;
using Bjd.util;

namespace DnsServer{

    //１つのリソースレコードを表現するクラス
    //ONeRRの内部データは、ネットワークバイトオーダで保持されている
    //【タイプは、A NS CNAME MX PTR SOAの6種類のみに限定する】
    public abstract class OneRr{

        public DnsType DnsType { get; private set; }
        private readonly long _createTime; //データが作成された日時(秒単位)
        public uint Ttl { get; private set; }//内部のネットワークバイトオーダのまま取得される
        public String Name { get; private set; }
        public byte[] Data { get; private set; }

        protected OneRr(string name, DnsType dnsType, uint ttl, byte[] d){
            _createTime = DateTime.Now.Ticks/10000000; //秒単位
            Name = name;
            DnsType = dnsType;
            Ttl = ttl;
            Data = new byte[d.Length];
            Buffer.BlockCopy(d, 0, Data, 0, d.Length);
        }

        //TTL値だけを変更したクローンを生成する
        public OneRr Clone(uint t){
            switch (DnsType){
                case DnsType.A:
                    return new RrA(Name, t, Data);
                case DnsType.Aaaa:
                    return new RrAaaa(Name, t, Data);
                case DnsType.Ns:
                    return new RrNs(Name, t, Data);
                case DnsType.Mx:
                    return new RrMx(Name, t, Data);
                case DnsType.Cname:
                    return new RrCname(Name, t, Data);
                case DnsType.Ptr:
                    return new RrPtr(Name, t, Data);
                case DnsType.Soa:
                    return new RrSoa(Name, t, Data);
                default:
                    Util.RuntimeException(string.Format("OneRr.close() not implement DnsType={0}", DnsType));
                    break;
            }
            return null; //これは実行されない
        }


        public byte[] GetData(int offset, int len){
            var dst = new byte[len];
            Buffer.BlockCopy(Data, offset, dst, 0, len);
            return dst;
        }

        public byte[] GetData(int offset){
            var len = Data.Length - offset;
            return GetData(offset, len);
        }


        //equalsの実装は、テストで使用される
        //内部変数の値が、全て等しいときにtrueとなる
        //TTLの値も検証するので、プロダクトコードには使用する場所が無い
        public new bool Equals(Object o){
            if (o == null){
                return false;
            }
            var r = (OneRr) o;
            if (Name != r.Name){
                return false;
            }
            if (DnsType != r.DnsType){
                return false;
            }
            if (Ttl != r.Ttl){
                return false;
            }
            var tmp = r.Data;
            if (Data.Length != tmp.Length){
                return false;
            }
            return !Data.Where((t, i) => t != tmp[i]).Any();
        }


        public int HashCode(){
            //assert false : "Use is not assumed.";
            return 101;
        }

        //データの有効・無効判断
        // [C#] nowは秒単位で指定する
        public bool IsEffective(long now) {
            if (Ttl == 0){
                return true;
            }
            if (_createTime + Ttl >= now){
                return true;
            }
            return false;
        }
    }
}