using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.sock;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class DataTest {
        [Test]
        public void AAA(){
            //setUp
            SockTcp sockTcp = new SockTcp(new Kernel(),??? );
            const int sizeLimit = 1000;
            var sut = new Data(sizeLimit);
            var expected = 0;
            //exercise
            var actual = sut.Recv();
        }
    }
}
