using System;
using System.IO;
using NUnit.Framework;

namespace WebServerTest{
    internal class WebStream : IDisposable{
        readonly WebStreamDisk _disk=null;
        readonly WebStreamMemory _memory = null;

        //最終的なサイズが分かっている場合は、limit(分からない場合は-1)を指定する
        public WebStream(int limit){
            if (limit == -1 || limit > 256000){
                //不明(-1)若しくは、サイズが大きい場合は、ファイルで保持する
                _disk = new WebStreamDisk();
            }else{
                _memory = new WebStreamMemory(limit); 
            }
        }

        public void Dispose(){
            if (_disk != null){
                _disk.Dispose();
            }else{
                _memory.Dispose();               
            }
        }
        
        public int Read(byte[] buffer, int offset, int count){
            if (_disk != null)
                return _disk.Read(buffer, offset, count);
            return _memory.Read(buffer, offset, count);
        }

        public bool Add(byte[] b){
            if (_disk != null)
                return _disk.Add(b);
            return _memory.Add(b);
        }

        class WebStreamMemory : IDisposable {
            readonly byte[] _buf;
            int _length = 0; //使用できる上限が決まっているのでintで対応できる
            int _pos = 0;//使用できる上限が決まっているのでintで対応できる

            public WebStreamMemory(int limit){
                _buf = new byte[limit];
                
            }
            public void Dispose(){
            }
            public bool Add(byte[] b) {
                if (b == null) {
                    return false;
                }
                if (_length + b.Length > _buf.Length)
                        return false;

                Buffer.BlockCopy(b, 0, _buf, _length, b.Length);
                _length += b.Length;

                return true;
            }
            public int Read(byte[] buffer, int offset, int count){
                //mode==メモリの場合は、Posをintにキャストしても問題ない
                int len = _buf.Length - _pos; //残りのサイズ
                if (len > count){
                    len = count; //残りのサイズが読み出しサイズより大きい場合
                }
                Buffer.BlockCopy(_buf, _pos, buffer, offset, len);
                _pos += len;
                return len;
            }
        }
        class WebStreamDisk : IDisposable {
            long _length = 0; //long(約2Gbyteまで対応）
            long _pos = 0; //long(約2Gbyteまで対応）
            private readonly FileStream _fs;
            private readonly string _fileName;
            public WebStreamDisk() {
                _fileName = string.Format("{0}", Path.GetTempFileName());
                _fs = new FileStream(_fileName, FileMode.Create, FileAccess.ReadWrite);
            }
            public void Dispose(){
                _fs.Close();
                File.Delete(_fileName);
            }
            public bool Add(byte[] b) {
                if (b == null) {
                    return false;
                }
                _fs.Write(b, 0, b.Length);
                _length += b.Length;
                return true;
            }
            public int Read(byte[] buffer, int offset, int count) {
                _fs.Seek(_pos, SeekOrigin.Begin); //ファイルの先頭にシークする
                long len = _fs.Length - _pos; //残りのサイズ
                if (len > count) {
                    len = count; //残りのサイズが読み出しサイズより大きい場合
                }
                if (len > 6553500) {
                    len = 6553500; //intにキャストできるようにサイズを制限
                }
                _pos += len;
                return _fs.Read(buffer, offset, (int)len);
            }
        }
    }

    [TestFixture]
    internal class WebStreamTest{


        [SetUp]
        public void SetUp(){
        }

        [TearDown]
        public void TearDown(){
        }

        [TestCase(2560000, 1)] //ディスク保存の試験
        [TestCase(2560000, 100)] //ディスク保存の試験
        [TestCase(256000, 1)] //メモリ保存の試験
        [TestCase(1000000, 100)] //1で1Mbyte
        [TestCase(25600, 10)]
        public void AddTest(int block, int count) {
            var max = block*count;
            var ws = new WebStream(max);

            var dmy = new byte[block];
            for (int i = 0; i < block; i++){
                dmy[i] = (byte) i;
            }
            for (int i = 0; i < count; i++){
                ws.Add(dmy);
            }

            var buf = new byte[block];
            for (int i = 0; i < count; i++){
                var len = ws.Read(buf, 0, buf.Length);
                Assert.AreEqual(len, block);
                Assert.AreEqual(buf[0], 0);
                Assert.AreEqual(buf[1], 1);
                Assert.AreEqual(buf[2], 2);
            }
            ws.Dispose();
        }

        [TestCase(25600,10)]
        public void DynamicTest(int block, int count){
            var max = block*count;
            var ws = new WebStream(-1);//limitを指定しないで-1でダイナミックに初期化する

            var dmy = new byte[block];
            for (int i = 0; i < block; i++){
                dmy[i] = (byte) i;
            }
            for (int i = 0; i < count; i++){
                ws.Add(dmy);
            }

            var buf = new byte[block];
            for (int i = 0; i < count; i++){
                var len = ws.Read(buf, 0, buf.Length);
                Assert.AreEqual(len, block);
                Assert.AreEqual(buf[0], 0);
                Assert.AreEqual(buf[1], 1);
                Assert.AreEqual(buf[2], 2);
            }
            ws.Dispose();
        }
    }
}

