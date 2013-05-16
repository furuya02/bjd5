using System;
using System.Collections.Generic;
using Bjd;
using Bjd.log;

namespace ProxyHttpServer {
    //*****************************************************************
    //メモリキャッシュクラス
    //*****************************************************************
    class MemoryCache : CacheBase {
        readonly List<OneCache> _ar = new List<OneCache>();

        public MemoryCache(Logger logger)
            : base(CacheKind.Memory, logger) {
        }

        //キャッシュ情報取得処理
        //        public override long GetInfo(ref List<CacheInfo> infoList, int sleep, ref bool life) {
        public override long GetInfo(ref List<CacheInfo> infoList, int sleep, ILife iLife) {
            var size = 0L;
            foreach (var o in _ar) {
                if (!iLife.IsLife())
                    return 0;
                infoList.Add(new CacheInfo(o.HostName, o.Port, o.Uri, o.LastModified, o.Expires, o.CreateDt, o.LastAccess, o.Length));
                size += o.Length;
            }
            return size;
        }

        //キャッシュ追加処理
        override protected bool AddCache(OneCache oneCache) {
            oneCache.LastAccess = DateTime.Now;//最終アクセス時刻の記録
            // メモリ上のデータの差し替えに該当する場合
            var o = Get(oneCache.HostName, oneCache.Port, oneCache.Uri);
            if (o != null) {
                Remove(oneCache.HostName, oneCache.Port, oneCache.Uri);
            }
            //キャッシュを追加
            _ar.Add(oneCache);
            return true;
        }

        //キャッシュ削除処理
        override protected bool RemoveCache(string hostName, int port, string uri, ref long size) {
            foreach (var oneCache in _ar) {
                if (oneCache.HostName.ToUpper() == hostName.ToUpper() && oneCache.Uri == uri && oneCache.Port == port) {
                    size = oneCache.Length;
                    if (_ar.Remove(oneCache)) {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        //キャッシュ取得処理
        public override OneCache Get(string hostName, int port, string uri) {
            foreach (var oneCache in _ar) {
                if (oneCache.HostName.ToUpper() == hostName.ToUpper() && oneCache.Uri == uri && oneCache.Port == port) {
                    oneCache.LastAccess = DateTime.Now;//最終アクセス時刻の記録
                    return oneCache;
                }
            }
            return null;
        }

        // アクセス時間が一番古いデータ取得する
        public OneCache Old() {
            OneCache result = null;
            var dt = DateTime.Now;
            foreach (var oneCache in _ar) {
                if (dt.Ticks > oneCache.LastAccess.Ticks) {
                    result = oneCache;
                }
            }
            return result;
        }
    }
}
