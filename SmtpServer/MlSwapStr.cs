using Bjd;
using Bjd.util;

namespace SmtpServer {
    class MlSwapStr {
        static public string ConvertAddr(string str,MlAddr mlAddr){
            str = Util.SwapStr("$ML_NAME", mlAddr.Name, str);
            str = Util.SwapStr("$POST_ADDR", mlAddr.Post.ToString(), str);
            str = Util.SwapStr("$CTRL_ADDR", mlAddr.Ctrl.ToString(), str);
            str = Util.SwapStr("$ADMIN_ADDR", mlAddr.Admin.ToString(), str);
            return str;
        }
    }
}
