using System.IO;

namespace WebServer {
    class WebServerUtil {
        //ETagを生成する サイズ+更新日時（秒単位）
        public static string Etag(FileInfo fileInfo) {
            if (fileInfo != null)
                return string.Format("\"{0:x}-{1:x}\"", fileInfo.Length, (fileInfo.LastWriteTimeUtc.Ticks / 10000000));
            return "";
        }
    }
}
