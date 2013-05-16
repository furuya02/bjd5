using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{

    public class RrQueryTest{

        [Test]
        public void GetDnsTypeの確認(){
            //setUp
            var expected = DnsType.A;
            var sut = new RrQuery("aaa.com", expected);
            //exercise
            var actual = sut.DnsType;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ToStringの確認(){
            //setUp
            var expected = "Query A aaa.com";
            var sut = new RrQuery("aaa.com", DnsType.A);
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}