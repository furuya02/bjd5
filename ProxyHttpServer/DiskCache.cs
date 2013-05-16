using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.util;

namespace ProxyHttpServer {
    //*****************************************************************
    //ディスクキャッシュクラス
    //*****************************************************************
    class DiskCache : CacheBase {
        readonly string _dir;
        public DiskCache(string dir, Logger logger)
            : base(CacheKind.Disk, logger) {
            _dir = dir;
        }

        string CreatePath(string hostName, int port, string uri) {
            var path = string.Format("{0}\\{1}_{2}\\{3}", _dir, hostName, port, uri);
            if (path[path.Length - 1] == '/') {
                path = path + "$$$";
            }
            path = Util.SwapChar('/', '\\', path);
            return path;
        }

        //再帰処理関数
        //long GetInfo1(ref List<CacheInfo> infoList, string path, int sleep, ref bool life) {
        long GetInfo1(ref List<CacheInfo> infoList, string path, int sleep, ILife iLife) {
            var dt = new DateTime(0);
            var size = 0L;
            foreach (string file in Directory.GetDirectories(path)) {
                size += GetInfo1(ref infoList, file, sleep, iLife);

                if (!iLife.IsLife())
                    return 0;
                Thread.Sleep(sleep);

                foreach (string name in Directory.GetFiles(file)) {
                    if (!iLife.IsLife())
                        return 0;
                    Thread.Sleep(sleep);

                    var fi = new FileInfo(name);

                    var str = name.Substring(_dir.Length + 1);
                    str = Util.SwapChar('\\', '/', str);
                    str = Util.SwapStr("$$$", "", str);

                    var index = str.IndexOf('/');
                    if (0 <= index) {
                        var host = str.Substring(0, index);
                        var uri = str.Substring(index);
                        index = host.LastIndexOf('_');
                        if (0 <= index) {
                            var hostName = host.Substring(0, index);
                            var portStr = host.Substring(index + 1);
                            var port = Convert.ToInt32(portStr);
                            infoList.Add(new CacheInfo(hostName, port, uri, dt, dt, fi.CreationTime, fi.LastAccessTime, fi.Length));
                        }
                    }
                    size += fi.Length;
                }
            }
            return size;
        }

        //キャッシュ情報取得処理
//        public override long GetInfo(ref List<CacheInfo> infoList, int sleep, ref bool life) {
//            return GetInfo1(ref infoList, _dir, sleep, ref life);//再帰処理
//        }

        public override long GetInfo(ref List<CacheInfo> infoList, int sleep, ILife iLife){
            return GetInfo1(ref infoList, _dir, sleep, iLife); //再帰処理
        }

        //キャッシュ追加処理
        override protected bool AddCache(OneCache oneCache) {
            //サイズ制限は無視して書き込む
            //サイズ制限をオーバーしたものは、Cache.Interval()で削除される
            if (oneCache.Uri.Length > 9600) {
                //ディスクキャッシュの場合は、これはきついかも
                //エラーにする？
                return false;
            }

            if (oneCache.Uri.IndexOf(':') != -1) {
                // URLに:が含まれているとき、ディスクには書き込まない
                return false;
            }
            var path = CreatePath(oneCache.HostName, oneCache.Port, oneCache.Uri);
            oneCache.Save(path);
            return true;
        }

        //キャッシュ削除処理
        override protected bool RemoveCache(string hostName, int port, string uri, ref long size) {
            var path = CreatePath(hostName, port, uri);
            if (File.Exists(path)) {
                File.Delete(path);
                return true;
            }
            return false;
        }

        //キャッシュ取得処理
        public override OneCache Get(string hostName, int port, string uri) {
            var path = CreatePath(hostName, port, uri);
            if (File.Exists(path)) {
                var oneCache = new OneCache(hostName, port, uri);
                if (oneCache.Read(path)) {
                    oneCache.LastAccess = DateTime.Now;//最終アクセス時刻の記録
                    return oneCache;
                }
            }
            return null;
        }
    }
}
