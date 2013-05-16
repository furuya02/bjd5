using System;
using System.Collections.Generic;
using Bjd.option;

namespace Bjd.log {
    //表示制限文字列のリストを保持し、表示対象かどうかをチェックするクラス
    public class LogLimit{

        private readonly String[] _limitStr;
        private readonly bool _isDisplay; //ヒットした場合に表示するかどうか

        //dat 制限文字列
        //isDisplay　ヒットした場合の動作（表示/非表示)
        public LogLimit(Dat dat, bool isDisplay){
            _isDisplay = isDisplay;

            var tmp = new List<string>();
            if (dat != null){
                foreach (var o in dat){
                    if (o.Enable){
                        //有効なデータだけを対象にする
                        tmp.Add(o.StrList[0]);
                    }
                }
            }
            _limitStr = tmp.ToArray();
        }

        /**
         * 指定した文字列を表示するか否かの判断
         * @param str 検査する文字列
         * @return　表示：true　　非表示:false
         */

        public bool IsDisplay(String str){
            if (str == null){
                return false;
            }
            foreach (var s in _limitStr){
                if (str.IndexOf(s) != -1){
                    return (_isDisplay);
                }
            }
            return (!_isDisplay);
        }
    }

}
