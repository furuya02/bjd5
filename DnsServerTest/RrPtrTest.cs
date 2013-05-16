using BjdTest.test;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    public class RrPtrTest{

        //PTR class=1 ttl=0x000000e10 localhost
        private const string Str0 = "000c000100000e10000b096c6f63616c686f737400";

        [Test]
        public void GetPtrの確認(){
            //setUp
            var expected = "www.aaa.com.";
            var sut = new RrPtr("1.0.0.127.in-addr.arpa.", 0, expected);
            //exercise
            var actual = sut.Ptr;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void バイナリ初期化との比較(){
            //setUp
            var sut = new RrPtr("1.0.0.127.in-addr.arpa.", 64800, "1.");
            var expected = (new RrPtr("1.0.0.127.in-addr.arpa.", 64800, new byte[]{01, 49, 0})).ToString();
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 実パケット生成したオブジェクトとの比較(){
            //setUp
            var sut = new RrPtr("1.0.0.127.in-addr.arpa.", 0x00000e10, "localhost");
            var rr = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
            var expected = (new RrPtr("1.0.0.127.in-addr.arpa.", rr.Ttl, rr.Data)).ToString();
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ToStringの確認(){
            //setUp
            var expected = "Ptr 1.0.0.127.in-addr.arpa. TTL=0 www.aaa.com.";
            var sut = new RrPtr("1.0.0.127.in-addr.arpa.", 0, "www.aaa.com.");
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}