using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.option;

namespace ProxyHttpServer {
    //*****************************************************************
    //抽象キャッシュクラス
    //メモリ及びディスクキャッシュを隠蔽して、１つのキャッシュとして表現する
    //*****************************************************************
    public class Cache : ThreadBase {
        readonly Logger logger;
        //readonly OneOption _oneOption;
        readonly Conf _conf;

        readonly MemoryCache _memoryCache;//メモリキャッシュ
        readonly DiskCache _diskCache;//ディスクキャッシュ

        readonly int _memorySize;//メモリキャッシュのサイズ
        readonly int _diskSize;//ディスクキャッシュのサイズ

        readonly CacheTarget _cacheTargetHost;//対象ホスト
        readonly CacheTarget _cacheTargetExt;//対象拡張子

        readonly bool _useCache;//オプション「キャッシュを使用する」
        readonly int _expires;//デフォルトの有効期限(h)
        readonly int _maxSize;//キャッシュに保存する最大ファイルサイズ

        System.Threading.Timer _timer;
        bool _cacheRefresh;//キャッシュ清掃

        public Cache(Kernel kernel, Logger logger, Conf conf)
            : base(logger) {
            this.logger = logger;
            //_oneOption = oneOption;
            _conf = conf;
            _useCache = (bool)conf.Get("useCache");

            if (!_useCache)
                return;

            _expires = (int)conf.Get("expires");
            _maxSize = (int)conf.Get("maxSize");
            _diskSize = (int)conf.Get("diskSize");
            _memorySize = (int)conf.Get("memorySize");


            //キャッシュ対象リスト
            _cacheTargetHost = new CacheTarget((Dat)conf.Get("cacheHost"), (int)conf.Get("enableHost"));
            _cacheTargetExt = new CacheTarget((Dat)conf.Get("cacheExt"), (int)conf.Get("enableExt"));

            //ディスクキャッシュ
            var cacheDir = (string)conf.Get("cacheDir");//キャッシュを保存するディレクトリ
            if (cacheDir == "" || !Directory.Exists(cacheDir)) {
                logger.Set(LogKind.Error, null, 15, string.Format("dir = {0}", cacheDir));
                _diskSize = 0;
            }
            if (_diskSize != 0) {
                _diskCache = new DiskCache(cacheDir, logger);
            }

            if (_memorySize != 0)//メモリキャッシュ
                _memoryCache = new MemoryCache(logger);

        }

        void SetTimer(long hour) {
            long msec = hour * 1000 * 60 * 60;
            _timer = new System.Threading.Timer(TimerTick, null, msec, 1000);
        }

        new public void Dispose() {
            if (_timer != null)
                _timer.Dispose();
            Stop();

            // メモリキャッシュはディスクに退避する
            if (_memoryCache != null && _diskCache != null) {
                while (true) {
                    var oneCache = _memoryCache.Old();
                    if (oneCache == null)
                        break;
                    _diskCache.Add(oneCache);
                    _memoryCache.Remove(oneCache.HostName, oneCache.Port, oneCache.Uri);
                }
            }

            base.Dispose();
        }
        override protected bool OnStartThread() {
            if (!_useCache)
                return false;//キャッシュを使用しない
            //ディスクキャッシュの定期的整理
            //Ver5.8.4
            //if (_diskSize == 0)
            //    return false;//ディスクキャッシュなし
            return true;
        }
        override protected void OnStopThread() { }
        override protected void OnRunThread() {

            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;


            var hour = (int)_conf.Get("testTime");
            SetTimer(hour);//タイマー設定

            long lastSize = 0;
            while (IsLife()) {
                if (_cacheRefresh) {
                    logger.Set(LogKind.Normal, null, 23, string.Format("Interval={0}h", hour));
                    _cacheRefresh = false;
                    var infoList = new List<CacheInfo>();

                    try {
                        long size = _diskCache.GetInfo(ref infoList, 1, this);
                        if (size != lastSize) {
                            infoList.Sort((x, y) => x != null ? x.LastAccess.CompareTo(y.LastAccess) : 0);
                            for (int i = 0; IsLife() && _diskSize * 1024 < size; i++) {
                                size -= infoList[i].Size;
                                if (!Remove(CacheKind.Disk, infoList[i].HostName, infoList[i].Port, infoList[i].Uri))
                                    break;
                            }
                            lastSize = size;
                        }
                    } catch (Exception ex) {
                        logger.Set(LogKind.Error, null, 27, ex.Message);
                    }
                    SetTimer(hour);//タイマー設定
                    logger.Set(LogKind.Normal, null, 24, string.Format("Interval={0}h", hour));
                } else {
                    Thread.Sleep(300);
                }
            }
            _timer.Dispose();
        }

        public override string GetMsg(int no){
            throw new NotImplementedException();
        }


        void TimerTick(object state) {
            _cacheRefresh = true;
            _timer.Dispose();
        }


        //リクエストがキャッシュのターゲットかどうかを判断する
        public bool IsTarget(string hostName, string uri, string ext) {
            //オプション「キャッシュを使用する」
            if (!_useCache)
                return false;

            // 対象・対象外のホストを検索する
            if (!_cacheTargetHost.IsHit(hostName)) {
                logger.Set(LogKind.Detail, null, 12, uri);
                return false;
            }
            // 対象・対象外の拡張子を検索する
            if (!_cacheTargetExt.IsMatch(ext)) {
                logger.Set(LogKind.Detail, null, 13, uri);
                return false;
            }
            return true;
        }

        // キャッシュ追加
        public bool Add(OneCache oneCache) {
            if (!_useCache)
                return false;

            if (oneCache == null)
                return false;

            //サイズが0のものは、キャッシュ対象外とする
            if (oneCache.Length <= 0) {
                return false;
            }

            if (oneCache.Length > _maxSize * 1000) {//最大サイズを超えたデータはキャッシュの対象外となる
                logger.Set(LogKind.Detail, null, 20, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
                return false;
            }
            lock (this) { // 排他制御
                //メモリキャッシュへの保存
                if (_memoryCache != null) {
                    //メモリキャッシュに収まるかどうかの判断
                    while (_memoryCache.Length + oneCache.Length > _memorySize * 1024) {
                        OneCache old = _memoryCache.Old();//一番古いものを取得する
                        if (old == null)
                            return false;
                        //一番古いものをメモリキャッシュから削除する
                        _memoryCache.Remove(old.HostName, old.Port, old.Uri);
                        //一番古いものをディスクキャッシュに保存する
                        if (_diskCache != null)
                            _diskCache.Add(old);
                    }
                    if (_memoryCache.Add(oneCache)) {
                        logger.Set(LogKind.Detail, null, 4, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
                        return true;
                    }
                }
                //ディスクキャッシュへの保存
                if (_diskCache != null) {
                    if (_diskCache.Add(oneCache)) {
                        logger.Set(LogKind.Detail, null, 5, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
                        return true;
                    }
                }
            } // 排他制御
            logger.Set(LogKind.Detail, null, 18, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
            return false;
        }

        public bool Remove(string hostName, int port, string uri) {
            bool action = false;//削除したかどうか
            // 排他制御
            lock (this) {
                if (_memoryCache != null) {
                    if (_memoryCache.Remove(hostName, port, uri))
                        action = true;
                }
                if (_diskCache != null) {
                    if (_diskCache.Remove(hostName, port, uri)) {
                        action = true;
                    }
                }
            }
            return action;
        }


        // キャッシュ削除(一覧表示からのみ呼び出される)
        public bool Remove(CacheKind cacheKind, string hostName, int port, string uri) {
            // 排他制御
            lock (this) {
                if (cacheKind == CacheKind.Memory && _memoryCache != null)
                    return _memoryCache.Remove(hostName, port, uri);
                if (cacheKind == CacheKind.Disk && _diskCache != null)
                    return _diskCache.Remove(hostName, port, uri);
            }
            return false;
        }
        // キャッシュ取得
        public OneCache Get(Request request, DateTime modified) {
            // 排他制御
            lock (this) {
                // メモリキャッシュ上に存在するかどうか？
                if (_memoryCache != null) {
                    OneCache oneCache = _memoryCache.Get(request.HostName, request.Port, request.Uri);
                    if (oneCache != null) {
                        //メモリキャッシュでヒットした 
                        if (modified.Ticks == 0 || oneCache.LastModified.Ticks == 0 || modified == oneCache.LastModified) {
                            //有効期限
                            long d = oneCache.Expires.Ticks;//ヘッダで示された場合
                            if (d == 0) {//ヘッダで示されていない場合は、本サーバのデフォルト値が使用される
                                d = oneCache.CreateDt.AddHours(_expires).Ticks;
                            }
                            if (d > DateTime.Now.Ticks) {//有効期限が切れていないかどうか
                                return oneCache;//有効キャッシュ
                            }
                        }
                        // メモリキャッシュにデータが存在するが「有効期限が経過している」もしくは、「Modifiedが一致しない」ので削除する
                        _memoryCache.Remove(request.HostName, request.Port, request.Uri);
                        if (_diskCache != null)
                            _diskCache.Remove(request.HostName, request.Port, request.Uri);
                        return null;
                    }
                }
                // ディスクキャッシュ上に存在するかどうか？
                if (_diskCache != null) {
                    OneCache oneCache = _diskCache.Get(request.HostName, request.Port, request.Uri);
                    if (oneCache != null) {
                        //ディスクキャッシュでヒットした 
                        if (modified.Ticks == 0 || oneCache.LastModified.Ticks == 0 || modified == oneCache.LastModified) {
                            //有効期限
                            long d = oneCache.Expires.Ticks;//ヘッダで示された場合
                            if (d == 0) {//ヘッダで示されていない場合は、本サーバのデフォルト値が使用される
                                d = oneCache.CreateDt.AddHours(_expires).Ticks;
                            }
                            if (d > DateTime.Now.Ticks) {//有効期限が切れていないかどうか
                                //メモリキャッシュへの移動
                                if (_memoryCache != null && _memoryCache.Add(oneCache)) {
                                    logger.Set(LogKind.Detail, null, 19, string.Format("{0}:{1}{2}", oneCache.HostName, oneCache.Port, oneCache.Uri));
                                }
                                return oneCache;//有効キャッシュ
                            }
                        }
                        //ディスクキャッシュにデータが存在するが「有効期限が経過している」もしくは、「Modifiedが一致しない」ので削除する
                        _diskCache.Remove(request.HostName, request.Port, request.Uri);
                        return null;
                    }
                }
            }// 排他制御
            return null;
        }
        // キャッシュ状態取得
        public long GetInfo(CacheKind cacheKind, ref List<CacheInfo> infoList) {
            if (cacheKind == CacheKind.Memory) {//メモリ
                if (_memoryCache != null)
                    return _memoryCache.GetInfo(ref infoList, 0, this);
            } else {//ディスク
                if (_diskCache != null)
                    return _diskCache.GetInfo(ref infoList, 0, this);
            }
            return 0;

        }
    }
}
