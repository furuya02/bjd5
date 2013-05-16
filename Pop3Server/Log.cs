
namespace Pop3Server {
    partial class Server {
        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1:return IsJp ? "２重ログインの要求が発生しました":"A demand of double login occurred";
                case 2:return IsJp ? "ログインしました":"Login";
                case 3:return IsJp ? "認証に失敗しました":"Certification failure";
                case 4:return IsJp ? "メールボックスの初期化に失敗しました。サーバは起動できません":"Failed in initialization of a mailbox (A server cannot start)";
                case 5:return IsJp ? "メールを受信しました [RETR]" : "Received an email [RETR]";
            }
            return "unknown";
        }
    }
}
