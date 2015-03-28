
namespace WebServer {
    partial class Server {
        protected override void CheckLang(){
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 0:  return Kernel.IsJp()?"リクエストの解釈に失敗しました（不正なリクエストの可能性があるため切断しました)":"Failed in interpretation of a request (I cut it off so that there was possibility of an unjust request in it)";
                case 1:  return Kernel.IsJp()?"サポート外のメソッドです（処理を継続できません）":"It is a method out of a support (Cannot continue processing)";
                case 2:  return Kernel.IsJp()?"サポート外のバージョンです（処理を継続できません）":"It is a version out of a support (Cannot continue processing)";
                case 3:  return "request";//詳細ログ用
                case 4:  return "response";//詳細ログ用
                case 5: return Kernel.IsJp() ? "URIの解釈に失敗しました（不正なリクエストの可能性があるため切断しました)" : "failed in interpretation of URI (I cut it off so that there was possibility of an unjust request in it)";
                case 6:  return Kernel.IsJp()?"認証エラー（認証リストに定義されていないユーザからのアクセスです）":"A certification error (it is access from the user who is not defined by a certification list)";
                case 7:  return Kernel.IsJp()?"認証エラー（ユーザリストに当該ユーザの情報がありません）":"A certification error (a user list does not include information of the user concerned)";
                case 8:  return Kernel.IsJp()?"認証成功":"Certification success";
                case 9:  return Kernel.IsJp()?"認証エラー（パスワードが違います）":"";
                case 10: return Kernel.IsJp()?"このアドレスからのリクエストは許可されていません":"";
                case 11: return Kernel.IsJp()?"この利用者のアクセスは許可されていません":"A certification error (a password is different)";
                //case 12: return "";
                case 13: return Kernel.IsJp()?".. が含まれるリクエストは許可されていません":"The request that .. is included in is not admitted";
                case 14: return Kernel.IsJp()?"ドキュメントルートで指定されたフォルダが存在しません（処理を継続できません）":"There is not a folder appointed by a DocumentRoot (Cannot continue processing)";
                case 15: return Kernel.IsJp()?"SSI #include 自分自身をインクルードすることはできません":"SSI #include Cannot do include of oneself";
                case 16: return Kernel.IsJp()?"CGI 実行エラー":"CGI execution error";
                case 17: return "exec SSI";
                case 18: return "execute";
                case 20: return Kernel.IsJp()?"パラメータの解釈に失敗しました":"Failed in interpretation of a parameter";
                case 21: return Kernel.IsJp() ? "SSIの処理に失敗しました" : "Failed in processing of small scale integration";
                case 22: return Kernel.IsJp()?"SSI 実行エラー":"SSI execution error";
                case 23: return Kernel.IsJp()?"リクエスト[HTTPS]":"Request[HTTPS]";//ノーマルログ用
                case 24: return Kernel.IsJp()?"リクエスト":"Request";//ノーマルログ用
                case 25: return Kernel.IsJp()?"雛型(エラードキュメント)が入力されていません":"A model is not appointed(Error Document)";
                case 26: return Kernel.IsJp()?"雛型(インデックスドキュメント)が入力されていません":"A model is not appointed(Index Document)";
                case 27: return Kernel.IsJp()?"CGI以外のファイルが指定されています" : "An appointed file is not CGI";
                case 28: return Kernel.IsJp() ? "#exec は、cmd及びcgiしか指定できません" : "\"#exec\" can appoint only \"cgi\" and \"cmd\"";
                case 29: return Kernel.IsJp() ? "ディレクトリを削除できませんでした" : "Failed in elimination of a directory";
                case 30: return Kernel.IsJp() ? "ファイルを削除できませんでした" : "Failed in elimination of a file";
                case 31: return Kernel.IsJp() ? "ファイルの作成に失敗しました" : "Failed in making of a file";
                case 32: return Kernel.IsJp() ? "ディレクトリを削除できませんでした" : "Failed in elimination of a directory";
                case 33: return Kernel.IsJp() ? "ファイルを削除できませんでした" : "Failed in elimination of a file";
                case 34: return Kernel.IsJp() ? "ディレクトリの移動(コピー)に失敗しました" : "Failed in movement(copy) of a directory";
                case 35: return Kernel.IsJp() ? "ファイルの移動(コピー)に失敗しました" : "Failed in movement(copy) of a file";
                case 36: return Kernel.IsJp() ? "ディレクトリの作成に失敗しました" : "Failed in making of a directory";
                case 37: return Kernel.IsJp() ? "URLの解釈に失敗しました" : "Failed in interpretation of URL";
                case 38: return "POST data recved";
                case 39: return "POST data recved";
                case 40: return "faild POST data recve.";
                case 41: return "faild POST data recve.";
            }
            return "unknown";
        }

    }
}
