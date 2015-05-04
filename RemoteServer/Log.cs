
namespace RemoteServer {
    partial class Server {
        //BJD.Lang.txtに必要な定義が揃っているかどうかの確認
        protected override void CheckLang()
        {
            for (var n = 1; n <= 5; n++)
            {
                Lang.Value(n);
            }
        }

        public override string GetMsg(int messageNo)
        {
            switch (messageNo)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                    return Lang.Value(messageNo);
            }
            return "unknown";
        }
    }
}
