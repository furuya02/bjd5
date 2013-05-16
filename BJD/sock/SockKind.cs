using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bjd.sock {
    public enum SockKind {
        //bindされたサーバから生成されたソケット UDPの場合はクローンなのでclose()しない
        ACCEPT,
        //
        CLIENT
    }
}
