
namespace ProxyTelnetServer {
    partial class Server {
        //BJD.Lang.txtに必要な定義が揃っているかどうかの確認
        protected override void CheckLang()
        {
            for (var n = 1; n <= 3; n++)
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
                    return Lang.Value(messageNo);
            }
            return "unknown";
        }

    }
}
