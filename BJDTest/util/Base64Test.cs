using Bjd.util;
using NUnit.Framework;

namespace BjdTest.util {
    [TestFixture]
    class Base64Test{

        //[TestCase("本日は晴天なり", "本日は晴天なり")]
        //[TestCase("123", "123")]
        //[TestCase("", "")]
        [TestCase(null, "")]
        //[TestCase("1\r\n2", "1\r\n2")]
        public void Base64のエンコード及びデコード(string str, string expected){
            //exercise
            string actual = Base64.Decode(Base64.Encode(str));
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
    }
}

