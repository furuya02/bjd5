
namespace SampleServer {
    partial class Server{
        protected override void CheckLang()
        {
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1:return Kernel.IsJp() ? "日本語" : "English";//この形式でログ用のメッセージ追加できます。
            }
            return "unknown";
        }
    }
}
