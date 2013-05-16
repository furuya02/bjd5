namespace DnsServer{


    public class RrCname : OneRr{

        public RrCname(string name, uint ttl, string cname) : base(name, DnsType.Cname, ttl, DnsUtil.Str2DnsName(cname)){
        }

        public RrCname(string name, uint ttl, byte[] data) : base(name, DnsType.Cname, ttl, data){
        }

        public string CName { get { return DnsUtil.DnsName2Str(Data); } }

        public override string ToString(){
            return string.Format("{0} {1} TTL={2} {3}", DnsType, Name, Ttl, CName);
        }
    }
}
