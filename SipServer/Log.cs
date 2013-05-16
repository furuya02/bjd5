
namespace SipServer {
    partial class Server {
        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1: return Kernel.IsJp() ? "日本語" : "English";//この形式でログ用のメッセージ追加できます。
            }
            return "unknown";
        }
    }
}
