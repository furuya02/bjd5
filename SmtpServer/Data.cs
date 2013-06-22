using System;
using System.Collections.Generic;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.sock;

namespace SmtpServer{
    //テストのためだけに公開されている
    internal enum RecvStatus {
        Continue=0,
        Finish=1,
        Limit=2,
    }

    class Data : IDisposable{

        private readonly long _sizeLimit;//Kbyte
        public Mail Mail { get; private set; }

        readonly List<byte[]> _lines = new List<byte[]>();
        long _len;//受信バッファのデータ量（受信サイズ制限に使用する）
        byte[] _keep = new byte[0];
    
        public Data(long sizeLimit) {
            _sizeLimit = sizeLimit;
            Mail = new Mail();
        }

        public void Dispose() {
            Mail.Dispose();
            Mail = null;
        }

        
        //テストのためだけに公開されている
        public RecvStatus Append(byte[] buf) {
            _len += buf.Length;//受信データ量

            //受信サイズ制限
            if (_sizeLimit != 0) {
                if (_sizeLimit <= _len / 1024) {
                    return RecvStatus.Limit;
                }
            }

            //繰越がある場合
            if (_keep.Length != 0) {
                var tmp = new byte[buf.Length + _keep.Length];
                Buffer.BlockCopy(_keep, 0, tmp, 0, _keep.Length);
                Buffer.BlockCopy(buf, 0, tmp, _keep.Length, buf.Length);
                buf = tmp;
                _keep = new byte[0];
            }

            var start = 0;
            for (var end = 0; ; end++) {
                if (buf[end] == '\n') {
                    if (1 <= end && buf[end - 1] == '\r') {
                        var tmp = new byte[end - start + 1];//\r\nを削除しない
                        Buffer.BlockCopy(buf, start, tmp, 0, end - start + 1);//\r\nを削除しない
                        if (tmp.Length == 3){
                            //.<CR><LF>
                            if (tmp[0] == '.' && tmp[1] == '\r' && tmp[2] == '\n'){
                                foreach (byte[] line in _lines) {
                                    Mail.AppendLine(line);
                                }
                                return RecvStatus.Finish;
                            }
                            
                        }
                        //ドットで始まる行の先頭のドットは削除する
                        if (tmp[0] == '.'){
                            var dmy = new byte[tmp.Length - 1];
                            Buffer.BlockCopy(tmp, 1, dmy, 0, dmy.Length);
                            _lines.Add(dmy);
                        } else{
                            _lines.Add(tmp);
                        }
                        start = end + 1;
                    }
                }
                if (end >= buf.Length - 1) {
                    if (0 < (end - start + 1)) {
                        //改行が検出されていないので、繰越す
                        _keep = new byte[end - start + 1];
                        Buffer.BlockCopy(buf, start, _keep, 0, end - start + 1);
                    }
                    break;
                }
            }
            //データ終了
//            if (_lines.Count >= 1 && _lines[_lines.Count - 1].Length >= 3) {
//                if (_lines[_lines.Count - 1][0] == '.' && _lines[_lines.Count - 1][1] == '\r' && _lines[_lines.Count - 1][2] == '\n') {
//                    _lines.RemoveAt(_lines.Count - 1);//最終行の「.\r\n」は、破棄する
//                    foreach (byte[] line in _lines) {
//                        Mail.Init(line);
//                    }
//                    return RecvStatus.Finish;
//                }
//            }
            return RecvStatus.Continue;

        }

        //通常はこれを使用する
        public bool Recv(SockTcp sockTcp, int sec,Logger logger, ILife iLife){
            var dtLast = DateTime.Now; //受信が20秒無かった場合は、処理を中断する
            while (iLife.IsLife()){
                if (dtLast.AddSeconds(sec) < DateTime.Now){
                    return false; //タイムアウト
                }
                var len = sockTcp.Length();
                if (len == 0){
                    continue;
                }
                var buf = sockTcp.Recv(len, sec, iLife);
                if (buf == null){
                    return false; //切断された
                }
                dtLast = DateTime.Now;

                var recvStatus = Append(buf);

                if (recvStatus == RecvStatus.Limit){
                    //サイズ制限
                    if (logger != null){
                        logger.Set(LogKind.Secure, sockTcp, 7, string.Format("Limit:{0}KByte", _sizeLimit));
                    }
                    sockTcp.AsciiSend("552 Requested mail action aborted: exceeded storage allocation");
                    return false;
                }
                if (recvStatus == RecvStatus.Finish){
                    return true;
                }
            }
            return false;
        }

    }
}
