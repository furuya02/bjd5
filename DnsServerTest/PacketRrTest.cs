using System;
using BjdTest.test;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    public class PacketRRTest{

        //type= 0x0002 class=0x0001 ttl=0x00011e86 dlen=0x0006 data=036e7332c00c
        private const string Str0 = "0002000100011e860006036e7332c00c";


        [Test]
        public void GetClsの確認(){
            //setUp
            var sut = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
            ushort expected = 0x0001;
            //exercise
            var actual = sut.Cls;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void GetTypeの確認(){
            //setUp
            var sut = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
            var expected = DnsType.Ns;
            //exercise
            var actual = sut.DnsType;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void GetTtlの確認(){
            //setUp
            var sut = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
            uint expected = 0x11E86; //733350
            //exercise
            var actual = sut.Ttl;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void GetDLenの確認(){
            //setUp
            var sut = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
           ushort expected = 6;
            //exercise
            var actual = sut.DLen;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void GetData確認(){
            //setUp
            var sut = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
            var expected = new byte[6];
            Buffer.BlockCopy(TestUtil.HexStream2Bytes(Str0), 10, expected, 0, 6);
            //exercise
            var actual = sut.Data;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SetClsの確認(){
            //setUp
            var sut = new PacketRr(0);

            ushort expected = 0x0002;
            sut.Cls = expected;

            //exercise
            var actual = sut.Cls;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SetTypeの確認(){
            //setUp
            var sut = new PacketRr(0);
            const DnsType expected = DnsType.Mx;
            sut.DnsType = expected;

            //exercise
            var actual = sut.DnsType;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}