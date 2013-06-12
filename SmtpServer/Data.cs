using System;
using System.Collections.Generic;
using Bjd;
using Bjd.mail;
using Bjd.sock;

namespace SmtpServer{
    internal enum RecvStatus{
        Success = 0,
        Disconnect = 1,
        LimitOver = 2,
        TimeOut = 3
    }

    class Data{

        private readonly long _sizeLimit;
        private readonly Mail _mail;
    
        public Data(Mail mail,long sizeLimit) {
            _mail = mail;
            _sizeLimit = sizeLimit;
        }
        
        public RecvStatus Recv(SockTcp sockTcp,int sec,ILife iLife) {
            var lines = new List<byte[]>();
            var dtLast = DateTime.Now;//受信が20秒無かった場合は、処理を中断する
            long linesSize = 0;//受信バッファのデータ量（受信サイズ制限に使用する）
            var keep = new byte[0];
            
            while (iLife.IsLife()) {
                if (dtLast.AddSeconds(sec) < DateTime.Now){
                    return RecvStatus.TimeOut;
                }
                var len = sockTcp.Length();
                if (len == 0)
                    continue;
                var buf = sockTcp.Recv(len, sec,iLife);
                if (buf == null) {//切断された
                    return RecvStatus.Disconnect;
                    
                }
                dtLast = DateTime.Now;
                linesSize += buf.Length;//受信データ量

                //受信サイズ制限
                if (_sizeLimit != 0) {
                    if (_sizeLimit < linesSize / 1024) {
                        return RecvStatus.LimitOver;
                    }
                }

                //繰越がある場合
                if (keep.Length != 0) {
                    var tmp = new byte[buf.Length + keep.Length];
                    Buffer.BlockCopy(keep, 0, tmp, 0, keep.Length);
                    Buffer.BlockCopy(buf, 0, tmp, keep.Length, buf.Length);
                    buf = tmp;
                    keep = new byte[0];
                }

                int start = 0;
                for (int end = 0; ; end++) {
                    if (buf[end] == '\n') {
                        if (1 <= end && buf[end - 1] == '\r') {
                            var tmp = new byte[end - start + 1];//\r\nを削除しない
                            Buffer.BlockCopy(buf, start, tmp, 0, end - start + 1);//\r\nを削除しない
                            lines.Add(tmp);
                            start = end + 1;
                        }
                    }
                    if (end >= buf.Length - 1) {
                        if (0 < (end - start + 1)) {
                            //改行が検出されていないので、繰越す
                            keep = new byte[end - start + 1];
                            Buffer.BlockCopy(buf, start, keep, 0, end - start + 1);
                        }
                        break;
                    }
                }
                //データ終了
                if (lines.Count >= 1 && lines[lines.Count - 1].Length >= 3) {
                    if (lines[lines.Count - 1][0] == '.' && lines[lines.Count - 1][1] == '\r' && lines[lines.Count - 1][2] == '\n') {
                        lines.RemoveAt(lines.Count - 1);//最終行の「.\r\n」は、破棄する
                        foreach (byte[] line in lines){
                            _mail.Init(line);
                        }
                        return RecvStatus.Success;
                    }
                }
            }
            return RecvStatus.Disconnect;
        }
   

    }
}
