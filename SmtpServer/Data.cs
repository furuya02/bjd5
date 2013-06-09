using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.sock;

namespace SmtpServer {
     class Data{

        private readonly long _sizeLimit;
        private readonly bool _useCheckFrom;
        private readonly List<byte[]> _lines;
        private readonly Logger _logger;
     
        public Mail Mail { get; private set; }
    
        public Data(long sizeLimit,bool useCheckFrom,Logger logger) {
            _sizeLimit = sizeLimit;
            _useCheckFrom = useCheckFrom;
            _logger = logger;

            _lines = new List<byte[]>();
            Mail = new Mail(_logger);
        }
        /*
        public bool Job(SockTcp sockTcp,int sec,ILife ilife){
            if (RecvStatus.Success != Recv(sockTcp,sec,ilife)){
                return false;
            }
            //受信が有効な場合
            foreach (byte[] line in _lines) {
                if (Mail.Init(line)) {
                    //ヘッダ終了時の処理
                    //Ver5.0.0-b8 Frmo:偽造の拒否
                    if (_useCheckFrom) {
                        var mailAddress = new MailAddress(Mail.GetHeader("From"));
                        //Ver5.4.3
                        if (mailAddress.User == "") {
                            if (_logger != null){
                                _logger.Set(LogKind.Secure, sockTcp, 52, string.Format("From:{0}", mailAddress));
                            }
                            sockTcp.AsciiSend("530 There is not an email address in a local user");
                            session.SetMode(SessionMode.Command);
                            break;
                        }

                        //ローカルドメインでない場合は拒否する
                        if (!mailAddress.IsLocal(DomainList)) {
                            if (_logger != null){
                                _logger.Set(LogKind.Secure, sockTcp, 28, string.Format("From:{0}", mailAddress));
                            }
                            sockTcp.AsciiSend("530 There is not an email address in a local domain");
                            session.SetMode(SessionMode.Command);
                            break;
                        }
                        //有効なユーザでない場合拒否する
                        if (!Kernel.MailBox.IsUser(mailAddress.User)) {
                            if (_logger != null){
                                _logger.Set(LogKind.Secure, sockTcp, 29, string.Format("From:{0}", mailAddress));
                           }
                            sockTcp.AsciiSend("530 There is not an email address in a local user");
                            session.SetMode(SessionMode.Command);
                            break;
                        }
                    }
                }
                //テンポラリバッファの内容でMailオブジェクトを生成する
                bool error = false;

                //ヘッダの変換及び追加
                _changeHeader.Exec(Mail,_logger);

                foreach (MailAddress to in session.RcptList) {
                    if (!MailSave(session.From, to, session.Mail, sockTcp.RemoteHostname, sockTcp.RemoteIp)) {//MLとそれ以外を振り分けて保存する
                        error = true;
                        break;
                    }
                }

                sockTcp.AsciiSend(error ? "554 MailBox Error" : "250 OK");
                session.SetMode(SessionMode.Command);
                // DATAコマンドでメールを受け取った時点でRCPTリストクリアする
                session.RcptList.Clear();
                continue;
            }            
        }
        */
        RecvStatus Recv(SockTcp sockTcp,int sec,ILife iLife) {
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
                        if (_logger != null){
                            _logger.Set(LogKind.Secure, sockTcp, 7, string.Format("Limit:{0}KByte", _sizeLimit));
                        }
                        sockTcp.AsciiSend("552 Requested mail action aborted: exceeded storage allocation");
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
                            _lines.Add(tmp);
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
                if (_lines.Count >= 1 && _lines[_lines.Count - 1].Length >= 3) {
                    if (_lines[_lines.Count - 1][0] == '.' && _lines[_lines.Count - 1][1] == '\r' && _lines[_lines.Count - 1][2] == '\n') {
                        _lines.RemoveAt(_lines.Count - 1);//最終行の「.\r\n」は、破棄する
                        return RecvStatus.Success;
                    }
                }
            }
            return RecvStatus.Disconnect;
        }
        enum RecvStatus {
            Success = 0,
            Disconnect = 1,
            LimitOver = 2,
            TimeOut = 3
        }
   

    }
}
