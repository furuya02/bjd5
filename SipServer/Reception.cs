using System.Collections.Generic;
using Bjd;
using Bjd.util;

namespace SipServer {
    class Reception {
        //解釈が失敗した場合は、StartLine.ReceptionKind がUnknownになっている
        public StartLine StartLine { get; private set; }
        public Header Header { get; private set; }
        public List<byte[]> Body { get; private set; }

        public Reception(byte[] buf) {

            //とりあえず、エラーを返せるように全てを初期化する
            Body = new List<byte[]>();
            Header = new Header();
            StartLine = new StartLine(null);

            var lines = Inet.GetLines(buf);
            if (lines.Count == 0){
                return; //スタートラインが存在しない
            }
            StartLine = new StartLine(lines[0]);
            if (StartLine.ReceptionKind == ReceptionKind.Unknown) {
                return; //スタートラインが無効な場合、これ以降の処理を行わない
            }
            //ヘッダ初期化
            var header = new List<byte[]>();
            var i = 1; //処理対象の行をカウントする
            for (; i < lines.Count; i++){
                if (lines[i].Length == 2 && lines[i][0] == '\r' && lines[i][1] == '\n'){
                    //改行のみの行
                    i++;
                    break;
                }
                header.Add(lines[i]);
            }
            //ヘッダ初期化
            Header = new Header(header);
            
            //ボディ初期化
            var contentLength = Header.GetVal("Content-Length");
            if (contentLength == "0")
                return;

            for (;i<lines.Count;i++) {
                Body.Add(lines[i]);
            }
        }
    }
}
