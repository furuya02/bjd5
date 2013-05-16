using System.Collections.Generic;
using Bjd.option;

namespace SmtpServer {
    //文字列リスト
    internal class StrList {
        readonly List<string> _tagList = new List<string>();
        readonly List<string> _strList = new List<string>();
        public StrList(IEnumerable<OneDat> dat) {
            foreach (var o in dat) {
                if (!o.Enable)
                    continue;
                _tagList.Add(o.StrList[0].Trim(' ', '　'));
                _strList.Add(o.StrList[1].Trim(' ', '　'));
            }
        }

        public int Max {
            get {
                return _tagList.Count;
            }
        }
        public string Tag(int n) {
            return _tagList[n];
        }

        public string Str(int n) {
            return _strList[n];
        }
    }
}
