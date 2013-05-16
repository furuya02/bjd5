using Bjd;
using Bjd.mail;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;

namespace BjdTest.mail {
    [TestFixture]
    class MailBoxTest2 {
        
        private static TmpOption _op = null; //設定ファイルの上書きと退避
        private Conf _conf;

        [SetUp]
        public void SetUp(){
            //設定ファイルの退避と上書き
            _op = new TmpOption("BJDTest","MailBoxTest.ini");
            var kernel = new Kernel();
            var oneOption = new OptionMailBox(kernel,"");
            _conf = new Conf(oneOption);
        }

        [TearDown]
        public void TearDown(){
            //設定ファイルのリストア
            _op.Dispose();
        }

        [TestCase("user1","user1",true)]
        public void AuthTest(string user,string pass,bool expected) {
            //setUp
            var sut = new MailBox(new Kernel(), _conf);
            //var expected = true;
            //exercise
            var actual = sut.Auth(user,pass);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


	}

}
