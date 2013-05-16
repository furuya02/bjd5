using Bjd.packet;
using NUnit.Framework;

namespace BjdTest.packet{


    public class PacketTest{

        const int Max = 100;

        private class MyPacket : Packet{

            public MyPacket() : base(new byte[Max], 0){

            }

            public override byte[] GetBytes(){
                return GetBytes(0,Max);
            }

            public override int Length(){
                return Max;
            }

        }

        [Test]
        public void SetShortで値を設定してgetShortで取得する(){
            //setUp
            var sut = new MyPacket();
            //short expected = (short) 0xff01;
            const ushort expected = 0x1f01;
            sut.SetUShort(expected, 20);
            //exercise
            var actual = sut.GetUShort(20);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SetIntで値を設定してgetIntで取得する(){
            //setUp
            var sut = new MyPacket();
            const int expected = 12345678;
            sut.SetUInt(expected, 20);
            //exercise
            var actual = sut.GetUInt(20);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void SetByteで値を設定してgetByteで取得する(){
            //setUp
            var sut = new MyPacket();
            const byte expected = (byte) 0xfd;
            sut.SetByte(expected, 20);
            //exercise
            var actual = sut.GetByte(20,1);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SetLongで値を設定してgetLongで取得する(){
            //setUp
            var sut = new MyPacket();
            const ulong expected = (long) 3333;
            sut.SetULong(expected, 20);
            //exercise
            var actual = sut.GetULong(20);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SetBytesで値を設定してgetBytesで取得する(){
            //setUp
            var sut = new MyPacket();

            var expected = new byte[Max - 20];
            for (var i = 0; i < Max - 20; i++){
                expected[i] = (byte) i;
            }
            sut.SetBytes(expected, 20);
            //exercise
            var actual = sut.GetBytes(20, Max - 20);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}