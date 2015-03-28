
namespace FtpServer {
    partial class Server {


        //BJD.Lang.txtに必要な定義が揃っているかどうかの確認
        protected override void CheckLang()
        {
            for (var n = 1; n <= 3; n++){
                Lang.Value(n);
            }
            for (var n = 14; n <= 15; n++){
                Lang.Value(n);
            }
        }
        public override string GetMsg(int messageNo){
            switch (messageNo)
            {
                case 1:
                case 2:
                case 3:
                    return Lang.Value(messageNo);
                case 5:
                    return "login";
                case 6:
                    return "login";
                case 7:
                    return "success";
                case 8:
                    return "RENAME";
                case 9:
                    return "UP start";
                case 10:
                    return "UP end";
                case 11:
                    return "DOWN start";
                case 12:
                    return "DOWN end";
                case 13:
                    return "logout";
                case 14:
               case 15:
                    return Lang.Value(messageNo);
                case 16:
                    return "sendBinary() IOException";
                case 17:
                    return "recvBinary() IOException";
                case 18:
                    return "Exception [session.CurrentDir.CreatePath]";
            }
            return null;
        }

    }
}
