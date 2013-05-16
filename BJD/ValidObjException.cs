using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bjd {
    //ValidObj用のチェック例外
    //初期化文字列が不正なため初期化に失敗している
    public class ValidObjException : Exception {
        public ValidObjException(String msg)
            : base(msg) {
        }
    }
}
