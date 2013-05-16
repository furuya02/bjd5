using System;
using Bjd;
using Bjd.log;
using NUnit.Framework;

namespace BjdTest.log{
    internal class OneLogTest{
        private const String NameTag = "NAME";
        private const long ThreadId = 100;
        private const String RemoteHostname = "127.0.0.1";
        private const int MessageId = 200;
        private const String Message = "MSG";
        private const String DetailInfomation = "DETAIL";

        private static DateTime GetDateTime(){
            return new DateTime(1970,1,1,9,0,0);
        }

        [Test]
        [ExpectedException(typeof (ValidObjException))]
        public void 無効な文字列で初期化すると例外_ValidObjException_が発生する(){
            //exercise
            new OneLog("xxx");
        }

        [Test]
        public void ToStringによる出力の確認(){
            //setUp
            var sut = new OneLog(GetDateTime(), LogKind.Debug, NameTag, ThreadId, RemoteHostname, MessageId, Message, DetailInfomation);
            const string expected = "1970/01/01 09:00:00\tDebug\t100\tNAME\t127.0.0.1\t0000200\tMSG\tDETAIL";
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void isSecureによる確認_LogKind_SECUREでtrueが返る(){

            //setUp
            const LogKind logKind = LogKind.Secure;
            const bool expected = true;
            var sut = new OneLog(GetDateTime(), logKind, NameTag, ThreadId, RemoteHostname, MessageId, Message, DetailInfomation);
            //exercise
            var actual = sut.IsSecure();
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void isSecureによる確認_LogKind_DEBUGでfalseが返る(){

            //setUp
            const LogKind logKind = LogKind.Debug;
            const bool expected = false;
            var sut = new OneLog(GetDateTime(), logKind, NameTag, ThreadId, RemoteHostname, MessageId, Message, DetailInfomation);
            //exercise
            var actual = sut.IsSecure();
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
    }
}