
namespace WebServer {
    partial class Server {
        //BJD.Lang.txtに必要な定義が揃っているかどうかの確認
        protected override void CheckLang()
        {
            for (var n = 0; n <= 2; n++)
            {
                Lang.Value(n);
            }
            for (var n =5; n <= 11; n++)
            {
                Lang.Value(n);
            }
            for (var n = 13; n <= 16; n++)
            {
                Lang.Value(n);
            }
            for (var n = 20; n <= 37; n++)
            {
                Lang.Value(n);
            }
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 0: 
                case 1: 
                case 2:
                    return Lang.Value(messageNo);
                case 3:  return "request";//詳細ログ用
                case 4:  return "response";//詳細ログ用

                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                    return Lang.Value(messageNo);

                //case 12: return "";
                
                case 13:
                case 14:
                case 15:
                case 16:
                    return Lang.Value(messageNo);

                
                case 17: return "exec SSI";
                case 18: return "execute";
                
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
                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                    return Lang.Value(messageNo);
                
                case 38: return "POST data recved";
                case 39: return "POST data recved";
                case 40: return "faild POST data recve.";
                case 41: return "faild POST data recve.";
            }
            return "unknown";
        }

    }
}
