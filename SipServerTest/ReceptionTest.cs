using System.Runtime.Remoting;
using System.Text;
using NUnit.Framework;
using SipServer;

namespace SipServerTest {
    [TestFixture]
    class ReceptionTest {

        private byte[] _lines0 = new byte[0];
        private byte[] _lines1 = new byte[0];
        private byte[] _lines2 = new byte[0];
        private byte[] _lines3 = new byte[0];

        [SetUp]
        public void SetUp() {
            var sb = new StringBuilder();
            sb.Append("REGISTER sip:192.168.0.11 SIP/2.0\r\n");
            sb.Append("Via: SIP/2.0/UDP 192.168.0.12:5060;branch=z9hG4bK-hl2in066asp6;rport\r\n");
            sb.Append("From: \"astarisk\" <sip:3000@192.168.0.11>;tag=m58p6kg08q\r\n");
            sb.Append("To: \"astarisk\" <sip:3000@192.168.0.11>\r\n");
            sb.Append("Call-ID: 3c267765064b-g9ka5zum5eyc@192-168-0-12\r\n");
            sb.Append("CSeq: 6 REGISTER\r\n");
            sb.Append("Max-Forwards: 70\r\n");
            sb.Append("Contact: <sip:3000@192.168.0.12:5060;line=jet7pbic>;q=1.0\r\n");
            sb.Append("User-Agent: snom105-2.04g\r\n");
            sb.Append("Supported: gruu\r\n");
            sb.Append("Authorization: Digest username=\"3000\",realm=\"asterisk\",nonce=\"5274951d\",uri=\"sip:192.168.0.11\",response=\"69b077091d939afed0f984495dbe849d\",algorithm=md5\r\n");
            sb.Append("Expires: 600\r\n");
            sb.Append("Content-Length: 0\r\n");
            _lines0 =Encoding.ASCII.GetBytes(sb.ToString());

            sb = new StringBuilder();
            sb.Append("SIP/2.0 401 Unauthorized\r\n");
            sb.Append("Via: SIP/2.0/UDP 192.168.0.10:5060;branch=z9hG4bK-7sqqen36earh;received=192.168.0.10;rport=5060\r\n");
            sb.Append("From: \"astarisk\" <sip:3001@192.168.0.11>;tag=xcfbqjoo80\r\n");
            sb.Append("To: \"astarisk\" <sip:3001@192.168.0.11>;tag=as672f9784\r\n");
            sb.Append("Call-ID: 3c26700a1ff6-6liv2cfevscu@192-168-0-10\r\n");
            sb.Append("CSeq: 1 REGISTER\r\n");
            sb.Append("User-Agent: Asterisk PBX\r\n");
            sb.Append("Allow: INVITE, ACK, CANCEL, OPTIONS, BYE, REFER, SUBSCRIBE, NOTIFY\r\n");
            sb.Append("WWW-Authenticate: Digest algorithm=MD5, realm=\"asterisk\", nonce=\"2ba6cb50\"\r\n");
            sb.Append("Content-Length: 0\r\n");
            _lines1 =Encoding.ASCII.GetBytes(sb.ToString());

            sb = new StringBuilder();
            sb.Append("INVITE sip:7170@iptel.org SIP/2.0\r\n");
            sb.Append("Via: SIP/2.0/UDP 195.37.77.100:5040;rport\r\n");
            sb.Append("Max-Forwards: 10\r\n");
            sb.Append("From: \"jiri\" <sip:jiri@iptel.org>;tagi=76ff7a07-c091-4192-84a0-d56e91fe104f\r\n");
            sb.Append("To: <sip:jiri@bat.iptel.org>\r\n");
            sb.Append("Call-ID: d10815e0-bf17-4afa-8412-d9130a793d96@213.20.128.35\r\n");
            sb.Append("CSeqi: 2 INVITE\r\n");
            sb.Append("Contact: <sip:213.20.128.35:9315>\r\n");
            sb.Append("User-Agent: Windows RTC/1.0\r\n");
            sb.Append("Proxy-Authorization: Digest username=\"jiri\", realm=\"iptel.org\",algorithm=\"MD5\", uri=\"sip:jiri@bat.iptel.org\", nonce=\"3cef753900000001771328f5ae1b8b7f0d742da1feb5753c\", response=\"53fe98db10e1074b03b3e06438bda70f\"\r\n");
            sb.Append("Content-Type: application/sdp\r\n");
            sb.Append("Content-Length: 451\r\n");
            sb.Append("\r\n");
            sb.Append("v=0\r\n");
            sb.Append("o=jku2 0 0 IN IP4 213.20.128.35\r\n");
            sb.Append("s=session\r\n");
            sb.Append("c=IN IP4 213.20.128.35\r\n");
            sb.Append("b=CT:1000\r\n");
            sb.Append("t=0 0\r\n");
            sb.Append("m=audio 54742 RTPi/AVP 97 111 112 6 0 8 4 5 3 101\r\n");
            sb.Append("a=rtpmap:97 red/8000\r\n");
            sb.Append("a=rtpmap:111 SIREN/16000\r\n");
            sb.Append("a=fmtp:111 bitrate=16000\r\n");
            sb.Append("a=rtpmap:112 G7221/16000\r\n");
            sb.Append("a=fmtp:112 bitrate=24000\r\n");
            sb.Append("a=rtpmap:6 DVI4/16000\r\n");
            sb.Append("a=rtpmap:0 PCMU/8000\r\n");
            sb.Append("a=rtpmap:4 G723/8000\r\n");
            sb.Append("a=rtpmap: 3 GSMi/8000\r\n");
            sb.Append("a=rtpmap:101 telephone-event/8000\r\n");
            sb.Append("a=fmtp:101 0-16\r\n");
            _lines2 = Encoding.ASCII.GetBytes(sb.ToString());

            sb = new StringBuilder();
            sb.Append("INVITE sip:7170@iptel.org SIP/2.0\r\n");
            sb.Append("REGISTER sip:192.168.0.106 SIP/2.0\r\n");
            sb.Append("Via: SIP/2.0/UDP 192.168.0.100:63466;branch=z9hG4bK-d8754z-9d2277295e25ce56-1---d8754z-;rport\r\n");
            sb.Append("Max-Forwards: 70\r\n");
            sb.Append("Contact: <sip:301@192.168.0.100:63466;rinstance=795c9d302f9064c8>\r\n");
            sb.Append("To: <sip:301@192.168.0.106>\r\n");
            sb.Append("From: <sip:301@192.168.0.106>;tag=f74ac60b\r\n");
            sb.Append("Call-ID: YjkzYjRiM2Q3YTMyMGI1MTg4NTczZGQ3MGFkZDc0MzI\r\n");
            sb.Append("CSeq: 1 REGISTER\r\n");
            sb.Append("Expires: 3600\r\n");
            sb.Append("Allow: INVITE, ACK, CANCEL, OPTIONS, BYE, REFER, NOTIFY, MESSAGE, SUBSCRIBE, INFO\r\n");
            sb.Append("User-Agent: X-Lite release 4.5.5  stamp 71236\r\n");
            sb.Append("Content-Length: 0\r\n");

            _lines3 = Encoding.ASCII.GetBytes(sb.ToString());


        }
        [TearDown]
        public void TearDown() {

        }

        Reception CreateReception(string name) {
            if (name == "lines_0") {
                return new Reception(_lines0);
            } if (name == "lines_1") {
                return new Reception(_lines1);
            } if (name == "lines_2") {
                return new Reception(_lines2);
            } if (name == "lines_3") {
                return new Reception(_lines3);
            }
            return null;
        }

        [TestCase("lines_0", ReceptionKind.Request)]
        [TestCase("lines_1", ReceptionKind.Status)]
        [TestCase("lines_2", ReceptionKind.Request)]
        [TestCase("lines_3", ReceptionKind.Request)]
        public void ReceptionKindの解釈(string name, ReceptionKind receptionKind) {
            //setup
            var sut = CreateReception(name);
            var exception = receptionKind;
            //exercise
            var actual = sut.StartLine.ReceptionKind;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase("lines_0", SipMethod.Register)]
        [TestCase("lines_1", SipMethod.Unknown)]//異常系
        [TestCase("lines_2", SipMethod.Invite)]
        [TestCase("lines_3", SipMethod.Invite)]
        public void SipMethodの解釈(string name, SipMethod sipMethod) {
            //setup
            var sut = CreateReception(name);
            var exception = sipMethod;
            //exercise
            var actual = sut.StartLine.SipMethod;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase("lines_0", 0)]//異常系
        [TestCase("lines_1", 401)]
        [TestCase("lines_2", 0)]//異常系
        [TestCase("lines_3", 0)]//異常系
        public void StatusCodeの解釈(string name, int statusCode) {
            //setup
            var sut = CreateReception(name);
            var exception = statusCode;
            //exercise
            var actual = sut.StartLine.StatusCode;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase("lines_0", "User-Agent", "snom105-2.04g")]
        [TestCase("lines_1", "User-Agent", "Asterisk PBX")]
        [TestCase("lines_2", "User-Agent", "Windows RTC/1.0")]
        [TestCase("lines_3", "User-Agent", "X-Lite release 4.5.5  stamp 71236")]
        public void Header内容の確認(string name, string key, string value) {
            //setup
            var sut = CreateReception(name);
            var exception = value;
            //exercise
            var actual = sut.Header.GetVal(key);
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        
        [TestCase("lines_0",0)]
        [TestCase("lines_1", 0)]
        [TestCase("lines_2", 18)]
        [TestCase("lines_3", 0)]
        public void Bodyの行数の確認(string name, int? count) {
            //setup
            var sut = CreateReception(name);
            var exception = count;
            //exercise
            var actual = sut.Body.Count;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

    }
}
