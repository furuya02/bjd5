using Bjd.net;
using Bjd.packet;

namespace DnsServer{


    public class RrAaaa : OneRr{

        public RrAaaa(string name, uint ttl, Ip ip) : base(name, DnsType.Aaaa, ttl, ip.IpV6.ToArray()){
        }

        public RrAaaa(string name, uint ttl, byte[] data) : base(name, DnsType.Aaaa, ttl, data){
        }

        public Ip Ip{
            get{
                var v6H = Conv.GetULong(Data, 0);
                var v6L = Conv.GetULong(Data, 8);
                return new Ip(v6H, v6L);
            }
        }

        public override string ToString(){
            return string.Format("{0} {1} TTL={2} {3}", DnsType, Name, Ttl, Ip);
        }
    }
}
