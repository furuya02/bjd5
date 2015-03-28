
namespace SmtpServer {
    public partial class Server {
        protected override void CheckLang(){
        }
        public override string GetMsg(int messageNo)
        {
            switch (messageNo) {
                case 0: return "MESSAE";
                case 1: return Kernel.IsJp()?"接続しました":"Connected";
                case 2: return Kernel.IsJp()?"ディレクトリ指定（メールボックス）に問題があります。（サーバは機能しません）":"Directory appointment (a mailbox) includes a problem(Server start failure)";
                case 3: return Kernel.IsJp()?"ドメイン名が指定されていません（サーバは機能しません）":"A domain name is not appointed(Server start failure)";
                case 4:return Kernel.IsJp()?"メールボックスの初期化に失敗しました(サーバは起動できません)":" failed in initialization of a mailbox(Server start failure)";
                case 5: return Kernel.IsJp()?"POP bfore SMTPによるリレー中継を許可しました":"admitted relay broadcast by POP before SMTP";
                case 6: return Kernel.IsJp()?"ユーザが存在しません":"There is not a user";
                case 7: return Kernel.IsJp()?"受信サイズの制限を越えました":"Exceeded a limit of reception size";
                case 8: return Kernel.IsJp()?"メールボックスへ格納しました":"Housed it to a mailbox";
                case 9: return Kernel.IsJp()?"メールキューへ格納しました":"Housed it to a mailqueue";
                case 10: return Kernel.IsJp()?"キュー処理(開始)":"Queue processing (start)";
                case 11: return Kernel.IsJp()?"キュー処理(成功)":"Queue processing (success)";
                case 12: return Kernel.IsJp()?"キュー処理(失敗) サーバ（アドレス）検索失敗":"Queue processing (Server search failure)";
                case 13: return Kernel.IsJp()?"キュー処理(失敗) 原因不明":"Queue processing (faild) A cause is unknown";
                case 14: return Kernel.IsJp()?"キュー処理(失敗) エラーコードを受信しました":"Queue processing (faild) Received an error cord";
                case 15: return Kernel.IsJp()?"エラーメールを作成しました":"Made an error email";
                case 16: return Kernel.IsJp()?"ヘッダを置き換えました":"Moved a header";
                case 17: return Kernel.IsJp()?"ヘッダを追加しました":"Added a header";
                //case 18: return Kernel.IsJp()?"UUCPアドレスには対応していません":"Not equivalent to an UUCP address";
                case 19: return Kernel.IsJp()?"エリアス指定が無効です（ユーザが存在しません）":"Elias appointment is invalidity (there is not a user)";
                case 20: return Kernel.IsJp()?"「基本オプション」－「サーバ名」が指定されていません(サーバによっては、送信に失敗する可能性があります)":"Option [Basic Option]-[Saerver Name]  is not appointed(With a server, I may fail in the transmission of a message)";
                case 21: return Kernel.IsJp()?"ファイルにメールが追加されました" : "An email was added to a file";
                case 22: return Kernel.IsJp()?"ファイルへのメール追加に失敗しました" : "I failed in email addition to a file";
                case 23: return Kernel.IsJp()?"自動受信" : "The automatic reception";
                //case 24: return Kernel.IsJp()?"サーバへの接続に失敗しました(自動受信)" : "Connection failure to a server(The automatic reception)";
                case 25: return Kernel.IsJp()?"中継許可の指定に問題があります" : "Relay configuration failure";
                case 26: return Kernel.IsJp()?"この接続には拡張SMTPが適用されません" : "ESMP is not applied in this connection";
                case 27: return Kernel.IsJp() ? "エリアス変換" : "Alias";
                case 28: return Kernel.IsJp() ? "メールアドレスがローカルドメインではありません（From偽造を許可しない）" : "There is not an email address in a local domain (From: Check)";
                case 29: return Kernel.IsJp() ? "メールアドレスがローカルユーザではありません（From偽造を許可しない）" : "There is not an email address in a local user (From: Check)";
                case 30: return Kernel.IsJp() ? "１ユーザのエリアスを複数行で指定することはできません。別名はカンマで区切って複数指定できます。" : "Can't appoint plural Elias of a 1 user in a line, and another name divides it in a comma and can appoint a plural number.";
                case 31: return Kernel.IsJp() ? "「管理領域（フォルダ）」の指定が無効です。MLは機能できません" : "Appointment of \"management directory\" is null and void(A ML cannot function)";
                case 32: return Kernel.IsJp() ? "(ML)メールの保存に失敗しました" : "(ML)Failed in a save of an email";
                case 33: return Kernel.IsJp() ? "投稿を受け付けました" : "Accepted a contribution";
                case 34: return Kernel.IsJp() ? "メンバー以外からの投稿です" : "It is a contribution from not member";
               // case 35: return kernel.IsJp() ? "メンバーへの配信に成功しました" : "Success delivery it";
               // case 36: return kernel.IsJp() ? "メンバーへの配信に失敗しました" : "Error delivery it";
                case 37: return Kernel.IsJp() ? "許可されないユーザからの制御メールです" : "It is the control demand that is not admitted";
                case 38: return Kernel.IsJp() ? "(ML)配信に成功しました" : "(ML)Success delivery it";
                case 39: return Kernel.IsJp() ? "(ML)配信に失敗しました" : "(ML)Error delivery it";
                case 40: return Kernel.IsJp() ? "制御コマンドの解釈に失敗しました" : "Failed in interpretation of a control command";
                case 41: return Kernel.IsJp() ? "制御コマンドを実行します" : "Execute a control command";
                case 42: return Kernel.IsJp() ? "Guideを送信しました" : "Transmitted a guide";
                case 43: return Kernel.IsJp() ? "投稿を許可されていないメンバーからの投稿です" : "It is POST from the member that it is not admitted a contribution";
                case 44: return Kernel.IsJp() ? "(ML)メーリングリストが有効になりました" : "(ML)A mailing list became effective";
                case 45: return Kernel.IsJp() ? "エリアス指定が無効です" : "Elias appointment is invalidity";
                case 46: return Kernel.IsJp() ? "メンバーを追加しました" : "Added a member";
                case 47: return Kernel.IsJp() ? "このコマンドを発行する権限がありません" : "There is not authority to execute this command";
                case 48: return Kernel.IsJp() ? "メンバーの追加に失敗しました" : "Failed in addition of a member";
                case 49: return Kernel.IsJp() ? "コマンド実行でエラーが発生しました" : "An error occurred by a command";
                case 50: return Kernel.IsJp() ? "管理者ログインが必要です" : "Manager login is necessary";
                case 51: return Kernel.IsJp() ? "パラメータに問題があります" : "A parameter includes a problem";
                case 52: return Kernel.IsJp() ? "メールアドレスがローカルユーザではありません（From偽造を許可しない）" : "There is not an email address in a local user (From: Check)";
                case 53: return Kernel.IsJp() ? "(ML)メンバ（管理者）のメールアドレスに問題があります":"(ML)There is a problem to an email address of a member (a manager)";
                case 54: return Kernel.IsJp() ? "無効コマンドが既定回数を超えました。処理を強制切断します。" : "Unknown command exceeded established frequency";//Ver5.4.7
                case 55: return Kernel.IsJp() ? "メールボックスへの格納に失敗しました" : "Failed in housing to a mailbox";
                case 56: return Kernel.IsJp() ? "サーバとの接続に失敗しました" : "Failed in connection with a server";
                case 57: return Kernel.IsJp() ? "(ML)メンバが存在しません" : "(ML)There is not a memberｓ";
            }
            return "unknown";
        }

    }
}
