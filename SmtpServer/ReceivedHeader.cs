using System;
using Bjd;
using Bjd.mail;
using Bjd.net;
using Bjd.util;

namespace SmtpServer {
    class ReceivedHeader{
        private readonly Kernel _kernel;
        readonly String _headerStr = "";

        int _idCounter;//id作成のための順次番号生成カウンタ
        
        public ReceivedHeader(Kernel kernel,String headerStr) {
            _kernel = kernel;
            _headerStr = headerStr;
        }

        public String Get(MailAddress to,String host,Ip addr){

            //ユニークなID文字列の生成
            var uidStr = string.Format("bjd.{0:D20}.{1:D3}", DateTime.Now.Ticks, _idCounter++);
            //日付文字列の生成
            var date = Util.LocalTime2Str(DateTime.Now);

            var str = "";
            if (_headerStr != null) {
                str = _headerStr;

                //Ver5.0.0-b5 $aと$hをkernel.ChangeTag()に送る前に修正する
                str = Util.SwapStr("$a", addr.ToString(), str);
                str = Util.SwapStr("$h", host, str);

                str = Util.SwapStr("$i", uidStr, str);
                str = Util.SwapStr("$t", to.ToString(), str);
                str = Util.SwapStr("$d", date, str);
                str = _kernel.ChangeTag(str);
            }
            return str;
        }

    }
}
