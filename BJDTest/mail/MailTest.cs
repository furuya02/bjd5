using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.log;
using Bjd.mail;
using NUnit.Framework;

namespace BjdTest.mail {
    [TestFixture]
    class MailTest{
        private Mail sut = null;
        
        [SetUp]
        public void SetUp(){
            sut = new Mail();
        }
        
        [TearDown]
        public void TearDown(){
            
        }

        [Test]
        public void AddHeaderによるヘッダの追加(){
            //setUp
            const string val = "value1";
            const string tag = "tag";
            
            var expected = val;

            //exerceise
            sut.AddHeader(tag, val);
            var actual = sut.GetHeader(tag);
            
            //verify
            Assert.That(actual,Is.EqualTo(expected));
        }

        [Test]
        public void ConvertHeaderによるヘッダの変換() {
            //setUp
            const string val = "value1";
            const string tag = "tag";
            var expected = "val2";

            sut.AddHeader(tag, val);

            //exerceise
            sut.ConvertHeader(tag, expected);
            var actual = sut.GetHeader(tag);

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

    //TODO まだ、全部のテストを実装できていない

    }
}
