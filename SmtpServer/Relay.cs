using System.Collections.Generic;
using Bjd.log;
using Bjd.net;
using Bjd.option;

namespace SmtpServer {
    class Relay {
        readonly RelayList _allowList;
        readonly RelayList _denyList;
        private readonly int _order; //order 0:許可リスト優勢 1:禁止リスト優先

        //リストが無い場合は、allowList及びdenyListはnullでもよい
        //テスト用にlogger=nullも可
        public Relay(IEnumerable<OneDat> allowList,IEnumerable<OneDat> denyList,int order,Logger logger){
            _allowList = new RelayList(allowList, "Allow List", logger);
            _denyList = new RelayList(denyList, "Denyt List", logger);
            _order = order;
        }
        //Allow及びDenyリストで中継（リレー）が許可されているかどうかのチェック
        public bool IsAllow(Ip ip) {
            if (_order == 0) {//許可リスト優先の場合
                if (_allowList.IsHit(ip))
                    return true;
                if (_denyList.IsHit(ip))
                    return false;
            } else { //禁止リスト優先
                if (_denyList.IsHit(ip))
                    return false;
                if (_allowList.IsHit(ip))
                    return true;
            }
            return false;
        }

    }
}
