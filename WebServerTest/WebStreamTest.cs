using NUnit.Framework;
using WebServer;

namespace WebServerTest{
    

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
        [TestCase(2560, 300)]
        public void AddTest(int block, int count) {
            var max = block*count;
            var ws = new WebStream(max);

            var dmy = new byte[block];
            for (var i = 0; i < block; i++){
                dmy[i] = (byte) i;
            }
            for (var i = 0; i < count; i++){
                ws.Add(dmy);
            }

            var buf = new byte[block];
            for (var i = 0; i < count; i++){
                var len = ws.Read(buf, 0, buf.Length);
                Assert.AreEqual(len, block);
                Assert.AreEqual(buf[0], 0);
                Assert.AreEqual(buf[1], 1);
                Assert.AreEqual(buf[2], 2);
            }
            ws.Dispose();
        }

        [TestCase(2560,300)]//当初メモリで動作し、途中でディクスに変更される
        [TestCase(2560,3)]//メモリで動作
        public void DynamicTest(int block, int count) {
            var ws = new WebStream(-1);//limitを指定しないで-1でダイナミックに初期化する

            var dmy = new byte[block];
            for (var i = 0; i < block; i++){
                dmy[i] = (byte) i;
            }
            for (var i = 0; i < count; i++){
                ws.Add(dmy);
            }

            var buf = new byte[block];
            for (var i = 0; i < count; i++){
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

