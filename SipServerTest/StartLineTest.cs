using NUnit.Framework;
using SipServer;
using System.Text;

namespace SipServerTest {
    [TestFixture]
    class StartLineTest {
        [SetUp]
        public void SetUp() {
            
        }
        [TearDown]
        public void TearDown() {
        }

        //SIPメソッドの解釈
        [TestCase("SIP", SipMethod.Unknown)]//異常系
        [TestCase("xxx sip:1@1 SIP/1.0\r\n", SipMethod.Unknown)]//異常系
        [TestCase("invite sip:1@1 SIP/1.5\r\n", SipMethod.Invite)]
        [TestCase("invite sip:1@1 SIP/1.5", SipMethod.Unknown)]//異常系(改行なし)
        [TestCase("INVITE sip:UserB@there.com SIP/2.0\r\n", SipMethod.Invite)]
        public void SipMethodの解釈(string str, SipMethod sipMethod) {
            //setup
            var sut = new StartLine(Encoding.ASCII.GetBytes(str));
            var exception = sipMethod;
            //exercise
            var actual = sut.SipMethod;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase("SIP", "")]//異常系
        [TestCase("xxx sip:1@1 SIP/1.0\r\n", "")]//異常系
        [TestCase("invite sip:1@1 SIP/1.5\r\n", "1@1")]
        [TestCase("invite sip:1@1 SIP/1.5", "")]//異常系(改行なし)
        [TestCase("INVITE sip:UserB@there.com SIP/2.0\r\n", "UserB@there.com")]
        public void RequestUriの解釈(string str, string requestUri) {
            //setup
            var sut = new StartLine(Encoding.ASCII.GetBytes(str));
            var exception = requestUri;
            //exercise
            var actual = sut.RequestUri;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase("SIP", (float)0)]//異常系
        [TestCase("xxx sip:1@1 SIP/1.0\r\n", (float)0)]//異常系
        [TestCase("invite sip:1@1 SIP/1.5\r\n", (float)1.5)]
        [TestCase("invite sip:1@1 SIP/1.5", (float)0)]//異常系(改行なし)
        [TestCase("INVITE sip:UserB@there.com SIP/2.0\r\n", (float)2.0)]
        public void SipVerの解釈(string str, float no) {
            //setup
            var sut = new StartLine(Encoding.ASCII.GetBytes(str));
            var exception = no;
            //exercise
            var actual = sut.SipVer.No;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase("SIP/2.0 180 Ringing\r\n", 180)]
        [TestCase("SIP/2.0 200 OK\r\n", 200)]
        [TestCase("SIP/2.0 200 \r\n", 0)]//異常系
        [TestCase("SIP/2.0 200", 0)]//異常系
        public void Statusコードの解釈(string str, int statusCode) {
            //setup
            var sut = new StartLine(Encoding.ASCII.GetBytes(str));
            var exception = statusCode;
            //exercise
            var actual = sut.StatusCode;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase("SIP/2.0 180 Ringing\r\n", (float)2.0)]
        [TestCase("SIP/2.0 200 OK\r\n", (float)2.0)]
        [TestCase("SIP/2.0 200 \r\n",(float)0)]//異常系
        public void Sipバージョンの解釈(string str, float verNo) {
            //setup
            var sut = new StartLine(Encoding.ASCII.GetBytes(str));
            var exception = verNo;
            //exercise
            var actual = sut.SipVer.No;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }


        //ReceptionKindの判定
        [TestCase("SIP/2.0 180 Ringing\r\n", ReceptionKind.Status)]
        [TestCase("SIP/2.0 200 OK\r\n", ReceptionKind.Status)]
        [TestCase("SIP/2.0 200 OK", ReceptionKind.Unknown)]//改行なし
        [TestCase("SIP/2.0 200\r\n", ReceptionKind.Unknown)]//項目不足
        [TestCase("INVITE sip SIP/1.0\r\n", ReceptionKind.Unknown)] //無効項目
        [TestCase("xxx sip:1@1 SIP/1.0\r\n", ReceptionKind.Unknown)] //無効メソッド
        [TestCase("INVITE sip:UserB@there.com \r\n", ReceptionKind.Unknown)]//項目不足
        [TestCase("invite sip:1@1 SIP/1.5\r\n", ReceptionKind.Request)]
        [TestCase("INVITE sip:UserB@there.com SIP/2.0\r\n", ReceptionKind.Request)]
        public void ReceptionKindの解釈(string str, ReceptionKind receptionKind) {
            //setup
            var sut = new StartLine(Encoding.ASCII.GetBytes(str));
            var exception = receptionKind;
            //exercise
            var actual = sut.ReceptionKind;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }


    }
}
