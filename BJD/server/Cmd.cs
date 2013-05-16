using System;

namespace Bjd.server{
    //受信したコマンドを表現するクラス
    //内部データは、nullの場合、""で初期化される
    public class Cmd{
        public String Str { get; private set; }
        public String CmdStr { get; private set; }
        public String ParamStr { get; private set; }

        public Cmd(String str, String cmdStr, String paramStr){
            Str = str ?? "";
            CmdStr = cmdStr ?? "";
            ParamStr = paramStr ?? "";
        }
    }
}
