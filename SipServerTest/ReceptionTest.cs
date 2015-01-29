using System.Runtime.Remoting;
using System.Text;
using NUnit.Framework;
using SipServer;

namespace SipServerTest {
    [TestFixture]
    class ReceptionTest {

        [SetUp]
        public void SetUp() {
        }

        [TearDown]
        public void TearDown() {

        }

        Reception Create(Tl tl) {
            return new Reception((new TestLines()).Get(tl));
        }

        [TestCase(Tl.Register0, ReceptionKind.Request)]
        [TestCase(Tl.Status0, ReceptionKind.Status)]
        [TestCase(Tl.Invite0, ReceptionKind.Request)]
        [TestCase(Tl.Invite1, ReceptionKind.Request)]
        [TestCase(Tl.Register1, ReceptionKind.Request)]
        [TestCase(Tl.Register2, ReceptionKind.Request)]
        public void ReceptionKindの解釈(Tl tl, ReceptionKind receptionKind) {
            //setup
            var sut = Create(tl);
            var exception = receptionKind;
            //exercise
            var actual = sut.StartLine.ReceptionKind;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(Tl.Register0, SipMethod.Register)]
        [TestCase(Tl.Status0, SipMethod.Unknown)]//異常系
        [TestCase(Tl.Invite0, SipMethod.Invite)]
        [TestCase(Tl.Invite1, SipMethod.Invite)]
        [TestCase(Tl.Register1, SipMethod.Register)]
        [TestCase(Tl.Register2, SipMethod.Register)]
        public void SipMethodの解釈(Tl tl, SipMethod sipMethod) {
            //setup
            var sut = Create(tl);
            var exception = sipMethod;
            //exercise
            var actual = sut.StartLine.SipMethod;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(Tl.Register0, 0)]//異常系
        [TestCase(Tl.Status0, 401)]
        [TestCase(Tl.Invite0, 0)]//異常系
        [TestCase(Tl.Invite1, 0)]//異常系
        [TestCase(Tl.Register1, 0)]
        [TestCase(Tl.Register2, 0)]
        public void StatusCodeの解釈(Tl tl, int statusCode) {
            //setup
            var sut = Create(tl);
            var exception = statusCode;
            //exercise
            var actual = sut.StartLine.StatusCode;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(Tl.Register0, "User-Agent", "snom105-2.04g")]
        [TestCase(Tl.Status0, "User-Agent", "Asterisk PBX")]
        [TestCase(Tl.Invite0, "User-Agent", "Windows RTC/1.0")]
        [TestCase(Tl.Invite1, "User-Agent", "X-Lite release 4.5.5  stamp 71236")]
        [TestCase(Tl.Register1, "User-Agent", null)]
        [TestCase(Tl.Register2, "User-Agent", "Fletsphone/2.3 NTTEAST/NTTWEST")]
        public void Header内容の確認(Tl tl, string key, string value) {
            //setup
            var sut = Create(tl);
            var exception = value;
            //exercise
            var actual = sut.Header.GetVal(key);
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }


        [TestCase(Tl.Register0, 0)]
        [TestCase(Tl.Status0, 0)]
        [TestCase(Tl.Invite0, 18)]
        [TestCase(Tl.Invite1, 0)]
        [TestCase(Tl.Register1, 0)]
        [TestCase(Tl.Register2, 0)]
        public void Bodyの行数の確認(Tl tl, int? count) {
            //setup
            var sut = Create(tl);
            var exception = count;
            //exercise
            var actual = sut.Body.Count;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

    }
}
