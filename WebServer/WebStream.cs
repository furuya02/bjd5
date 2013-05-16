using System;
using System.IO;

namespace WebServer {
    class WebStream : IDisposable {
        WebStreamDisk _disk;
        WebStreamMemory _memory;
        private readonly int _limit;

        public long Length{
            get{
                if (_disk != null)
                    return _disk.Length;
                return _memory.Length;
            }
        } //long(約2Gbyteまで対応）

        //最終的なサイズが分かっている場合は、limit(分からない場合は-1)を指定する
        public WebStream(int limit) {
            _limit = limit;

            if (_limit > 256000) {//サイズが大きい場合は、ファイルで保持する
                _disk = new WebStreamDisk();
                _memory = null;
            } else {//リミットが分からないときは、とりあえず256KByteで初期化する
                _disk = null;
                _memory = new WebStreamMemory((_limit < 0) ? 256000 : _limit);
            }
        }

        public void Dispose() {
            if (_disk != null) {
                _disk.Dispose();
            } else {
                _memory.Dispose();
            }
            GC.Collect();
        }

        public int Read(byte[] buffer, int offset, int count) {
            if (_disk != null)
                return _disk.Read(buffer, offset, count);
            return _memory.Read(buffer, offset, count);
        }
        public byte[] GetBytes(){
            if (_disk != null)
                return _disk.GetBytes();
            return _memory.GetBytes();
        }
        public bool Add(byte[] b){
            if (b == null)
                return false;
            return Add2(b, 0, b.Length);
        }

        public bool Add2(byte[] b,int offset,int length) {
            //b==nullの場合は、下位クラスに向かう前に、ここではじく
            if (b == null)
                return false;

            if (_disk != null)
                return _disk.Add(b,offset,length);
            
            if(_limit==-1 || (_memory.Length + length)>=_limit ){
                //暫定の初期化でサイズをオーバした場合
                var buf = new byte[_memory.Length];
                _memory.Read(buf, 0, _memory.Length);
                _memory.Dispose();
                _memory = null;
                //ディスクに変更
                _disk = new WebStreamDisk();
                _disk.Add(buf,0,buf.Length);
                return _disk.Add(b,offset,length);
            }
            return _memory.Add(b,offset,length);
        }

        class WebStreamMemory : IDisposable {
            readonly byte[] _buf;
            public int Length { get; private set; } //使用できる上限が決まっているのでintで対応できる
            int _pos;//使用できる上限が決まっているのでintで対応できる

            public WebStreamMemory(int limit){
                _pos = 0;
                _buf = new byte[limit];

            }
            public void Dispose() {
            }
            public bool Add(byte[] b,int offset,int length) {
                if (Length + length > _buf.Length)
                    return false;

                Buffer.BlockCopy(b, offset, _buf, Length, length);
                Length += length;

                return true;
            }
            public int Read(byte[] buffer, int offset, int count) {
                //mode==メモリの場合は、Posをintにキャストしても問題ない
                var len = _buf.Length - _pos; //残りのサイズ
                if (len > count) {
                    len = count; //残りのサイズが読み出しサイズより大きい場合
                }
                Buffer.BlockCopy(_buf, _pos, buffer, offset, len);
                _pos += len;
                return len;
            }
            public byte [] GetBytes(){
                var b = new byte[Length];
                Buffer.BlockCopy(_buf,0,b,0,Length);
                return b;
            }
        }
        class WebStreamDisk : IDisposable {
            public long Length { get; private set; } //long(約2Gbyteまで対応）
            long _pos; //long(約2Gbyteまで対応）
            private readonly FileStream _fs;
            private readonly string _fileName;
            public WebStreamDisk(){
                _pos = 0;
                _fileName = string.Format("{0}", Path.GetTempFileName());
                _fs = new FileStream(_fileName, FileMode.Create, FileAccess.ReadWrite);
            }
            public void Dispose() {
                _fs.Close();
                File.Delete(_fileName);
            }
            public bool Add(byte[] b, int offset, int length) {
                _fs.Write(b, offset, length);
                Length += length;
                return true;
            }
            public int Read(byte[] buffer, int offset, int count) {
                _fs.Seek(_pos, SeekOrigin.Begin); //ファイルの先頭にシークする
                var len = _fs.Length - _pos; //残りのサイズ(long)
                if (len > count) {
                    len = count; //残りのサイズが読み出しサイズより大きい場合
                }
                if (len > 6553500) {
                    len = 6553500; //intにキャストできるようにサイズを制限
                }
                _pos += len;
                return _fs.Read(buffer, offset, (int)len);
            }
            public byte[] GetBytes() {
                _fs.Seek(0, SeekOrigin.Begin); //ファイルの先頭にシークする
                var len = _fs.Length;//long
                if (len > Int32.MaxValue) {
                    len = Int32.MaxValue; //intにキャストできるようにサイズを制限
                }
                var b = new byte[len];
                _fs.Read(b,0,(int)len);
                return b;
            }
        }
    }
}
