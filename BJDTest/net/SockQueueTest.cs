using Bjd.sock;
using NUnit.Framework;


namespace BjdTest.net{


    public class SockQueueTest{

        [Test]
        public void 生成時のlengthは0になる(){
            //setUp
            var sut = new SockQueue();
            const int expected = 0;
            //exercise
            var actual = sut.Length;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Lengthが0の時Dequeueで100バイト取得しても0バイトしか返らない(){
            //setUp
            var sut = new SockQueue();
            const int expected = 0;
            //exercise
            var actual = sut.Dequeue(100).Length;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Lengthが50の時Dequeueで100バイト取得しても50バイトしか返らない(){
            //setUp
            var sut = new SockQueue();
            sut.Enqueue(new byte[50], 50);
            const int expected = 50;
            //exercise
            var actual = sut.Dequeue(100).Length;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Lengthが200の時Dequeueで100バイト取得すると100バイト返る(){
            //setUp
            var sut = new SockQueue();
            sut.Enqueue(new byte[200], 200);
            const int expected = 100;
            //exercise
            var actual = sut.Dequeue(100).Length;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Lengthが200の時Dequeueで100バイト取得すると残りは100バイトになる(){
            //setUp
            var sut = new SockQueue();
            sut.Enqueue(new byte[200], 200);
            sut.Dequeue(100);
            const int expected = 100;
            //exercise
            var actual = sut.Length;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void EnqueueしたデータとDequeueしたデータの整合性を確認する(){
            //setUp
            var sut = new SockQueue();
            var expected = new byte[]{0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
            sut.Enqueue(expected, 10);
            //exercise
            var actual = sut.Dequeue(10);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Enqueueしたデータの一部をDequeueしたデータの整合性を確認する(){
            //setUp
            var sut = new SockQueue();
            var buf = new byte[]{0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
            sut.Enqueue(buf, 10);
            sut.Dequeue(5); //最初に5バイト取得
            var expected = new byte[]{5, 6, 7, 8, 9};
            //exercise
            var actual = sut.Dequeue(5);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void SockQueueスペース確認(){
            const int max = 2000000;

            var sockQueu = new SockQueue();

            var space = sockQueu.Space;
            //キューの空きサイズ
            Assert.That(space, Is.EqualTo(max));

            var buf = new byte[max - 100];
            sockQueu.Enqueue(buf, buf.Length);

            space = sockQueu.Space;
            //キューの空きサイズ
            Assert.That(space, Is.EqualTo(100));

            var len = sockQueu.Enqueue(buf, 200);
            //空きサイズを超えて格納すると失敗する(※0が返る)
            Assert.That(len, Is.EqualTo(0));

        }

        [Test]
        public void SockQueue行取得(){
            //int max = 1048560;

            var sockQueu = new SockQueue();

            var lines = new byte[]{0x61, 0x0d, 0x0a, 0x62, 0x0d, 0x0a, 0x63};
            sockQueu.Enqueue(lines, lines.Length);
            //2行と改行なしの1行で初期化

            var buf = sockQueu.DequeueLine();
            //sockQueue.dequeuLine()=\"1/r/n\" 1行目取得
            Assert.That(buf, Is.EqualTo(new byte[]{0x61, 0x0d, 0x0a}));

            //sockQueue.dequeuLine()=\"2/r/n\" 2行目取得 
            buf = sockQueu.DequeueLine();
            Assert.That(buf, Is.EqualTo(new byte[]{0x62, 0x0d, 0x0a}));

            buf = sockQueu.DequeueLine();
            //sockQueue.dequeuLine()=\"\" 3行目の取得は失敗する
            Assert.That(buf, Is.EqualTo(new byte[0]));

            lines = new byte[]{0x0d, 0x0a};
            sockQueu.Enqueue(lines, lines.Length);
            //"sockQueue.enqueu(/r/n) 改行のみ追加

            buf = sockQueu.DequeueLine();
            //sockQueue.dequeuLine()=\"3\" 3行目の取得に成功する"
            Assert.That(buf, Is.EqualTo(new byte[]{0x63, 0x0d, 0x0a}));
        }
    }
}