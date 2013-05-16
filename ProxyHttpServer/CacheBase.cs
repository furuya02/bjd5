using System.Collections.Generic;
using Bjd;
using Bjd.log;

namespace ProxyHttpServer {
    //*****************************************************************
    //基底キャッシュクラス（メモリ及びディスクキャッシュの基底クラスとなる）
    //*****************************************************************
    abstract class CacheBase {
        readonly Logger _logger;
        readonly CacheKind _kind;//キャッシュの種類

        protected CacheBase(CacheKind kind, Logger logger) {
            _kind = kind;
            _logger = logger;
            Length = 0;//現在キャッシュに保持しているデータ量
        }

        public long Length { get; private set; }

        abstract protected bool AddCache(OneCache oneCache);//キャッシュ追加処理

        public bool Add(OneCache oneCache) {
            if (AddCache(oneCache)) { //キャッシュ追加処理
                Length += oneCache.Length;
                return true;
            }
            return false;
        }

        abstract protected bool RemoveCache(string hostName, int port, string uri, ref long size);//キャッシュ削除処理

        public bool Remove(string hostName, int port, string uri) {
            long size = 0;
            if (RemoveCache(hostName, port, uri, ref size)) {
                Length -= size;
                _logger.Set(LogKind.Detail, null, 25, string.Format("Remove {0} cache {1}:{2}{3}", _kind, hostName, port, uri));
                return true;
            }
            return false;
        }

//        abstract public long GetInfo(ref List<CacheInfo> infoList, int sleep, ref bool life);//キャッシュ情報取得
       abstract public long GetInfo(ref List<CacheInfo> infoList, int sleep, ILife iLife);//キャッシュ情報取得

        abstract public OneCache Get(string hostName, int port, string uri);//キャッシュ取得処理
    }
}
