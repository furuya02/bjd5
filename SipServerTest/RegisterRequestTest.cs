using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SipServer;

namespace SipServerTest{

    [TestFixture]
    internal class RegisterRequestTest{

        [SetUp]
        public void SetUp(){

        }

        [TearDown]
        public void TearDown(){

        }

        private RegisterRequest Create(int n){
            var reception = new Reception((new TestLines()).Register(n));
            return new RegisterRequest(reception);
        }

        [TestCase(0, "astarisk<sip:3000@192.168.0.11:5060>")]
        [TestCase(1, "Bob")]
        [TestCase(2, "<sip:4@192.168.1.1:5060>")]
        [TestCase(3, "<sip:alice@example.com:5060>")]
        [TestCase(4, "<sip:4@192.168.1.1:5060>")]
        public void Toの解釈(int n, string to) {
            //setup
            var sut = Create(n);
            var exception = to;
            //exercise
            var actual = sut.To.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(0, "astarisk<sip:3000@192.168.0.11:5060>")]
        [TestCase(1, "Bob")]
        [TestCase(2, "<sip:4@192.168.1.1:5060>")]
        [TestCase(3, "<sip:alice@example.com:5060>")]
        [TestCase(4, "<sip:4@192.168.1.1:5060>")]
        public void Fromの解釈(int n, string from) {
            //setup
            var sut = Create(n);
            var exception = from;
            //exercise
            var actual = sut.From.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(0, "<sip:192.168.0.11:5060>")]
        [TestCase(1, "<sips:ss2.biloxi.example.com:5060>")]
        [TestCase(2, "<sip:192.168.1.1:5060>")]
        [TestCase(3, "<sip:example.com:5060>")]
        [TestCase(4, "<sip:192.168.1.1:5060>")]
        public void Serverの解釈(int n, string server) {
            //setup
            var sut = Create(n);
            var exception = server;
            //exercise
            var actual = sut.Server.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

        [TestCase(0, 600)]
        [TestCase(1, 1800)]
        [TestCase(2, 0)]
        [TestCase(3, 3600)]
        [TestCase(4, 0)]
        public void Expiresの解釈(int n, int expires) {
            //setup
            var sut = Create(n);
            var exception = expires;
            //exercise
            var actual = sut.Expires;
            //verify
            Assert.That(actual, Is.EqualTo(exception));
        }

    }
}

