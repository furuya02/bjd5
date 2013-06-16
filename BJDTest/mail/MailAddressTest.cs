using System.Linq;
using Bjd.mail;

using NUnit.Framework;

namespace BjdTest.mail {
    
    class MailAddressTest {

        [TestCase("", "", "")]
        [TestCase("user1", "user1", "")]
        [TestCase("user1@example.com", "user1", "example.com")]
        [TestCase("user1@example,jp\b\b\b.jp", "user1", "example.jp")] //バックスペースで修正されて入力
        public void コンストラクタによる名前とドメイン名の初期化(string mailaddress, string user, string domain) {
            //setUp
            var sut = new MailAddress(mailaddress);
            //verify
            Assert.That(sut.User, Is.EqualTo(user));
            Assert.That(sut.Domain, Is.EqualTo(domain));
        }

        [TestCase("1@aaa.com", new[] { "aaa.com" }, true)]
        [TestCase("1@aaa.com", new[] { "bbb.com" }, false)]
        [TestCase("1@aaa.com", new[] { "aaa.com","bbb.com" }, true)]
        [TestCase("1@bbb.com", new[] { "aaa.com", "bbb.com" }, true)]
        [TestCase("1", new[] { "aaa.com", "bbb.com" }, false)]
        public void IsLocalによるドメインに属するかどうかの確認(string mailaddress, string[] domainList, bool expected) {
            //setUp
            var sut = new MailAddress(mailaddress);
            //exercise
            var actual = sut.IsLocal(domainList.ToList());
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("user@aaa.com","user","aaa.com")]
        [TestCase("<user@aaa.com>", "user", "aaa.com")]
        [TestCase("user", "user", "")]
        [TestCase("", "", "")]
        [TestCase("\"<user@aaa.com>\"", "user", "aaa.com")]
        [TestCase("\"名前<user@aaa.com>\"", "user", "aaa.com")]
        [TestCase("\" 名前 <user@aaa.com> \"", "user", "aaa.com")]
        public void コンストラクタによる初期化(string str, string user, string domain) {
            //setUp
            var sut = new MailAddress(str);
            //exercise
            Assert.That(sut.User,Is.EqualTo(user));
            Assert.That(sut.Domain, Is.EqualTo(domain));
        }

    }

}
