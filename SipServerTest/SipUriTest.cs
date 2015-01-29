using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SipServer;

namespace SipServerTest {
    [TestFixture]
    class SipUriTest {
        
        [SetUp]
        public void SetUp() {
        }

        [TearDown]
        public void TearDown() {
        }

        [TestCase(" \"astarisk\" <sip:3000@192.168.0.11>", "astarisk")]
        [TestCase("Bob", "Bob")]
        [TestCase("sip:alice@example.com", "")]
        [TestCase(" Maki <sip:User@east.net>;tag=c38756", "Maki")]
        [TestCase("Bob ;tag=a73kszlfl", "Bob")]
        [TestCase("sip:200@192.168.0.100;tag=a73kszlfl", "")]
        [TestCase("<sip:3000@192.168.0.12:5060;line=jet7pbic>;q=1.0", "")]
        [TestCase("sip:192.168.0.11", "")]
        public void Detailの解釈(string str, string display) {
            //setup
            var sut = new SipUri(str);
            var exception = display;
            //exercise
            var actual = sut.Display;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(" \"astarisk\" <sip:3000@192.168.0.11>", Protocol.Sip)]
        [TestCase("Bob", Protocol.Unknown)]
        [TestCase("sip:alice@example.com", Protocol.Sip)]
        [TestCase("sips:alice@example.com", Protocol.Sips)]
        [TestCase(" Maki <sip:User@east.net>;tag=c38756", Protocol.Sip)]
        [TestCase("Bob ;tag=a73kszlfl", Protocol.Unknown)]
        [TestCase("sip:200@192.168.0.100;tag=a73kszlfl",Protocol.Sip)]
        [TestCase("<sip:3000@192.168.0.12:5060;line=jet7pbic>;q=1.0", Protocol.Sip)]
        [TestCase("sip:192.168.0.11", Protocol.Sip)]
        public void Protocolの解釈(string str, Protocol protocol) {
            //setup
            var sut = new SipUri(str);
            var exception = protocol;
            //exercise
            var actual = sut.Protocol;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(" \"astarisk\" <sip:3000@192.168.0.11>", "3000")]
        [TestCase("Bob", "")]
        [TestCase("sip:alice@example.com", "alice")]
        [TestCase(" Maki <sip:User@east.net>;tag=c38756", "User")]
        [TestCase("Bob ;tag=a73kszlfl", "")]
        [TestCase("sip:200@192.168.0.100;tag=a73kszlfl", "200")]
        [TestCase("<sip:3000@192.168.0.12:5060;line=jet7pbic>;q=1.0", "3000")]
        [TestCase("sip:192.168.0.11", "")]
        public void Nameの解釈(string str, string name) {
            //setup
            var sut = new SipUri(str);
            var exception = name;
            //exercise
            var actual = sut.Name;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(" \"astarisk\" <sip:3000@192.168.0.11>", "192.168.0.11")]
        [TestCase("Bob", "")]
        [TestCase("sip:alice@example.com", "example.com")]
        [TestCase(" Maki <sip:User@east.net>;tag=c38756", "east.net")]
        [TestCase("Bob ;tag=a73kszlfl", "")]
        [TestCase("sip:200@192.168.0.100;tag=a73kszlfl", "192.168.0.100")]
        [TestCase("<sip:3000@192.168.0.12:5060;line=jet7pbic>;q=1.0", "192.168.0.12")]
        [TestCase("sip:192.168.0.11", "192.168.0.11")]
        public void Hostの解釈(string str, string host) {
            //setup
            var sut = new SipUri(str);
            var exception = host;
            //exercise
            var actual = sut.Host;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }


        [TestCase(" \"astarisk\" <sip:3000@192.168.0.11>",5060)]
        [TestCase("Bob", 5060)]
        [TestCase("sip:alice@example.com", 5060)]
        [TestCase(" Maki <sip:User@east.net>;tag=c38756", 5060)]
        [TestCase("Bob ;tag=a73kszlfl", 5060)]
        [TestCase("sip:200@192.168.0.100;tag=a73kszlfl", 5060)]
        [TestCase("<sip:3000@192.168.0.12:5060;line=jet7pbic>;q=1.0",5060)]
        [TestCase("<sip:3000@192.168.0.12:5061;line=jet7pbic>;q=1.0", 5061)]
        [TestCase("sip:192.168.0.11", 5060)]
        public void Portの解釈(string str, int port) {
            //setup
            var sut = new SipUri(str);
            var exception = port;
            //exercise
            var actual = sut.Port;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

    }
}
