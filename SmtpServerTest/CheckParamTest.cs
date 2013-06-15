using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest
{
    internal class CheckParamTest{

        List<string> CreateParam(String str){
            var tmp = str.Split(' ');
            return tmp.ToList();
        }

        [TestCase("FROM <1@1>")]
        [TestCase("FROM \"<1@1>\"")]
        [TestCase("FROM 1@1")]
        public void Mailコマンドのチェック_正常(String str){
            //setUp
            const bool useNullFrom = false;
            const bool useNullDomain = false;
            var sut = new CheckParam(useNullFrom, useNullDomain);
            var paramList = CreateParam(str);

            var expected = true;

            //exercise
            var actual = sut.Mail(paramList);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [TestCase("From")]
        [TestCase("XXX")]
        [TestCase("")]
        public void Mailコマンドのチェック_異常(String str){
            //setUp
            const bool useNullFrom = false;
            const bool useNullDomain = false;
            var sut = new CheckParam(useNullFrom, useNullDomain);
            var paramList = CreateParam(str);

            var expected = "501 Syntax error in parameters scanning \"\"";

            //exercise
            sut.Mail(paramList);
            var actual = sut.Message;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("user")]
        [TestCase("user@")]
        public void Mailコマンドのチェック_異常_ドメイン名なしを許容しない(String mailAddress){
            //setUp
            const bool useNullFrom = false;
            const bool useNullDomain = false; //ドメイン名なしを許容しない
            var sut = new CheckParam(useNullFrom, useNullDomain);
            var paramList = CreateParam("From " + mailAddress);

            var expected = String.Format("553 {0}... Domain part missing", mailAddress);

            //exercise
            sut.Mail(paramList);
            var actual = sut.Message;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


    }
}
