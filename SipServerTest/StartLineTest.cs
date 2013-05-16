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

        //リクエストラインのテスト
        [TestCase("SIP",SipMethod.Unknown, "", (float)0)]//異常系
        [TestCase("xxx sip:1@1 SIP/1.0\r\n", SipMethod.Unknown, "", (float)0)]//異常系
        [TestCase("invite sip:1@1 SIP/1.5\r\n", SipMethod.Invite, "1@1", (float)1.5)]
        [TestCase("INVITE sip:UserB@there.com SIP/2.0\r\n", SipMethod.Invite, "UserB@there.com", (float)2.0)]
        public void RequestLineTest(string str,SipMethod sipMethod,string requestUri,float verNo) {
            var startLine = new StartLine(Encoding.ASCII.GetBytes(str));
            
            Assert.AreEqual(startLine.SipMethod,sipMethod);
            Assert.AreEqual(startLine.RequestUri, requestUri);
            Assert.AreEqual(startLine.SipVer.No, verNo);
        }
        
        //ステータスラインのテスト
        [TestCase("SIP/2.0 180 Ringing\r\n", 180, (float)2.0)]
        [TestCase("SIP/2.0 200 OK\r\n", 200, (float)2.0)]
        [TestCase("SIP/2.0 200 \r\n", 0, (float)0)]//異常系
        public void StatusLineTest(string str, int statusCode, float verNo) {
            var startLine = new StartLine(Encoding.ASCII.GetBytes(str));

            Assert.AreEqual(startLine.StatusCode, statusCode);
            Assert.AreEqual(startLine.SipVer.No, verNo);
        }

        //ReceptionKindの判定
        [TestCase("SIP/2.0 180 Ringing\r\n", ReceptionKind.Status)]
        [TestCase("SIP/2.0 200 OK\r\n", ReceptionKind.Status)]
        [TestCase("SIP/2.0 200\r\n", ReceptionKind.Unknown)]//項目不足
        [TestCase("INVITE sip SIP/1.0\r\n", ReceptionKind.Unknown)] //無効項目
        [TestCase("xxx sip:1@1 SIP/1.0\r\n", ReceptionKind.Unknown)] //無効メソッド
        [TestCase("INVITE sip:UserB@there.com \r\n", ReceptionKind.Unknown)]//項目不足
        [TestCase("invite sip:1@1 SIP/1.5\r\n", ReceptionKind.Request)]
        [TestCase("INVITE sip:UserB@there.com SIP/2.0\r\n", ReceptionKind.Request)]
        public void ReceptionKindTest(string str, ReceptionKind receptionKind) {
            
            var startLine = new StartLine(Encoding.ASCII.GetBytes(str));

            Assert.AreEqual(startLine.ReceptionKind, receptionKind);
        }


    }
}
