using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.server;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class SmtpCmdTest {

        Cmd CreateCmd(string str){
            var tmp = str.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            return new Cmd(str, tmp[0], tmp[1]);

        }
        [TestCase("Mail From: 1@1")]
        [TestCase("Mail From:1@1")]
        public void ParamListの確認(string str) {
            //setUp
            var sut = new SmtpCmd(CreateCmd(str));
            //exercise
            var actual = sut.ParamList;
            //verify
            Assert.That(actual[0],Is.EqualTo("From:"));
            Assert.That(actual[1], Is.EqualTo("1@1"));
        }

        [TestCase("Mail From: 1@1")]
        [TestCase("Mail From:1@1")]
        public void Kindの確認(string str) {
            //setUp
            var sut = new SmtpCmd(CreateCmd(str));
            var expected = SmtpCmdKind.Mail;
            //exercise
            var actual = sut.Kind;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
