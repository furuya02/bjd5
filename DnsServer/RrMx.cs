using Bjd.packet;
using Bjd.util;

namespace DnsServer{


    public class RrMx : OneRr{

        public RrMx(string name, uint ttl, ushort preference, string mailExchangerHost) : base(name, DnsType.Mx, ttl, Bytes.Create(Conv.GetBytes(preference), DnsUtil.Str2DnsName(mailExchangerHost))){

        }

        public RrMx(string name, uint ttl, byte[] data) : base(name, DnsType.Mx, ttl, data){
        }

        public ushort Preference { get { return Conv.GetUShort(GetData(0, 2)); } }

        public string MailExchangeHost { get { return DnsUtil.DnsName2Str(GetData(2)); } }


        public override string ToString(){
            return string.Format("{0} {1} TTL={2} {3} {4}", DnsType, Name, Ttl, Preference, MailExchangeHost);
        }
    }
}