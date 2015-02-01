using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SipServerTest {
    enum Tl{
        Register0,
        Register1,
        Register2,

        Status0,
        
        Invite0,
        Invite1,
    }
    class TestLines {


        readonly List<byte[]> _register = new List<byte[]>();
        readonly List<byte[]> _invite = new List<byte[]>();
        readonly List<byte[]> _status = new List<byte[]>();

        public byte[] Register(int n){
            return _register[n];
        }
        public byte[] Invite(int n) {
            return _invite[n];
        }
        public byte[] Status(int n) {
            return _status[n];
        }

        public byte[] Get(Tl tl){
            switch (tl){
                case Tl.Status0:
                    return Status(0);

                case Tl.Invite0:
                    return Invite(0);
                case Tl.Invite1:
                    return Invite(1);
                
                case Tl.Register0:
                    return Register(0);
                case Tl.Register1:
                    return Register(1);
                case Tl.Register2:
                    return Register(2);
            }
            return new byte[0];
        }

        public TestLines(){

            for (var i = 0; i < 50; i++){
                _register.Add(new byte[0]);
                _invite.Add(new byte[0]);
                _status.Add(new byte[0]);
            }

            //****************************************************
            // REGISTER
            //****************************************************

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
            _register[0] = Encoding.ASCII.GetBytes(sb.ToString());

            sb = new StringBuilder();
            sb.Append("REGISTER sips:ss2.biloxi.example.com SIP/2.0\r\n");
            sb.Append("Via: SIP/2.0/TLS client.biloxi.example.com:5061;branch=z9hG4bKnashds7\r\n");
            sb.Append("Max-Forwards: 70\r\n");
            sb.Append("From: Bob ;tag=a73kszlfl\r\n");
            sb.Append("To: Bob\r\n");
            sb.Append("Call-ID: 1j9FpLxk3uxtm8tn@biloxi.example.com\r\n");
            sb.Append("CSeq: 1 REGISTER\r\n");
            sb.Append("Contact:\r\n");
            sb.Append("Content-Length: 0\r\n");
            _register[1] = Encoding.ASCII.GetBytes(sb.ToString());

            sb = new StringBuilder();
            sb.Append("REGISTER sip:192.168.1.1 SIP/2.0\r\n");
            sb.Append("Via: SIP/2.0/UDP 192.168.1.7:5060;branch=z9hG4bK1869152357\r\n");
            sb.Append("Max-Forwards: 70\r\n");
            sb.Append("To: <sip:4@192.168.1.1>\r\n");
            sb.Append("From: <sip:4@192.168.1.1>;tag=1569973373\r\n");
            sb.Append("Call-ID: 02CE00808710205C0003398EF90A@192.168.1.7\r\n");
            sb.Append("CSeq: 1 REGISTER\r\n");
            sb.Append("Expires: 0\r\n");
            sb.Append("User-Agent: Fletsphone/2.3 NTTEAST/NTTWEST\r\n");
            sb.Append("Contact: *\r\n");
            sb.Append("Content-Length: 0\r\n");
            _register[2] = Encoding.ASCII.GetBytes(sb.ToString());

            sb = new StringBuilder();
            sb.Append("REGISTER sip:example.com SIP/2.0\r\n");
            sb.Append("Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bknashds8\r\n");
            sb.Append("Max-Forwards: 70\r\n");
            sb.Append("To: sip:alice@example.com\r\n");
            sb.Append("From: sip:alice@example.com;tag=1008141161\r\n");
            sb.Append("Call-ID: 75671f481397401d8f6508d51ae9a1dc@client.example.com\r\n");
            sb.Append("CSeq: 1 REGISTER\r\n");
            sb.Append("Contact: sip:alice@client.example.com:5060;expires=3600\r\n");
            sb.Append("Content-Length: 0\r\n");
            _register[3] = Encoding.ASCII.GetBytes(sb.ToString());

            sb = new StringBuilder();
            sb.Append("REGISTER sip:192.168.1.1 SIP/2.0\r\n");
            sb.Append("Via: SIP/2.0/UDP 192.168.1.7:5060;branch=z9hG4bK1869152357\r\n");
            sb.Append("Max-Forwards: 70\r\n");
            sb.Append("To: <sip:4@192.168.1.1>\r\n");
            sb.Append("From: <sip:4@192.168.1.1>;tag=1569973373\r\n");
            sb.Append("Call-ID: 02CE00808710205C0003398EF90A@192.168.1.7\r\n");
            sb.Append("CSeq: 1 REGISTER\r\n");
            sb.Append("Expires: 0\r\n");
            sb.Append("User-Agent: Fletsphone/2.3 NTTEAST/NTTWEST\r\n");
            sb.Append("Contact: *\r\n");
            sb.Append("Content-Length: 0\r\n");
            _register[4] = Encoding.ASCII.GetBytes(sb.ToString());

            //****************************************************
            // STATUS
            //****************************************************

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
            _status[0] = Encoding.ASCII.GetBytes(sb.ToString());

            //****************************************************
            // INVARIDATE
            //****************************************************
            
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
            _invite[0] = Encoding.ASCII.GetBytes(sb.ToString());


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

            _invite[1] = Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}
