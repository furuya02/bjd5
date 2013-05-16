using System.Text;
using Bjd.util;
using NUnit.Framework;
using Bjd;

namespace BjdTest {
    class HeaderTest {
        [Test]
        public void TotalTest() {
            const int max = 5;
            var sb = new StringBuilder();
            for (int i = 0; i < max; i++) {
                sb.Append(string.Format("key_{0:D3}: val_{0:D3}\r\n", i));
            }
            byte [] buf = Bytes.Create(sb.ToString());
            var header = new Header(buf);

            //GetBytes()
            //自動的に追加される空行を追加すると、初期化したbyte[]と同じになるはず
            var tmp = Bytes.Create(header.GetBytes(),"\r\n");
            for (int i=0;i<buf.Length;i++) {
                Assert.AreEqual(buf[i],tmp[i]);
            }

            //Count
            Assert.AreEqual(header.Count,max);


            for(var i=0;i<header.Count;i++){
                var key = string.Format("key_{0:D3}",i);
                var valStr = string.Format("val_{0:D3}",i);

                //GetVal(string key)
                Assert.AreEqual(header.GetVal(key), valStr);

                //Replace(string key,string str)
                var replaceStr = string.Format("replace_{0:D3}",i);
                header.Replace(key,replaceStr);
                Assert.AreEqual(header.GetVal(key), replaceStr);
            }

            const int appendMax = 3;
            for (int i = 0; i < appendMax; i++) {
                //Append(string key,string val)
                var key = string.Format("AppendKey_{0:D3}", i);
                var val = string.Format("AppendVal_{0:D3}", i);
                header.Append(key, Encoding.ASCII.GetBytes(val));
                //string s = header.GetVal(key);
                Assert.AreEqual(header.GetVal(key), val);
            }
            Assert.AreEqual(header.Count, max + appendMax);

        }
        
        
        //Recv のテスト
        /*[Test]
        public void RecvTest() {
            Header target = new Header(); // TODO: 適切な値に初期化してください
            sockTcp tcpObj = null; // TODO: 適切な値に初期化してください
            int timeout = 0; // TODO: 適切な値に初期化してください
            bool life = false; // TODO: 適切な値に初期化してください
            bool lifeExpected = false; // TODO: 適切な値に初期化してください
            bool expected = false; // TODO: 適切な値に初期化してください
            bool actual;
            actual = target.Recv(tcpObj, timeout, ref life);
            Assert.AreEqual(lifeExpected, life);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }
        */
    }
}
