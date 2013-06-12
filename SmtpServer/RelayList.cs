using System.Collections.Generic;
using System.Linq;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.acl;
using Bjd.option;

namespace SmtpServer {
    //アドレスリスト allowList denyList
    internal class RelayList {

        readonly List<Acl> _arV4 = new List<Acl>();
        readonly List<Acl> _arV6 = new List<Acl>();

        //リストが無い場合は、allowList及びdenyListはnullでもよい
        //テスト用にlogger=nullも可
        public RelayList(IEnumerable<OneDat> dat,string name,Logger logger) {
            if (dat == null){ //リストなし
                return;
            }
            foreach (var o in dat) {
                if (!o.Enable){
                    continue;
                }
                var ipStr= o.StrList[0];

                if (ipStr.IndexOf('.') != -1) {//IPv4ルール
                    var acl = new AclV4(name,ipStr);
                    if (!acl.Status) {
                        if (logger != null){
                            logger.Set(LogKind.Error, null, 25, string.Format("{0} : {1}", name, ipStr));
                        }
                    } else {
                        _arV4.Add(acl);
                    }
                } else {//IPv6ルール
                    var acl = new AclV6(name,ipStr);
                    if (!acl.Status) {
                        if (logger != null){
                            logger.Set(LogKind.Error, null, 25, string.Format("{0} : {1}", name, ipStr));
                        }
                    } else {
                        _arV6.Add(acl);
                    }
                }
            }
        }

        public bool IsHit(Ip ip){
            return ip.InetKind == InetKind.V4 ? _arV4.Any(p => p.IsHit(ip)) : _arV6.Any(p => p.IsHit(ip));
        }
    }

}
