using System;
using BjdTest;
using BjdTest.test;
using NUnit.Framework;
using WebServer;

namespace WebServerTest {
    [TestFixture]
    internal class ExecProcessTest{
        [SetUp]
        public void SetUp(){}

        [TearDown]
        public void TearDown(){}
        
        [TestCase(1000000, 1)] //1で1Mbyte
        [TestCase(256, 1)] //1で1Mbyte
        //[TestCase(1000000, 2000)] //1で1Mbyte 自作cat.exeでは200MByteまでしか対応できない
        public void StartTest(int block, int count) {
            var srcDir = string.Format("{0}\\WebServerTest", TestUtil.ProjectDirectory());

            //こちらの自作cat.exeでは、200Mbyteまでしか対応できていない
            var execProcess = new ExecProcess(string.Format("{0}\\cat.exe",srcDir), "", srcDir,null);

            var buf = new byte[block];
            for (var b = 0; b < block; b++) {
                buf[b] = (byte)b;
            }
            var inputStream = new WebStream(block*count);
            for (var i = 0; i < count; i++) {
                inputStream.Add(buf);
            }
            WebStream outputStream;
            execProcess.Start(inputStream,out outputStream);

            for (var i = 0; i < count; i++) {
                var len = outputStream.Read(buf, 0, buf.Length);
                Assert.AreEqual(len, block);
                if(i==0){
                    Assert.AreEqual(buf[0], 0);
                    Assert.AreEqual(buf[1], 1);
                    Assert.AreEqual(buf[2], 2);
                   
                }
            }
            
            outputStream.Dispose();
            inputStream.Dispose();
        }

    }
}
