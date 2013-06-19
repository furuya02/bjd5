using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest.Fetch {
    class PopClientTest {
        [Test]
        public void AAA(){
            //setUp
            var sut = new PopClient();
            //exercise
            sut.Recv();

            //verify

        }
    }
}
