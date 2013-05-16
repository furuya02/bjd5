using System;
using Bjd.util;
using NUnit.Framework;

namespace BjdTest.util {


    [TestFixture]
    public class CryptTest{



        [TestCase("本日は晴天なり")]
        [TestCase("123")]
        [TestCase("xxxx")]
        [TestCase("1\r\n2")]
        public void Encrypt及びDecrypt(string str){
            //setUp
            var expected = str;
            //exercise
            var actual = Crypt.Decrypt(Crypt.Encrypt(str));
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null)]
        public void Encryptの例外テスト(string str){
            try{
                Crypt.Encrypt(str);
                Assert.Fail("この行が実行されたらエラー");
            } catch (Exception){
            }
        }

        [TestCase(null)]
        [TestCase("123")]
        [TestCase("本日は晴天なり")]
        public void Decryptの例外テスト(string str){
            try{
                Crypt.Decrypt(str);
                Assert.Fail("この行が実行されたらエラー");
            } catch (Exception){
            }
        }
    }
}