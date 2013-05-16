using Bjd.util;
using NUnit.Framework;
using System;
using System.Text;

namespace BjdTest.util{
    internal class InetTest{
        //バイナリ-文字列変換
        [TestCase("本日は晴天なり", "2c67e5656f30746629596a308a30")]
        [TestCase("12345", "31003200330034003500")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void GetBytesTest(string str, string byteStr){
            var bytes = Inet.ToBytes(str);

            var sb = new StringBuilder(bytes.Length*2);
            foreach (byte b in bytes){
                if (b < 16) sb.Append('0'); // 二桁になるよう0を追加
                sb.Append(Convert.ToString(b, 16));
            }
            Assert.AreEqual(sb.ToString(), byteStr);
        }

        //バイナリ-文字列変換
        [TestCase("本日は晴天なり", "2c67e5656f30746629596a308a30")]
        [TestCase("12345", "31003200330034003500")]
        [TestCase("", "")]
        [TestCase("", null)]
        public void GetStringTest(string str, string byteStr){
            if (byteStr == null){
                Assert.AreEqual(Inet.FromBytes(null), str);
            } else{
                var length = byteStr.Length/2;
                var bytes = new byte[length];
                int j = 0;
                for (int i = 0; i < length; i++){
                    bytes[i] = Convert.ToByte(byteStr.Substring(j, 2), 16);
                    j += 2;
                }
                Assert.AreEqual(Inet.FromBytes(bytes), str);
            }
        }

        [TestCase("1\r\n2\r\n3", 3)]
        [TestCase("1\r\n2\r\n3\r\n", 4)]
        [TestCase("1\n2\n3", 1)]
        [TestCase("", 1)]
        [TestCase("\r\n", 2)]
        public void GetLinesTest(string str, int count){
            var lines = Inet.GetLines(str);
            Assert.AreEqual(lines.Count, count);
        }


        [TestCase(new byte[]{0x62, 0x0d, 0x0a, 0x62, 0x0d, 0x0a, 0x62}, 3)]
        [TestCase(new byte[]{0x62, 0x0d, 0x0a, 0x62, 0x0d, 0x0a, 0x62, 0x0d, 0x0a}, 3)]
        [TestCase(new byte[]{0x62, 0x0d, 0x0a}, 1)]
        [TestCase(new byte[]{0x0d, 0x0a}, 1)]
        [TestCase(new byte[]{}, 0)]
        [TestCase(null, 0)]
        public void GetLinesTest(byte[] buf, int count){
            var lines = Inet.GetLines(buf);
            Assert.AreEqual(lines.Count, count);
        }

        [TestCase("1", "1")]
        [TestCase("1\r\n", "1")]
        [TestCase("1\r", "1\r")]
        [TestCase("1\n", "1")]
        [TestCase("1\n2\n", "1\n2")]
        public void TrimCrlfTest(String str, String expanded){
            Assert.AreEqual(Inet.TrimCrlf(str), expanded);
        }

        [TestCase(new byte[]{0x64}, new byte[]{0x64})]
        [TestCase(new byte[]{0x64, 0x0d, 0x0a}, new byte[]{0x64})]
        [TestCase(new byte[]{0x64, 0x0d}, new byte[]{0x64, 0x0d})]
        [TestCase(new byte[]{0x64, 0x0a}, new byte[]{0x64})]
        [TestCase(new byte[]{0x64, 0x0a, 0x65, 0x0a}, new byte[]{0x64, 0x0a, 0x65})]
        public void trimCrlf_byte配列(byte[] buf, byte[] expended){
            var actual = Inet.TrimCrlf(buf);
            Assert.AreEqual(actual.Length, expended.Length);
            for (int i = 0; i < actual.Length; i++){
                Assert.AreEqual(actual[i], expended[i]);
            }
        }

        [TestCase("<HTML>", "&lt;HTML&gt;")]
        [TestCase("R&B", "R&amp;B")]
        [TestCase("123~", "123%7E")]
        public void サニタイズ処理(String str, String expended){
            var actual = Inet.Sanitize(str);
            Assert.AreEqual(actual, expended);
        }

        [TestCase("<HTML>", "BE-90-72-8C-11-BF-70-8F-52-50-28-A6-78-0F-8E-17")]
        [TestCase("abc", "90-01-50-98-3C-D2-4F-B0-D6-96-3F-7D-28-E1-7F-72")]
        [TestCase("", "D4-1D-8C-D9-8F-00-B2-04-E9-80-09-98-EC-F8-42-7E")]
        [TestCase(null, "")]
        public void MD5ハッシュ文字列(String str, String expended){
            var actual = Inet.Md5Str(str);
            Assert.AreEqual(actual, expended);
        }
    }
}