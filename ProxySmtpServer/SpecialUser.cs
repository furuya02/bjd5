using System;
using System.Collections.Generic;
using System.Linq;
using Bjd.option;

namespace ProxySmtpServer {
    class SpecialUser {
        readonly List<OneSpecialUser> _ar = new List<OneSpecialUser>();
        public SpecialUser(IEnumerable<OneDat> dat) {
            foreach (var o in dat) {
                if (o.Enable) {
                    var before = o.StrList[0];
                    var server = o.StrList[1];
                    var port = Convert.ToInt32(o.StrList[2]);
                    var after = o.StrList[3];
                    _ar.Add(new OneSpecialUser(before, server, port, after));
                }
            }
        }

        public OneSpecialUser Search(string before){
            return _ar.FirstOrDefault(oneSpecialUser => oneSpecialUser.Before == before);
        }
    }
}
