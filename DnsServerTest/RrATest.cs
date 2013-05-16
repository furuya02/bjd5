using Bjd.net;
using BjdTest.test;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    public class RrATest{

        //type= 0x0001(A) class=0x0001 ttl=0x00000e10 dlen=0x0004 data=3b6a1bd0
        private const string Str0 = "0001000100000e1000043b6a1bd0";

        [Test]
        public void GetIpの確認(){
            //setUp
            var expected = new Ip("127.0.0.1");
            var sut = new RrA("aaa.com", 0, expected);
            //exercise
            var actual = sut.Ip;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void バイナリ初期化との比較(){
            //setUp
            var sut = new RrA("aaa.com", 64800, new Ip("1.2.3.4"));
            var expected = (new RrA("aaa.com", 64800, new byte[]{1, 2, 3, 4})).ToString();
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 実パケット生成したオブジェクトとの比較(){
            //setUp
            var sut = new RrA("aaa.com", 0x00000e10, new Ip("59.106.27.208"));
            var rr = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
            var expected = (new RrA("aaa.com", rr.Ttl, rr.Data)).ToString();
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ToStringの確認(){
            //setUp
            var expected = "A aaa.com TTL=0 127.0.0.1";
            var sut = new RrA("aaa.com", 0, new Ip("127.0.0.1"));
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}