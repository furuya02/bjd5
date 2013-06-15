using System.Linq;
using NUnit.Framework;
using Bjd;

namespace BjdTest {
    
    [TestFixture]
    class LookupTest {
        
        
        [SetUp]
        public void SetUp() {
        }
        
        [TearDown]
        public void TearDown() {
        }


        [Test]
        public void DnsServerTest() {
            var o = Lookup.DnsServer();
            Assert.AreNotEqual(o.Count,0);
            
            //デフォルトゲートウエイ確認 ※環境の違いを吸収
            if (o[0] != "192.168.0.254" && o[0] != "10.0.0.1" && o[0] != "192.168.1.1")
            {
                Assert.Fail();
            }
            //Assert.AreEqual(o[0],"192.168.0.254");//デフォルトゲートウエイ確認
        }

        
        [TestCase("www.sapporoworks.ne.jp", "59.106.27.208")]
        [TestCase("yahoo.co.jp", "124.83.187.140")]
        [TestCase("yahoo.co.jp", "203.216.243.240")]
        public void QueryATest(string target, string ipStr) {
            var o = Lookup.QueryA(target);
            Assert.AreNotEqual(o.Count,0);
            if (o.Any(s => s == ipStr)){
                return;
            }
            Assert.AreEqual(ipStr,o);
        }
        [TestCase("google.com", "aspmx.l.google.com.")]
        [TestCase("google.com", "alt1.aspmx.l.google.com.")]
        [TestCase("google.com", "alt2.aspmx.l.google.com.")]
        [TestCase("google.com", "alt3.aspmx.l.google.com.")]
        [TestCase("google.com", "alt4.aspmx.l.google.com.")]
        [TestCase("sapporoworks.ne.jp", "sapporoworks.ne.jp.")]
        public void QueryMxTest(string target,string answer) {
            var d = Lookup.DnsServer();
            var dnsServer = d[0];
            var o = Lookup.QueryMx(target,dnsServer);
            Assert.AreNotEqual(o.Count, 0);

            if (o.Any(s => s == answer)){
                return;
            }
            Assert.AreEqual(answer, o);
        }
    }
}
