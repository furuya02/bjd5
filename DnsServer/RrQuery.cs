namespace DnsServer{
    public class RrQuery : OneRr{

        public RrQuery(string name, DnsType dnsType) : base(name, dnsType, 0, new byte[0]){
        }


        public override string ToString(){
            return string.Format("Query {0} {1}", DnsType, Name);
        }
    }
}

