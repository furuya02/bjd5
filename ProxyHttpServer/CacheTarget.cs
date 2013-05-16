using System.Collections.Generic;
using System.Linq;
using Bjd.option;

namespace ProxyHttpServer {
    //大文字・小文字は区別されない
    internal class CacheTarget {
        readonly List<string> _ar = new List<string>();
        readonly int _enabled;
        public CacheTarget(IEnumerable<OneDat> dat, int enabled) {
            _enabled = enabled;
            foreach (var o in dat) {
                if (o.Enable) { //有効なデータだけを対象にする
                    var str = o.StrList[0];
                    _ar.Add(str.ToUpper());
                }
            }
        }

        //先頭一致
        public bool IsHit(string host) {
            if (host == "")
                return (_enabled != 0);
            if (_ar.Any(s => host.ToUpper().IndexOf(s) == 0)){
                return (_enabled == 0);
            }
            return (_enabled != 0);
        }

        //同一
        public bool IsMatch(string ext) {
            if (ext == "")
                return (_enabled != 0);
            if (_ar.Any(s => ext.ToUpper() == s)){
                return (_enabled == 0);
            }
            return (_enabled != 0);
        }
    }
}
