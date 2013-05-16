
namespace ProxyFtpServer {
    partial class Server {
        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1: return Kernel.IsJp()?"認証に失敗しました":"Failed in the certification";
                case 2: return Kernel.IsJp()?"アイドルタイムアウト":"An idle time out";
                case 3: return Kernel.IsJp()?"送信に失敗しました":"Transmission of a message failure";
                case 4: return Kernel.IsJp()?"PORTコマンドの解釈に失敗しました":"Interpretation failure of a command(PORT)";
                case 5: return Kernel.IsJp()?"PASVコマンドの解釈に失敗しました(1)":"Interpretation failure of a command(PASV)[1]";
                case 6: return Kernel.IsJp()?"PASVコマンドの解釈に失敗しました(2)":"Interpretation failure of a command(PASV)[2]";
                case 7: return Kernel.IsJp()?"コマンド受信":"The command reception";
                case 8: return Kernel.IsJp() ? "「USER ユーザ名@ホスト名」の接続形式しか対応できません。" : "Only a connection form of [USER username@hostname] can support.";

            }
            return "unknown";
        }

    }
}
