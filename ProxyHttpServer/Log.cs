
namespace ProxyHttpServer {
    partial class Server {
        protected override void CheckLang()
        {
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 0:  return Kernel.IsJp()?"リクエスト":"Request";
                case 1:  return Kernel.IsJp()?"サポート外のメソッドです（処理を継続できません）":"It is a method out of a support (Cannot continue processing)";
                case 2:  return Kernel.IsJp()?"サポート外のバージョンです（処理を継続できません）":"It is a version out of a support(Cannot continue processing)";
                case 3:  return Kernel.IsJp()?"タイムアウト":"Timeout";
                case 4:  return Kernel.IsJp()?"キャッシュ（メモリ）へ保存しました":"Saved it to cash (MEMORY)";
                case 5:  return Kernel.IsJp()?"キャッシュ（ディスク）へ保存しました":"Saved it to cash (DISK)";
                case 6:  return Kernel.IsJp()?"レスポンス受信に失敗しました":"Failed in the reception of a response";
                case 7:  return Kernel.IsJp()?"ヘッダ受信に失敗しました":"Failed in the reception of a header";
                case 8:  return "BREAK";
                case 9:  return Kernel.IsJp()?"送信に失敗しました":"Transmission of a message failure";
                case 10: return Kernel.IsJp()?"URL制限にヒットしました":"An URL limit";
                case 11: return Kernel.IsJp()?"名前解決に失敗しました。サーバへ接続できません":"Name solution failure(Cannot be connected to a server";
                case 12:  return Kernel.IsJp()?"キャッシュ対象ではありません（ホスト）":"It is not a cash object (Host)";
                case 13:  return Kernel.IsJp()?"キャッシュ対象ではありません（拡張子）":"It is not a cash object (Extension)";
                case 14:  return Kernel.IsJp()?"キャッシュにヒットしました":"Hit in cache";
                case 15:  return Kernel.IsJp()?"「キャッシュ保存ディレクトリ」の指定が無効です。ディスクキャッシュは機能できません":"Appointment of \"save directory\" is null and void(A disk cache cannot function)";
                case 16:  return Kernel.IsJp()?"「no cache」のためキャッシュしません":"Don't do cash for \"no cache\"";
                case 17:  return Kernel.IsJp()?"キャッシュに保存しました":"saved it in cache";
                case 18:  return Kernel.IsJp()?"キャッシュへの保存をキャンセルしました":"I did not save it in cache";
                case 19:  return Kernel.IsJp()?"キャッシュ（メモリ）へ移動しました":"I moved to cache (memory)";
                case 20:  return Kernel.IsJp()?"最大サイズを超えているのでキャッシュへの保存をキャンセルします":"Because I exceed maximum size, I cancel a save to cache";
                case 21:  return Kernel.IsJp()?"コンテンツ制限にヒットしました（接続は中断されました）":"I made a hit in contents limit (the connection was stopped)";
                case 22:  return Kernel.IsJp()?"POSTリクエストの処理に失敗しました" : "Failed in processing of a POST request";
                case 23:  return Kernel.IsJp()?"ディスクキャッシュの最適化を開始します":"Start optimization of a disk cache";
                case 24:  return Kernel.IsJp()?"ディスクキャッシュの最適化を終了します":"Stop optimization of a disk cache";
                case 25:  return Kernel.IsJp()?"古いキャッシュを削除します":"Remove old cash";
                case 26:  return Kernel.IsJp()?"接続に失敗しました":"Failed in connection";
                case 27: return Kernel.IsJp() ? "ディスクキャッシュの最適化でエラーが発生しました" : "An error occurred by optimization of disk cache";
                case 28: return Kernel.IsJp() ? "URL制限で解釈できない正規表現が設定されました" : "The regular expression that I cannot interpret by an URL limit was set";
                case 29: return Kernel.IsJp() ? "設計エラー　Request.Recv()" : "Request.Recv()";
                //case 27:  return kernel.IsJp() ? "HTTP/1.1 は使用できません" : "Cannot use HTTP /1.1";
            }
            return "unknown";
        }
    }
   
}
