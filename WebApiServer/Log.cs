using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiServer {
    public partial class Server {
        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1: return Kernel.IsJp() ? "日本語" : "English";//この形式でログ用のメッセージ追加できます。
            }
            return "unknown";
        }
    }
}

