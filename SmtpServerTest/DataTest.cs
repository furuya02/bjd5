using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.sock;
using Bjd.util;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class DataTest {
        [Test]
        public void Appendでメール受信の完了時にFinishが返される(){
            //setUp
            const int sizeLimit = 1000;
            var sut = new Data(sizeLimit);
            var expected = RecvStatus.Finish;
            //exercise
            var actual = sut.Append(Encoding.ASCII.GetBytes("1:1\r\n\r\n.\r\n"));//<CL><CR>.<CL><CR>
            //verify
            Assert.That(actual,Is.EqualTo(expected));
        }
        [Test]
        public void Appendでドットのみの行を受信() {
            //setUp
            const int sizeLimit = 1000;
            var sut = new Data(sizeLimit);
            var expected = RecvStatus.Continue;
            //exercise
            var actual = sut.Append(Encoding.ASCII.GetBytes("1:1\r\n\r\n..\r\n"));//<CL><CR>..<CL><CR>
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Appendでドットで始まる行の確認() {
            //setUp
            const int sizeLimit = 1000;
            var sut = new Data(sizeLimit);
            var expected = ".htaccess\r\n";
            //exercise
            sut.Append(Encoding.ASCII.GetBytes("1:1\r\n\r\n..htaccess\r\n.\r\n"));//>.htaccess
            var lines = Inet.GetLines(sut.Mail.GetBody());
            var actual = Encoding.ASCII.GetString(lines[0]);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Appendでドットのみの行の確認() {
            //setUp
            const int sizeLimit = 1000;
            var sut = new Data(sizeLimit);
            var expected = ".\r\n";
            //exercise
            sut.Append(Encoding.ASCII.GetBytes("1:1\r\n\r\n..\r\n.\r\n"));//>.htaccess
            var lines = Inet.GetLines(sut.Mail.GetBody());
            var actual = Encoding.ASCII.GetString(lines[0]);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void Appendでドットを含む行の受信() {
            //setUp
            const int sizeLimit = 1000;
            var sut = new Data(sizeLimit);
            var expected = RecvStatus.Continue;
            //exercise
            var actual = sut.Append(Encoding.ASCII.GetBytes("123.\r\n"));//.<CL><CR>
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
        [Test]
        public void Appendでメール受信中にContinueが返される() {
            //setUp
            const int sizeLimit = 1000;
            var sut = new Data(sizeLimit);
            var expected = RecvStatus.Continue;
            //exercise
            var actual = sut.Append(Encoding.ASCII.GetBytes("1:1\r\n\r\n."));
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
        [TestCase(1,1023, RecvStatus.Continue)] //1Kbyte制限
        [TestCase(1, 1024, RecvStatus.Limit)]//1Kbyte制限
        [TestCase(1, 1025, RecvStatus.Limit)]//1Kbyte制限
        [TestCase(0, 2048, RecvStatus.Continue)] //制限なし
        public void Appendでサイズ制限を超えるとContinueが返される(int limit, int size, RecvStatus recvStatus) {
            //setUp
            var sut = new Data(limit);
            var expected = recvStatus;
            //exercise
            var actual = sut.Append(new byte[size]);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
