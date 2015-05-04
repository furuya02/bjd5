
namespace ProxyHttpServer {
    partial class Server {
        //BJD.Lang.txtに必要な定義が揃っているかどうかの確認
        protected override void CheckLang()
        {
            for (var n = 0; n <= 29; n++)
            {
                Lang.Value(n);
            }
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                case 29:
                    return Lang.Value(messageNo);
            }
            return "unknown";
        }
    }
   
}
