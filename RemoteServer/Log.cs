
namespace RemoteServer {
    partial class Server {
        protected override void CheckLang()
        {
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1: return (Kernel.IsJp())?"リモートクライアントから接続されました":"Connected by a RemoteClient";
                case 2: return (Kernel.IsJp())?"リモートクライアントからの接続が解除されました":"Disconnected by a RemoteClient";
                case 3: return (Kernel.IsJp())?"リモートサーバスレッドが初期化されていません":"A RemoteServerThread is not initialized";
                case 4: return (Kernel.IsJp())?"パスワードが違います":"Password incorrect";
                case 5: return (Kernel.IsJp())?"認証パスワードは設定されていません":"No password";
            }
            return "unknown";
        }

    }
}
