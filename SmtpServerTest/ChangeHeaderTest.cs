using System.Text;
using Bjd.ctrl;
using Bjd.log;
using Bjd.mail;
using Bjd.option;
using Bjd.util;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {


    class ChangeHeaderTest {
        [Test]
        public void Relpaceによるヘッダの置き換え(){
            //setUp
            var replace = new Dat(new CtrlType[]{CtrlType.TextBox, CtrlType.TextBox});
            replace.Add(true, "ABC\tXYZ");
            var sut = new ChangeHeader(replace, null);

            var mail = new Mail();
            mail.AddHeader("tag1", "ABC123");
            mail.AddHeader("tag2", "DEF123");
            mail.AddHeader("tag3", "GHI123");

            var expected = "tag1: XYZ123\r\n";

            //exercise
            sut.Exec(mail, new Logger());
            var actual = Encoding.ASCII.GetString(mail.GetBytes()).Substring(0, 14);

            //varify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void Relpaceによるヘッダの置き換え2() {
            //setUp
            var replace = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox });
            replace.Add(true, "ABC\tBBB");
            var sut = new ChangeHeader(replace, null);

            var mail = new Mail();
            mail.AddHeader("tag1", "ABC123");
            mail.AddHeader("tag2", "DEF123");
            mail.AddHeader("tag3", "GHI123");

            var expected = "BBB123";

            //exercise
            sut.Exec(mail, new Logger());
            var actual = mail.GetHeader("tag1");

            //varify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void Relpaceによるヘッダの置き換え3() {
            //setUp
            var replace = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox });
            replace.Add(true, "EFGH\tWXYZ");
            var sut = new ChangeHeader(replace, null);

            var mail = new Mail();
            mail.AddHeader("To", "\"ABCD\" <****@******>");
            mail.AddHeader("From", "\"EFGH\" <****@******>");
            mail.AddHeader("Subject", "test");

            var expected = "\"WXYZ\" <****@******>";

            //exercise
            sut.Exec(mail, new Logger());
            var actual = mail.GetHeader("From");

            //varify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void Relpaceによるヘッダの置き換え4() {
            //setUp
            var replace = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox });
            replace.Add(true, "User-Agent:.*\tUser-Agent:Henteko Mailer 09.87.12");
            var sut = new ChangeHeader(replace, null);

            var mail = new Mail();
            mail.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 5.1; rv:17.0) Gecko/20130801 Thunderbird/17.0.8");

            var expected = "Henteko Mailer 09.87.12";

            //exercise
            sut.Exec(mail, new Logger());
            var actual = mail.GetHeader("User-Agent");

            //varify
            Assert.That(actual, Is.EqualTo(expected));

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
            sut.Exec(mail, new Logger());
            var actual = mail.GetHeader("tag2");

            //varify
            Assert.That(actual, Is.EqualTo(expected));

        }
        
    
    }
}
