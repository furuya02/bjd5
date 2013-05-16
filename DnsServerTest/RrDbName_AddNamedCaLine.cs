using System.IO;
using Bjd.util;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    public class RrDbTest_addNamedCaLine{
        
        // 共通メソッド
        // リソースレコードのtostring()
        private string print(OneRr o){
            switch (o.DnsType){
                case DnsType.A:
                    return o.ToString();
                case DnsType.Aaaa:
                    return o.ToString();
                case DnsType.Ns:
                    return o.ToString();
                case DnsType.Mx:
                    return o.ToString();
                case DnsType.Ptr:
                    return o.ToString();
                case DnsType.Soa:
                    return o.ToString();
                case DnsType.Cname:
                    return o.ToString();
                default:
                    Util.RuntimeException("not implement.");
                    break;
            }
            return "";
        }

        [Test]
        public void コメント行は処理されない(){
            //setUp
            var sut = new RrDb();
            //exercise
            var expected = 0;
            RrDbTest.AddNamedCaLine(sut, "", "; formerly NS.INTERNIC.NET");
            var actual = RrDbTest.Size(sut);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 空白行は処理されない(){
            //setUp
            var sut = new RrDb();
            //exercise
            var expected = 0;
            RrDbTest.AddNamedCaLine(sut, "", "");
            var actual = RrDbTest.Size(sut);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void Aレコードの処理(){
            //setUp
            var sut = new RrDb();
            //exercise
            var retName = RrDbTest.AddNamedCaLine(sut, "", "A.ROOT-SERVERS.NET.      3600000      A     198.41.0.4");
            //verify
            Assert.That(retName, Is.EqualTo("A.ROOT-SERVERS.NET."));
            Assert.That(RrDbTest.Size(sut), Is.EqualTo(1)); //A
            Assert.That(print(RrDbTest.Get(sut, 0)), Is.EqualTo("A A.ROOT-SERVERS.NET. TTL=0 198.41.0.4")); //TTLは強制的に0になる
        }

        [Test]
        public void AAAAレコードの処理(){
            //setUp
            var sut = new RrDb();
            //exercise
            var retName = RrDbTest.AddNamedCaLine(sut, "", "A.ROOT-SERVERS.NET.      3600000      AAAA  2001:503:BA3E::2:30");
            //verify
            Assert.That(retName, Is.EqualTo("A.ROOT-SERVERS.NET."));
            Assert.That(RrDbTest.Size(sut), Is.EqualTo(1)); //Aaaa
            Assert.That(print(RrDbTest.Get(sut, 0)), Is.EqualTo("Aaaa A.ROOT-SERVERS.NET. TTL=0 2001:503:ba3e::2:30")); //TTLは強制的に0になる
        }

        [Test]
        public void NSレコードの処理(){
            //setUp
            var sut = new RrDb();
            //exercise
            string retName = RrDbTest.AddNamedCaLine(sut, "", ".                        3600000  IN  NS    A.ROOT-SERVERS.NET.");
            //verify
            Assert.That(retName, Is.EqualTo("."));
            Assert.That(RrDbTest.Size(sut), Is.EqualTo(1)); //Ns
            Assert.That(print(RrDbTest.Get(sut, 0)), Is.EqualTo("Ns . TTL=0 A.ROOT-SERVERS.NET.")); //TTLは強制的に0になる
        }

        [Test]
        [ExpectedException(typeof (IOException))]
        public void DnsTypeが無い場合例外が発生する(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.AddNamedCaLine(sut, "", ".                        3600000  IN      A.ROOT-SERVERS.NET.");
        }

        [Test]
        [ExpectedException(typeof (IOException))]
        public void DnsTypeの次のカラムのDataが無い場合例外が発生する(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.AddNamedCaLine(sut, "", ".                        3600000  IN  NS");
        }

        [Test]
        [ExpectedException(typeof (IOException))]
        public void Aタイプでアドレスに矛盾があると例外が発生する(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.AddNamedCaLine(sut, "", "A.ROOT-SERVERS.NET.      3600000      A     ::1");
        }


        [Test]
        [ExpectedException(typeof (IOException))]
        public void AAAAタイプでアドレスに矛盾があると例外が発生する(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.AddNamedCaLine(sut, "", "A.ROOT-SERVERS.NET.      3600000      AAAA     192.168.0.1");
        }

        [Test]
        [ExpectedException(typeof (IOException))]
        public void A_AAAA_NS以外タイプは例外が発生する(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.AddNamedCaLine(sut, "", ".                        3600000  IN  MX    A.ROOT-SERVERS.NET.");
        }

        [Test]
        [ExpectedException(typeof (IOException))]
        public void Aタイプで不正なアドレスを指定すると例外が発生する(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.AddNamedCaLine(sut, "", "A.ROOT-SERVERS.NET.      3600000      A     1.1.1.1.1");
        }

        [Test]
        [ExpectedException(typeof (IOException))]
        public void AAAAタイプで不正なアドレスを指定すると例外が発生する(){
            //setUp
            var sut = new RrDb();
            //exercise
            RrDbTest.AddNamedCaLine(sut, "", "A.ROOT-SERVERS.NET.      3600000      AAAA     xxx");
        }

        [Test]
        public void 名前補完_アットマークの場合ドメイン名になる(){
            //setUp
            var sut = new RrDb();
            //exercise
            var expected = "example.com.";
            var actual = RrDbTest.AddNamedCaLine(sut, "", "@      3600000      A     198.41.0.4");
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 名前補完_最後にドットが無い場合_ドメイン名が補完される(){
            //setUp
            var sut = new RrDb();
            //exercise
            var expected = "www.example.com.";
            var actual = RrDbTest.AddNamedCaLine(sut, "", "www      3600000      A     198.41.0.4");
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 名前補完_指定されない場合_前行と同じになる(){
            //setUp
            var sut = new RrDb();
            //exercise
            var expected = "before.aaa.com.";
            var actual = RrDbTest.AddNamedCaLine(sut, "before.aaa.com.", "     3600000      A     198.41.0.4");
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}