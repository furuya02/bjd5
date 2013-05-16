using System;
using System.Text;
using Bjd.net;
using Bjd.remote;
using Bjd.sock;

namespace Bjd {
    public class OneRemoteData {
        public string Str { get; private set; }
        public RemoteDataKind Kind { get; private set; }
        public OneRemoteData(RemoteDataKind kind, string str) {
            Kind = kind;
            Str = str;
        }

        public bool Send(SockTcp sockTcp) {
            if (sockTcp != null) {
                //1.REMOTE_DATA_KINDの送信(トレースなし)
                var b = new[] { (byte)Kind };
                sockTcp.SendNoTrace(b);

                //データのバイナリ化
                var data = Encoding.GetEncoding(932).GetBytes(Str);

                //2.データサイズの送信(トレースなし)
                b = BitConverter.GetBytes(data.Length == 0 ? 0 : data.Length);
                sockTcp.SendNoTrace(b);

                //3.データ本体の送信(トレースなし)
                if (data.Length != 0)
                    sockTcp.SendNoTrace(data);
                return true;
            }
            return false;
        }
    }
}