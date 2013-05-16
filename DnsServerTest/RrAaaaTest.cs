using Bjd.net;
using BjdTest.test;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    public class RrAaaaTest{

        //type= 0x0001(A) class=0x0001 ttl=0x00000e10 dlen=0x0004 data=3b6a1bd0
        private const string Str0 = "001c0001000151800010200102000dfffff102163efffeb144d7";

        [Test]
        public void GetIpの確認(){
            //setUp
            var expected = new Ip("2001:200:dff:fff1:216:3eff:feb1:44d7");
            var sut = new RrAaaa("www.com", 0, expected);
            //exercise
            var actual = sut.Ip;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void バイナリ初期化との比較(){
            //setUp
            var sut = new RrAaaa("aaa.com", 64800, new Ip("::1"));
            var expected = (new RrAaaa("aaa.com", 64800, new byte[]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1})).ToString();
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 実パケット生成したオブジェクトとの比較(){
            //setUp
            var sut = new RrAaaa("orange.kame.net", 0x00015180, new Ip("2001:200:dff:fff1:216:3eff:feb1:44d7"));
            var rr = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
            var expected = (new RrAaaa("orange.kame.net", rr.Ttl, rr.Data)).ToString();
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ToStringの確認(){
            //setUp
            var expected = "Aaaa www.com TTL=100 ::1";
            var sut = new RrAaaa("www.com", 100, new Ip("::1"));
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
