using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class RelayTest {

        [TestCase(0,true)] //許可リスト優先
        [TestCase(1, false)] //禁止リスト優先
        public void Orderによる制御をテストする(int order, bool isAllow) {
            //setUp
            var allowList = new Dat(new CtrlType[]{CtrlType.TextBox});
            allowList.Add(true, "192.168.0.0/24");
            var denyList = new Dat(new CtrlType[]{CtrlType.TextBox});
            denyList.Add(true, "192.0.0.0/8");

            var sut = new Relay(allowList,denyList,order,null);
            var expected = isAllow;
            //exercise
            var actual = sut.IsAllow(new Ip("192.168.0.1"));
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(0, false)] //許可リスト優先
        [TestCase(1, false)] //禁止リスト優先
        public void リストが空の場合(int order, bool isAllow) {
            //setUp
            var sut = new Relay(null,null, order, null);
            var expected = isAllow;
            //exercise
            var actual = sut.IsAllow(new Ip("192.168.0.1"));
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(0, true)] //許可リスト優先
        [TestCase(1, true)] //禁止リスト優先
        public void 許可リストだけの場合(int order, bool isAllow) {
            //setUp
            var allowList = new Dat(new CtrlType[] { CtrlType.TextBox });
            allowList.Add(true, "192.168.0.0/24");
            
            var sut = new Relay(allowList, null, order, null);
            var expected = isAllow;
            //exercise
            var actual = sut.IsAllow(new Ip("192.168.0.1"));
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

    }
}
