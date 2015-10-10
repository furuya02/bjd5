namespace DnsServer{
    public class RrNs : OneRr{

        public RrNs(string name, uint ttl, string nsName) : 
            base(name, DnsType.Ns, ttl, DnsUtil.Str2DnsName(nsName)){
        }


        public RrNs(string name, uint ttl, byte[] data) : 
            base(name, DnsType.Ns, ttl, data){
        }

        public string NsName{
            get{
                return DnsUtil.DnsName2Str(Data);
            }
        }


        public override string ToString(){
            return string.Format("{0} {1} TTL={2} {3}", DnsType, Name, Ttl, NsName);
        }
    }
}
