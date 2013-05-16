using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    public class RrDbTest_initLocalHost{

        [Test]
        public void 件数は４件になる(){
            //setUp
            var sut = new RrDb();
            var expected = 5;
            //exercise
            RrDbTest.InitLocalHost(sut);
            var actual = RrDbTest.Size(sut);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void リソース確認_1番目はAレコードとなる(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.InitLocalHost(sut);
            var o = (RrA) RrDbTest.Get(sut, 0);
            //verify
            Assert.That(o.DnsType, Is.EqualTo(DnsType.A));
            Assert.That(o.Name, Is.EqualTo("localhost."));
            Assert.That(o.Ip.ToString(), Is.EqualTo("127.0.0.1"));
        }

        [Test]
        public void リソース確認_2番目はPTRレコードとなる(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.InitLocalHost(sut);
            var o = (RrPtr) RrDbTest.Get(sut, 1);
            //verify
            Assert.That(o.DnsType, Is.EqualTo(DnsType.Ptr));
            Assert.That(o.Name, Is.EqualTo("1.0.0.127.in-addr.arpa."));
            Assert.That(o.Ptr, Is.EqualTo("localhost."));
        }

        [Test]
        public void リソース確認_3番目はAAAAレコードとなる(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.InitLocalHost(sut);
            var o = (RrAaaa) RrDbTest.Get(sut, 2);
            //verify
            //Assert.That(o.getDnsType(), Is.EqualTo(DnsType.Aaaa));
            //Assert.That(o.getName(), Is.EqualTo("localhost."));
            Assert.That(o.Ip.ToString(), Is.EqualTo("::1"));
        }

        [Test]
        public void リソース確認_4番目はPTRレコードとなる(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.InitLocalHost(sut);
            var o = (RrPtr) RrDbTest.Get(sut, 3);
            //verify
            Assert.That(o.DnsType, Is.EqualTo(DnsType.Ptr));
            Assert.That(o.Name, Is.EqualTo("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.IP6.ARPA."));
            Assert.That(o.Ptr, Is.EqualTo("localhost."));
        }

        [Test]
        public void リソース確認_5番目はNSレコードとなる(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.InitLocalHost(sut);
            var o = (RrNs) RrDbTest.Get(sut, 4);
            //verify
            Assert.That(o.DnsType, Is.EqualTo(DnsType.Ns));
            Assert.That(o.Name, Is.EqualTo("localhost."));
            Assert.That(o.NsName, Is.EqualTo("localhost."));
        }
    }
}
