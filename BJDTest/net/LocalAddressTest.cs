using Bjd;
using Bjd.net;
using NUnit.Framework;

namespace BjdTest.net{

    public class LocalAddressTest{

        [Test]
        public void RemoteStrで取得したテキストで改めてLocalAddressを生成して同じかどうかを確認(){

            //setUp
            var localAddress = LocalAddress.GetInstance();
            var expected = localAddress.RemoteStr();

            //exercise
            var sut = new LocalAddress(expected);
            var actual = sut.RemoteStr();

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ValidObjException))]
        public void 無効な文字列で初期化すると例外ValidObjExceptionが発生する(){
            //exercise
            new LocalAddress("XXX");
        }
    }
}

