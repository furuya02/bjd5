using System.Collections.Generic;
using System.Linq;
using Bjd.option;

namespace ProxyHttpServer {
    //********************************************************
    //Datオブジェクトの各プロパティをObject形式ではない本来の型で強制するため
    //カバーリングクラスを作成する
    //********************************************************
    //コンテンツ制限
    internal class LimitString {
        readonly List<string> _ar = new List<string>();
        public LimitString(IEnumerable<OneDat> dat) {
            foreach (var o in dat) {
                if (o.Enable) { //有効なデータだけを対象にする
                    _ar.Add(o.StrList[0]);
                }
            }
        }

        //戻り値は、ヒットした文字列
        //ヒットしなかった場合はnullが返される
        public string IsHit(string str){
            return _ar.FirstOrDefault(s => str.IndexOf(s) != -1);
        }

        public int Length {
            get {
                return _ar.Count;
            }
        }
    }
}