namespace DnsServer{
    public class RrPtr : OneRr{

        public RrPtr(string name, uint ttl, string ptr) : 
            base(name, DnsType.Ptr, ttl, DnsUtil.Str2DnsName(ptr)){
        }

        public RrPtr(string name, uint ttl, byte[] data) : 
            base(name, DnsType.Ptr, ttl, data){

        }

        public string Ptr{
            get{
                return DnsUtil.DnsName2Str(Data);
            }
        }


        public override string ToString(){
            return string.Format("{0} {1} TTL={2} {3}", DnsType, Name, Ttl, Ptr);
        }
    }
}
