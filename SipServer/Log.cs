
namespace SipServer {
    partial class Server {
        //BJD.Lang.txtに必要な定義が揃っているかどうかの確認
        protected override void CheckLang()
        {
            Lang.Value(1);
        }

        public override string GetMsg(int messageNo)
        {
            switch (messageNo)
            {
                case 1:
                    return Lang.Value(messageNo);
            }
            return "unknown";
        }
    }
}
