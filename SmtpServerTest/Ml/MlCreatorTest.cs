using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SmtpServer;


namespace SmtpServerTest {
    [TestFixture]
    internal class MlCreatorTest{
        MlCreator _mlCreator;

        [SetUp]
        public void SetUp(){
            var mlAddr = new MlAddr("1ban", new List<string>{"example.com"});
            var docs = new List<string>();
            foreach (var docKind in Enum.GetValues(typeof(MlDocKind))) {
                var buf = docKind.ToString();
                if (buf.Length < 2 || buf[buf.Length - 2] != '\r' || buf[buf.Length - 1] != '\n') {
                    buf = buf + "\r\n";
                }
                docs.Add(buf);
            }
            _mlCreator = new MlCreator(mlAddr, docs);
            
        }


        [TearDown]
        public void TearDown(){
            
        }
        //[Test]
        //public  void WelcomeTest(){
        //    var mail = _mlCreator.Welcome();
        //    var body = Encoding.ASCII.GetString(mail.GetBody());
        //    var subject = mail.GetHeader("Subject");
        //    var contentType = mail.GetHeader("Content-Type");

        //    Assert.AreEqual(body, "Welcome\r\n");
        //    Assert.AreEqual(subject, "welcome (1ban ML)");
        //    Assert.AreEqual(contentType, "text/plain; charset=iso-2022-jp");

        //}

        [TestCase(MlDocKind.Welcome)]
        [TestCase(MlDocKind.Admin)]
        [TestCase(MlDocKind.Help)]
        [TestCase(MlDocKind.Guide)]
        public void CreateMailTest(MlDocKind kind) {
            var mail = _mlCreator.Welcome();
            switch (kind) {
                case MlDocKind.Admin:
                    mail = _mlCreator.Admin();
                    break;
                case MlDocKind.Help:
                    mail = _mlCreator.Help();
                    break;
                case MlDocKind.Guide:
                    mail = _mlCreator.Guide();
                    break;
            }

            var body = Encoding.ASCII.GetString(mail.GetBody());
            var subject = mail.GetHeader("Subject");
            var contentType = mail.GetHeader("Content-Type");

            Assert.AreEqual(body, string.Format("{0}\r\n",kind));
            Assert.AreEqual(subject, string.Format("{0} (1ban ML)",kind.ToString().ToLower()));
            Assert.AreEqual(contentType, "text/plain; charset=iso-2022-jp");
        }


        
    }
}
