using System;
using System.Collections.Generic;
using System.Linq;
using Bjd.net;

namespace Bjd {
    public class AttackDb {
        readonly int _sec;//分母となる秒数
        readonly int _max;//上記の期間に、この回数を超えたら攻撃と判定する
        List<OneAttack> _ar = new List<OneAttack>();
        public AttackDb(int sec, int max) {
            _sec = sec;
            _max = max;
        }

        //bool success 認証の成否
        //IP ip 接続元IPアドレス
        //ブルートフォースと判断された場合 return true
        public bool IsInjustice(bool success, Ip ip) {
            if (success) {//認証成功
                lock (this) {
                    //同一IPの情報をすべて削除する
                    _ar = _ar.FindAll(n => !(n.Ip.Equals(ip)));
                }
            } else {//認証失敗
                var now = DateTime.Now;
                lock (this) {
    
                    _ar.Add(new OneAttack(now, ip));
                    //１分以上経過した情報を削除する
                    
                    _ar = _ar.FindAll(n => n.Dt.AddSeconds(_sec) > now);
                    var m = _ar.Count(n => n.Ip.Equals(ip));
                    if (_max <= m)
                        return true;//不正アクセス
                }
                return false;
            }
            return false;
        }

        class OneAttack {
            public DateTime Dt { get; private set; }
            public Ip Ip { get; private set; }
            public OneAttack(DateTime dt, Ip ip) {
                Ip = ip;
                Dt = dt;
            }
        }
    }
}
