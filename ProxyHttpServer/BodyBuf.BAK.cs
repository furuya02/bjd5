using System;

namespace ProxyHttpServer {
    class BodyBuf {
        byte [] buf;
        long threwLength = 0;//切り捨てたサイズ
        public bool CanUse{ get; private set;}//キャッシュや、制限文字列で使用可能かどうかのフラグ
        int max;//指定サイズ以上のバッファリングをしない
        public BodyBuf(int max) {
            this.max = max;
            Set(new byte[0]);
        }
           public long Length {
            get {
                return buf.Length + threwLength;
            }
        }
        public void Add(byte[] b) {
            if (buf.Length != 0) {
                //var tmp =  new byte[buf.Length];
                //Buffer.BlockCopy(buf, 0, tmp, 0, buf.Length);
                var tmp = buf;
                buf = new byte[tmp.Length + b.Length];
                Buffer.BlockCopy(tmp, 0, buf, 0, tmp.Length);
                Buffer.BlockCopy(b, 0, buf, tmp.Length, b.Length);
            } else {
                buf = b;
            }
        }
        public void Set(byte[] b) {
            //初期化されたのと同じなので、CanUseも初期化される
            buf = b;
            CanUse = true;
            threwLength = 0;
        }
        public byte[] Get() {
            if (CanUse) {
                return buf;
            }
            return null;
        }
        public byte[] SendBuf(int start) {
            if (start < 0) {
                CanUse = false;
            }

            if (CanUse) {
                int len = buf.Length - start;
                
                if (len == 0)
                    return null;//これ以上データは無い

                var b = new byte[len];
                Buffer.BlockCopy(buf, start, b, 0, len);

                if (buf.Length > max) {
                    CanUse = false;

                    threwLength += buf.Length;//サイズ保存
                    buf = new byte[0];//現在のバッファを捨てる
                }
                return b;
            } else {
                //start が<0の時、intをオーバーしているので条件判断しない
                if (start < 0) {
                    int x = 10;
                }
                if (start == threwLength || start<0) {

                    var b = new byte[buf.Length];
                    Buffer.BlockCopy(buf, 0, b, 0, buf.Length);

                    threwLength += buf.Length;//サイズ保存
                    buf = new byte[0];//現在のバッファを捨てる

                    return b;
                } else {
                    return null;
                }
            }
        }
    }
}
