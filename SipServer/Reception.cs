using System.Collections.Generic;
using Bjd;
using Bjd.util;

namespace SipServer {
    class Reception {
        public StartLine StartLine { get; private set; }
        public Header Header { get; private set; }
        public List<byte[]> Body { get; private set; }

        public Reception(byte[] buf) {
            var lines = Inet.GetLines(buf);

            Body = new List<byte[]>();
            Header = new Header();
            
            StartLine = new StartLine(lines[0]);
            if (StartLine.ReceptionKind == ReceptionKind.Unknown) {
                return;//スタートラインが無効な場合、これ以降の処理を行わない
            }
            
            lines.RemoveAt(0);//スタートラインの行を削除

            //ヘッダ初期化
            Header = new Header(lines);
            
            //ボディ初期化
            var contentLength = Header.GetVal("Content-Length");
            var isBody = false;
            if (contentLength == "0")
                return;
            foreach (var l in lines) {
                if (l.Length == 2 && l[0] == '\r' && l[1] == '\n') {
                    isBody=true;
                    continue;
                }
                if(isBody) {
                    Body.Add(l);
                }
            }
        }
    }
}
