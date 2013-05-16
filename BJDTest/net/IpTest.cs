using Bjd.net;
using NUnit.Framework;
using Bjd;

namespace BjdTest.net {
    class IpTest {

        [TestCase("192.168.0.1", "192.168.0.1")]
        [TestCase("255.255.0.254", "255.255.0.254")]
        [TestCase("INADDR_ANY", "INADDR_ANY")]//DOTO
        [TestCase("0.0.0.0", "0.0.0.0")]
        [TestCase("IN6ADDR_ANY_INIT", "IN6ADDR_ANY_INIT")]
        [TestCase("::", "::0")]//DOTO
        [TestCase("::1", "::1")]
		[TestCase("::809f", "::809f")]
		[TestCase("ff34::809f", "ff34::809f")]
        [TestCase("1234:56::1234:5678:90ab", "1234:56::1234:5678:90ab")]
        [TestCase("fe80::7090:40f5:96f7:17db%13", "fe80::7090:40f5:96f7:17db%13")]//Ver5.4.9
        [TestCase("12::78:90ab", "12::78:90ab")]
        [TestCase("[12::78:90ab]", "12::78:90ab")]//[括弧付きで指定された場合]
		[TestCase("fff::", "fff::")]
        public void 文字列のコンストラクタで生成してToStringで確認する(string ipStr, string expected) {
            //setUp
			var sut = new Ip(ipStr);
			//exercise
			var actual = sut.ToString();
			//verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("192.168.0.1", "192.168.0.1")]
        [TestCase("255.255.0.254", "255.255.0.254")]
        [TestCase("INADDR_ANY", "0.0.0.0")]
        [TestCase("0.0.0.0", "0.0.0.0")]
        [TestCase("IN6ADDR_ANY_INIT", "::")]
        [TestCase("::", "::")]
        [TestCase("::1", "::1")]
        [TestCase("::809f", "::809f")]
        [TestCase("ff34::809f", "ff34::809f")]
        [TestCase("1234:56::1234:5678:90ab", "1234:56::1234:5678:90ab")]
        [TestCase("fe80::7090:40f5:96f7:17db%13", "fe80::7090:40f5:96f7:17db%13")]
        [TestCase("12::78:90ab", "12::78:90ab")]
        [TestCase("[12::78:90ab]", "12::78:90ab")]//[括弧付きで指定された場合]
        public void 文字列のコンストラクタで生成してIPAddress_ToStringで確認する(string ipStr, string expected) {
            //setUp
            var sut = new Ip(ipStr);
            //exercise
            var actual = sut.IPAddress.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(192, 168, 0, 1)]
        [TestCase(127, 0, 0, 1)]
        [TestCase(0, 0, 0, 0)]
        [TestCase(255, 255, 255, 255)]
        [TestCase(255, 255, 0, 254)]
        public void プロパティIpV4の確認(int n1, int n2, int n3, int n4){
            //setUp
            var ipStr = string.Format("{0}.{1}.{2}.{3}", n1, n2, n3, n4);
            var sut = new Ip(ipStr);
            //exercise
            var p = sut.IpV4;
            //verify
            Assert.That(p[0], Is.EqualTo(n1));
            Assert.That(p[1], Is.EqualTo(n2));
            Assert.That(p[2], Is.EqualTo(n3));
            Assert.That(p[3], Is.EqualTo(n4));
        }

        [TestCase("1234:56::1234:5678:90ab", 0x12, 0x34, 0x00, 0x56, 0, 0, 0, 0, 0, 0, 0x12, 0x34, 0x56, 0x78, 0x90, 0xab)]
        [TestCase("1::1", 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
		[TestCase("ff04::f234", 0xff, 0x04, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xf2, 0x34)]
        [TestCase("1::1%16", 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
        [TestCase("[1::1]", 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
        public void プロパティIpV6の確認(string ipStr, int n1, int n2, int n3, int n4, int n5, int n6, int n7, int n8, int n9, int n10, int n11, int n12, int n13, int n14, int n15, int n16) {
            //setUp
            var sut = new Ip(ipStr);
            //exercise
            var p = sut.IpV6;
            //verify
            Assert.That(p[0], Is.EqualTo(n1));
            Assert.That(p[1], Is.EqualTo(n2));
            Assert.That(p[2], Is.EqualTo(n3));
            Assert.That(p[3], Is.EqualTo(n4));
            Assert.That(p[4], Is.EqualTo(n5));
            Assert.That(p[5], Is.EqualTo(n6));
            Assert.That(p[6], Is.EqualTo(n7));
            Assert.That(p[7], Is.EqualTo(n8));
            Assert.That(p[8], Is.EqualTo(n9));
            Assert.That(p[9], Is.EqualTo(n10));
            Assert.That(p[10], Is.EqualTo(n11));
            Assert.That(p[11], Is.EqualTo(n12));
            Assert.That(p[12], Is.EqualTo(n13));
            Assert.That(p[13], Is.EqualTo(n14));
            Assert.That(p[14], Is.EqualTo(n15));
            Assert.That(p[15], Is.EqualTo(n16));
        }

        [TestCase("192.168.0.1", "192.168.0.1", true)]
        [TestCase("192.168.0.1", "192.168.0.2", false)]
        [TestCase("192.168.0.1", null, false)]
        [TestCase("::1", "::1", true)]
        [TestCase("::1%1", "::1%1", true)]
        [TestCase("::1%1", "::1", false)]
        [TestCase("ff01::1", "::1", false)]
        [TestCase("::1", null, false)]
        public void 演算子イコールの判定_null判定(string ipStr,string targetStr,bool expected) {
            //setUp
            var sut = new Ip(ipStr);
            Ip target = null;
            if(targetStr!=null){
                target = new Ip(targetStr);
            }
            //exercise
			var actual = (sut == target);
			//verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("1.2.3.4")]
        [TestCase("192.168.0.1")]
        [TestCase("255.255.255.255")]
        [TestCase("INADDR_ANY")]
        public void AddrV4で取得した値からIpオブジェクトを再構築する(string ipStr) {
            //setUp
            var sut = new Ip((new Ip(ipStr)).AddrV4);
            var expected = ipStr;
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("102:304:506:708:90a:b0c:d0e:f01")]
        [TestCase("ff83::e:f01")]
        [TestCase("::1")]
        [TestCase("fff::")]
        public void AddrV6HとAddrV6Lで取得した値からIpオブジェクトを再構築する(string ipStr) {
            //setUp
            var ip = new Ip(ipStr);
            var sut = new Ip(ip.AddrV6H,ip.AddrV6L);
            var expected = ipStr;
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [TestCase("")]
        [TestCase("IN_ADDR_ANY")]
        [TestCase("xxx")]
        [TestCase("192.168.0.1.2")]
        [TestCase(null)]
        [TestCase("11111::")]
        [ExpectedException(typeof(ValidObjException))]
        public void 無効な文字列による初期化の例外テスト(string ipStr) {
            //exercise
            new Ip(ipStr);
        }

        [TestCase("192.168.0.1", 0xc0a80001)]
        public void AddrV4の検証(string ipStr,uint ip) {
            //setUp
            var sut = new Ip(ipStr);
            var expected = ip;
            //exercise
            var actual = sut.AddrV4;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("1234:56::1234:5678:90ab", 0x1234005600000000uL, 0x00001234567890abuL)]
        public void AddrV6の検証(string ipStr, ulong v6h,ulong v6l) {
            //setUp
            var sut = new Ip(ipStr);
            //exercise
            ulong h = sut.AddrV6H;
            ulong l = sut.AddrV6L;
            //verify
            Assert.That(h, Is.EqualTo(v6h));
            Assert.That(l, Is.EqualTo(v6l));
        }

    }
}
