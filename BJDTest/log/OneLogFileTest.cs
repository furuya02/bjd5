using System;
using System.IO;
using System.Linq;
using Bjd.log;
using BjdTest.test;
using NUnit.Framework;

namespace BjdTest.log{
    internal class OneLogFileTest{

        //テンポラリディレクトリ名
        private const String TmpDir = "OneLogFileTest";

        //テンポラリのフォルダの削除
        //このクラスの最後に１度だけ実行される
        //個々のテストでは、例外終了等で完全に削除出来ないので、ここで最後にディレクトリごと削除する
        [TestFixtureTearDown]
        public static void AfterClass(){
            var dir = TestUtil.GetTmpDir(TmpDir);
            Directory.Delete(dir, true);
        }

        [Test]
        public void 一度disposeしたファイルに正常に追加できるかどうか(){

            //setUp

            var fileName = TestUtil.GetTmpPath(TmpDir);
            var sut = new OneLogFile(fileName);
            sut.Set("1");
            sut.Set("2");
            sut.Set("3");
            //いったんクローズする
            sut.Dispose();

            //同一のファイルを再度開いてさらに３行追加
            sut = new OneLogFile(fileName);
            sut.Set("4");
            sut.Set("5");
            sut.Set("6");
            sut.Dispose();

            const int expected = 6;

            //exercise
            var actual = File.ReadAllLines(fileName).Count();

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}

