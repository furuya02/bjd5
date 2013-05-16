using System;

namespace ProxyHttpServer {
    //キャッシュ情報を取得するためのクラス
    public class CacheInfo {
        public CacheInfo(string hostName, int port, string uri, DateTime lastModified, DateTime expires, DateTime createDt, DateTime lastAccess, long size) {
            HostName = hostName;
            Port = port;
            Uri = uri;
            LastModified = lastModified;//ドキュメントの最終更新日時（ヘッダに指定されていない場合は0となる）
            Expires = expires;//有効期限（ヘッダに指定されていない場合は0となる）
            CreateDt = createDt;//このOneCacheが作成された日時（expire保存期間に影響する）
            LastAccess = lastAccess;//このOneCahceを最後に使用した日時（キャッシュＤＢに留まるかどうかの判断に使用される）
            Size = size;
        }

        //****************************************************************
        //プロパティ
        //****************************************************************
        public DateTime LastModified { get; private set; }
        public DateTime Expires { get; private set; }
        public DateTime CreateDt { get; private set; }
        public DateTime LastAccess { get; private set; }
        public string HostName { get; private set; }
        public int Port { get; private set; }
        public string Uri { get; private set; }
        public long Size { get; private set; }
        public string Url {
            get {
                return string.Format("http://{0}_{1}{2}", HostName, Port, Uri);
            }
        }
        //ToString()を戻すためのコンストラクタ
        public CacheInfo(string str) {
            string[] tmp = str.Split('\t');
            if (tmp.Length != 8) {
                HostName = "";
                Port = 0;
                Uri = "";
                LastModified = new DateTime(0);
                Expires = new DateTime(0);
                CreateDt = new DateTime(0);
                LastAccess = new DateTime(0);
                Size = 0;
                return;
            }
            HostName = tmp[0];
            Port = Convert.ToInt32(tmp[1]);
            Uri = tmp[2];
            LastModified = new DateTime(Convert.ToInt64(tmp[3]));
            Expires = new DateTime(Convert.ToInt64(tmp[4]));
            CreateDt = new DateTime(Convert.ToInt64(tmp[5]));
            LastAccess = new DateTime(Convert.ToInt64(tmp[6]));
            Size = Convert.ToInt64(tmp[7]);
        }

        public override string ToString() {
            return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                HostName,
                Port.ToString(),
                Uri,
                LastModified.Ticks.ToString(),
                Expires.Ticks.ToString(),
                CreateDt.Ticks.ToString(),
                LastAccess.Ticks.ToString(),
                Size);
        }
    }
}
