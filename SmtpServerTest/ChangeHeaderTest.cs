using Bjd.ctrl;
using Bjd.log;
using Bjd.mail;
using Bjd.option;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {


    class ChangeHeaderTest {
        [Test]
        public void Relpaceによるヘッダの置き換え(){
            //setUp
            var replace = new Dat(new CtrlType[]{CtrlType.TextBox, CtrlType.TextBox});
            replace.Add(true, "tag1: xxx\ttag1: yyy");
            var sut = new ChangeHeader(replace, null);

            var mail = new Mail(); 
            mail.AddHeader("tag1","xxxx");
            var s = mail.GetHeader("tag1");

            var expected = "yyy";

            //exercise
            sut.Exec(mail,null);
            var actual = mail.GetHeader("tag1");

            //varify
            Assert.That(actual,Is.EqualTo(expected));

        }

        [Test]
        public void Appendによるヘッダの追加() {
            //setUp
            var appned = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox });
            appned.Add(true, "tag2\tzzz");
            var sut = new ChangeHeader(null, appned);

            var mail = new Mail();

            var expected = "zzz";

            //exercise
            sut.Exec(mail, null);
            var actual = mail.GetHeader("tag2");

            //varify
            Assert.That(actual, Is.EqualTo(expected));

        }
        
    
    }
}
