using Bjd.packet;
using Bjd.util;

namespace DnsServer{


    public class RrSoa : OneRr{

        public RrSoa(string name, uint ttl, string n1, string n2, uint serial, uint refresh, uint retry, uint expire, uint minimum) : base(name, DnsType.Soa, ttl, Bytes.Create(DnsUtil.Str2DnsName(n1), DnsUtil.Str2DnsName(n2), Conv.GetBytes(serial), Conv.GetBytes(refresh), Conv.GetBytes(retry), Conv.GetBytes(expire), Conv.GetBytes(minimum))){
        }

        public RrSoa(string name, uint ttl, byte[] data) : base(name, DnsType.Soa, ttl, data){
        }

        public string NameServer{
            get{
                return DnsUtil.DnsName2Str(Data);
            }
        }

        public string PostMaster{
            get{
                return DnsUtil.DnsName2Str(GetData(NameServer.Length + 1));
            }
        }

//        private int GetInt(int offset){
//            int p = NameServer.Length + PostMaster.Length + 2;
//            return Conv.GetInt(Data, p + offset);
//        }
        //[C#]
        private uint GetUInt(int offset) {
            int p = NameServer.Length + PostMaster.Length + 2;
            return Conv.GetUInt(Data, p + offset);
        }

        public uint Serial{
            get{
                return GetUInt(0);
            }
        }

        public uint Refresh{
            get{
                return GetUInt(4);
            }
        }

        public uint Retry{
            get{
                return GetUInt(8);
            }
        }

        public uint Expire{
            get{
                return GetUInt(12);
            }
        }

        public uint Minimum{
            get{
                return GetUInt(16);
            }
        }


        public override string ToString(){
            return string.Format("{0} {1} TTL={2} {3} {4} {5:X8} {6:X8} {7:X8} {8:X8} {9:X8}", DnsType, Name, Ttl, NameServer, PostMaster, Serial, Refresh, Retry, Expire, Minimum);
        }
    }
}
