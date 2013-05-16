using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bjd.util;
using NUnit.Framework;

namespace BjdTest.util{

    public class MLangTest{

        [Test]
        public void getEncoding及びgetstringの確認(){
            //setUp
            string str = "あいうえお";
            string[] charsetList = new[] { "utf-8", "euc-jp", "iso-2022-jp", "shift_jis" };

            //verify
            foreach (string charset in charsetList){
                byte[] bytes = Encoding.GetEncoding(charset).GetBytes(str);
                Assert.That(MLang.GetEncoding(bytes).WebName, Is.EqualTo(charset));
                Assert.That(MLang.GetString(bytes), Is.EqualTo(str));
            }
        }

        [Test]
        public void getEncoding_fileName_の確認(){

            //setUp
            string tempFile = Path.GetTempFileName();
            //File tempFile = File.createTempFile("tmp", ".txt");
            List<string> lines = new List<string>();
            lines.Add("あいうえお");
            File.WriteAllLines(tempFile, lines);

            Encoding sut = MLang.GetEncoding(tempFile);
            string expected = "utf-8";
            //exercise
            string actual = sut.WebName;
            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //TearDown
            File.Delete(tempFile);
        }
    }
}