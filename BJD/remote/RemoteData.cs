using System;
using System.Text;
using System.Threading;
using Bjd.log;
using Bjd.sock;
using Bjd.util;

namespace Bjd.remote {
    public static class RemoteData {
        //送信
        public static bool Send(SockTcp sockTcp, RemoteDataKind kind, string str) {
            var o = new OneRemoteData(kind, str);
            return o.Send(sockTcp);
        }

        //受信（無効な場合 return null)
        public static OneRemoteData Recv(SockTcp sockTcp, ILife iLife) {
            if (sockTcp != null) {
                //Ver5.8.6
                var sec = 10;//最初はタイムアウト値を最小に設定する
                var b = sockTcp.Recv(1, sec, iLife);//REMOTE_DATA_KINDの受信
                if (b != null && b.Length == 1) {
                    var kind = (RemoteDataKind)b[0];

                    //これ以降は、データが到着しているはずなので、タイムアウト値を上げて待機する
                    //timeout = 3000;
                    //Ver5.8.6
                    sec = 10;
                    Thread.Sleep(1);
                    b = sockTcp.Recv(4, sec, iLife);//データサイズの受信
                    
                    if (b != null && b.Length == 4) {
                        var len = BitConverter.ToInt32(b, 0);
                        if (len == 0) {
                            return new OneRemoteData(kind, "");//データ本体はサイズ0
                        }
                        //Ver5.8.6
                        b = new byte[0];
                        while (iLife.IsLife()) {
                            Thread.Sleep(1);
                            var buf = sockTcp.Recv(len, sec, iLife);//データ本体の受信
                            if (buf == null) {
                                return null;
                            }
                            b = Bytes.Create(b, buf);
                            if (b.Length == len){
                                return new OneRemoteData(kind, Encoding.GetEncoding(932).GetString(b));
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
