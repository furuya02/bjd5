using System.Net;
using System.Text.RegularExpressions;
using Bjd.net;

namespace Bjd.acl {
    internal class FqdnAcl : Acl{
        private Regex _fqdn;

        public FqdnAcl(string name, string fqdnStr) : base(name){
            var s = fqdnStr.Replace(".", "\\.");
            s = s.Replace("*", ".*");
            _fqdn = new Regex(s);

        }

        protected override void Init(){
        }

        public override bool IsHit(Ip ip){
            throw new System.NotImplementedException();
        }

        public bool IsHit(Ip ip,string hostName){
            return _fqdn.IsMatch(hostName);
        }

    }

}
