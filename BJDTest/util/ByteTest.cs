using System.Text;
using Bjd.util;
using NUnit.Framework;

namespace BjdTest.util{
    [TestFixture]
    public class BytesTest{

        //*****************************************************
        //各テストクラスで共通に使用される元データの作成クラス
        //*****************************************************
        //@Ignore("テストから除外される　-各テストクラスで共通に使用される元データの作成クラス")  //テストから除外
        public static class Data{
            public static byte[] Generate(){
                const int max = 100;
                var dmy = new byte[max];
                for (byte i = 0; i < max; i++){
                    dmy[i] = i;
                }
                const byte b = 1;
                const short a1 = 2;
                const int a2 = 3;
                const long a3 = 4L;
                const string s = "123";
                return Bytes.Create(dmy, b, a1, a2, a3, s, dmy);
            }
        }



        [TestCase(100, (byte) 1)]
        [TestCase(100 + 1, (byte) 2)]
        [TestCase(100 + 1 + 2, (byte) 3)]
        [TestCase(100 + 1 + 2 + 4, (byte) 4)]
        [TestCase(100 + 1 + 2 + 4 + 8 + 0, (byte) '1')]
        [TestCase(100 + 1 + 2 + 4 + 8 + 1, (byte) '2')]
        [TestCase(100 + 1 + 2 + 4 + 8 + 2, (byte) '3')]
        [TestCase(100 + 1 + 2 + 4 + 8 + 3, (byte) 0)]
        public void Bytes_create_offset番目のデータの確認(int offset, byte expected){
            //setUp
            var data = Data.Generate();
            //exercise
            var actual = data[offset];
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [TestCase(0, 49)] //１つ目dmyの中に存在する
        [TestCase(50, 100 + 1 + 2 + 4 + 8)]
        [TestCase(100, 100 + 1 + 2 + 4 + 8)]
        [TestCase(150, 167)] //2つ目dmyの中に存在する
        [TestCase(200, -1)] //存在しない
        public void Bytes_search指定したoffset以降で123が出現する位置を検索する(int offset, int expected){
            //setUp
            var data = Data.Generate();
            var src = Encoding.ASCII.GetBytes("123");
            //exercise
            var actual = Bytes.IndexOf(data, offset, src);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}

  