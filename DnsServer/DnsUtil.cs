using System;
using System.Text;
using Bjd.util;

namespace DnsServer{

    // 文字列とDNS形式の名前(.の所に文字数が入る）の変換
    public class DnsUtil{

        //デフォルトコンストラクタの隠蔽
        private DnsUtil(){

        }

        public static string DnsName2Str(byte[] data){
            var tmp = new byte[data.Length - 1];
            for (int src = 0, dst = 0; src < data.Length - 1;){
                var c = data[src++];
                if (c == 0){
                    var buf = new byte[dst];
                    Buffer.BlockCopy(tmp, 0, buf, 0, dst);
                    //byte[] buf = Arrays.copyOfRange(tmp, 0, dst);

                    tmp = buf;
                    break;
                }
                for (var i = 0; i < c; i++){
                    tmp[dst++] = data[src++];
                }
                tmp[dst++] = (byte) '.';
            }
            return Encoding.ASCII.GetString(tmp);
        }

	    //文字列とDNS形式の名前(.の所に文字数が入る）の変換
        public static byte[] Str2DnsName(string name){
            if (name == ""){
                return new byte[]{0};
            }

            if (name[name.Length - 1] == '.'){
                name = name.Substring(0, name.Length - 1);
            }
            var tmp = name.Split('.');
            //最初の文字カウントと最後の'\0'分を追加（途中の.は文字カウント分として使用される）
            //www.nifty.com  -> 03www05nifty03com00
            var data = new byte[name.Length + 2];
            var d = 0;
            foreach (var t in tmp){
                data[d++] = (byte) t.Length;
                var dd = Encoding.ASCII.GetBytes(t);
                for (var e = 0; e < t.Length; e++){
                    data[d++] = dd[e];
                }
            }
            return data;
        }

        public static DnsType Short2DnsType(short d){
            switch (d){
                case 0x0001:
                    return DnsType.A;
                case 0x0002:
                    return DnsType.Ns;
                case 0x0005:
                    return DnsType.Cname;
                case 0x0006:
                    return DnsType.Soa;
                    //case 0x0007:
                    //    return DnsType.Mb;
                    //case 0x0008:
                    //    return DnsType.Mg;
                    //case 0x0009:
                    //    return DnsType.Mr;
                    //case 0x000a:
                    //    return DnsType.Null;
                    //case 0x000b:
                    //    return DnsType.Wks;
                case 0x000c:
                    return DnsType.Ptr;
                    //case 0x000d:
                    //    return DnsType.Hinfo;
                    //case 0x000e:
                    //    return DnsType.Minfo;
                case 0x000f:
                    return DnsType.Mx;
                    //case 0x0010:
                    //    return DnsType.Txt;
                case 0x001c:
                    return DnsType.Aaaa;
                //Java fix Ver5.8.4
                //default:
                //    Util.RuntimeException("short2DnsType() unknown data");
                //    break;
            }
            return DnsType.Unknown;
        }

        public static short DnsType2Short(DnsType dnsType){
            switch (dnsType){
                case DnsType.A:
                    return 0x0001;
                case DnsType.Ns:
                    return 0x0002;
                case DnsType.Cname:
                    return 0x0005;
                case DnsType.Soa:
                    return 0x0006;
                    //case DNS_TYPE.MB:
                    //    return 0x0007;
                    //case DNS_TYPE.MG:
                    //    return 0x0008;
                    //case DNS_TYPE.MR:
                    //    return 0x0009;
                    //case DNS_TYPE.NULL:
                    //    return 0x000a;
                    //case DNS_TYPE.WKS:
                    //    return 0x000b;
                case DnsType.Ptr:
                    return 0x000c;
                    //case DNS_TYPE.HINFO:
                    //    return 0x000d;
                    //case DNS_TYPE.MINFO:
                    //    return 0x000e;
                case DnsType.Mx:
                    return 0x000f;
                    //case DNS_TYPE.TXT:
                    //    return 0x0010;
                case DnsType.Aaaa:
                    return 0x001c;
                //Ver5.8.4 Java fix
//                default:
//                    Util.RuntimeException("dnsType2Short() unknown data");
//                    break;
            }
            return 0x0000;
        }

        public static OneRr CreateRr(string name, DnsType dnsType, uint ttl, byte[] data){
            switch (dnsType){
                case DnsType.A:
                    return new RrA(name, ttl, data);
                case DnsType.Aaaa:
                    return new RrAaaa(name, ttl, data);
                case DnsType.Ns:
                    return new RrNs(name, ttl, data);
                case DnsType.Mx:
                    return new RrMx(name, ttl, data);
                case DnsType.Soa:
                    return new RrSoa(name, ttl, data);
                case DnsType.Ptr:
                    return new RrPtr(name, ttl, data);
                case DnsType.Cname:
                    return new RrCname(name, ttl, data);
                default:
                    Util.RuntimeException(string.Format("DnsUtil.creaetRr() not implement. DnsType={0}", dnsType));
                    break;
            }
            return null; //これが返されることはない
        }

    }
}