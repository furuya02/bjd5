
namespace TftpServer {
    partial class Server {
        //BJD.Lang.txtに必要な定義が揃っているかどうかの確認
        protected override void CheckLang()
        {
            for (var n = 1; n <= 14; n++)
            {
                Lang.Value(n);
            }
        }


        public override string GetMsg(int messageNo) {
            switch (messageNo) {
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
                    return Lang.Value(messageNo);
            }
            return "unknown";
        }
    }
}
