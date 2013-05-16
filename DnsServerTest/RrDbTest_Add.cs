using Bjd.net;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    public class RrDbTest_add{

        [Test]
        public void 新規のリソース追加は成功する(){
            //setUp
            var sut = new RrDb();
            var expected = true; //成功
            //exercise
            var actual = sut.Add(new RrA("domain", 100, new Ip("1.2.3.4")));
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 同一リソースの追加_TTLが0の場合は失敗する(){
            //setUp
            var sut = new RrDb();
            var expected = false; //失敗
            //exercise
            var ttl = 0u; //最初のリソースはTTL=0
            sut.Add(new RrA("domain", ttl, new Ip("1.2.3.4")));
            var actual = sut.Add(new RrA("domain", 100, new Ip("1.2.3.4")));
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 同一リソースの追加_TTLが0以外の場合は上書きされる(){
            //setUp
            var sut = new RrDb();
            //exercise
            var ttl = 10u; //最初のリソースはTTL=0以外
            sut.Add(new RrA("domain", ttl, new Ip("1.2.3.4")));
            sut.Add(new RrA("domain", 20, new Ip("1.2.3.4")));
            //verify
            Assert.That(RrDbTest.Size(sut), Is.EqualTo(1)); //件数は１件になる
            Assert.That(RrDbTest.Get(sut, 0).Ttl, Is.EqualTo(20)); //TTLは後から追加した20になる
        }

        [Test]
        public void 異なるリソースの追加(){
            //setUp
            var sut = new RrDb();
            var expected = 3; //全部で3件になる
            //exercise
            sut.Add(new RrA("domain", 10, new Ip("1.2.3.4")));
            sut.Add(new RrA("domain", 10, new Ip("3.4.5.6")));
            sut.Add(new RrNs("domain", 10, "ns"));
            var actual = RrDbTest.Size(sut);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}