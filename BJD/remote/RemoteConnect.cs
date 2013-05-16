using System.Runtime.InteropServices;
using Bjd.net;
using Bjd.sock;
using Bjd.trace;

namespace Bjd.remote {
    //リモートサーバ側で動作しているときにクライアントへのアクセスするためのオブジェクト
    public class RemoteConnect {
        readonly SockTcp _sockTcp;
        public bool OpenTraceDlg { private get; set; }

        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();

        public RemoteConnect(SockTcp sockTcp) {
            _sockTcp = sockTcp;
        }

        //クライアント側への送信
        public void AddTrace(TraceKind traceKind, string str, Ip ip) {
            if (!OpenTraceDlg)
                return;
            var threadId = GetCurrentThreadId();
            var buffer = string.Format("{0}\b{1}\b{2}\b{3}", traceKind.ToString(), threadId.ToString(), ip, str);
            //トレース(S->C)
            RemoteData.Send(_sockTcp, RemoteDataKind.DatTrace, buffer);
        }
    }
}