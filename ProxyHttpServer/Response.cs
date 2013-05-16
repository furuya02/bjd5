using System;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyHttpServer {
    internal class Response {
        string _responseStr = "";
        Logger _logger;
        //データ取得（内部データは、初期化される）
        public bool Recv(Logger logger, SockTcp sockTcp, int timeout, ILife iLife) {
            _logger = logger;
            //int limit = 3600;//文字数制限
            var str = sockTcp.AsciiRecv(timeout, iLife);
            if (str == null){
                return false;
            }
            return Interpretation(Inet.TrimCrlf(str));
        }
        //キャッシュ用
        public bool Recv(string str) {
            return Interpretation(str);
        }

        bool Interpretation(string str) {
            Code = 0;
            if (str == null) {
                _logger.Set(LogKind.Debug, null, 702, "Interpretation() str==null");
                return false;
            }
            _responseStr = str;
            var tmp = _responseStr.Split(' ');
            if (tmp.Length >= 2) {
                try {
                    Code = Convert.ToInt32(tmp[1]);
                    return true;
                } catch {
                    _logger.Set(LogKind.Debug, null, 703, string.Format("Interpretation() catch() tmp[1]={0}", tmp[1]));
                    return false;
                }
            }
            _logger.Set(LogKind.Debug, null, 704, string.Format("Interpretation() catch() tmp.Length = {0}", tmp.Length));
            return false;
        }


        public int Code { get; private set; }
        public override string ToString() {
            return _responseStr;
        }
    }
}
