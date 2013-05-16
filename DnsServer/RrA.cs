using Bjd.net;
using Bjd.packet;

namespace DnsServer{

    public class RrA : OneRr{

        public RrA(string name, uint ttl, Ip ip) 
            : base(name, DnsType.A, ttl, ip.IpV4.ToArray()){
        }

        public RrA(string name, uint ttl, byte[] data) 
            : base(name, DnsType.A, ttl, data){
        }

        public Ip Ip{
            get{
                return new Ip(Conv.GetUInt(Data));
            }
        }

        public override string ToString(){
            return string.Format("{0} {1} TTL={2} {3}", DnsType, Name, Ttl, Ip);
        }
    }
}

