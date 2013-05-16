using System.Collections.Generic;
using Bjd;

namespace SmtpServer {
    class MlDoc {
        readonly List<string> ar = new List<string>();
        public MlDoc(List<string> docs, MlAddr mlAddr) {
            foreach (var s in docs) {
                var buf = Util.SwapStr("$ML_NAME", mlAddr.Name, s);
                buf = Util.SwapStr("$POST_ADDR", mlAddr.Post.ToString(), buf);
                buf = Util.SwapStr("$CTRL_ADDR", mlAddr.Ctrl.ToString(), buf);
                buf = Util.SwapStr("$ADMIN_ADDR", mlAddr.Admin.ToString(), buf);
                ar.Add(buf);
            }
        }
        public string Get(MLDocKind kind) {
            return ar[(int)kind];
        }
    }
}