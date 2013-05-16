//using System.Windows.Forms;

using System;
using System.IO;
using BjdTest.test;
using NUnit.Framework;

using Bjd;

namespace BjdTest{
    /*[Test]
    public void TotalTest() {
        //var kernel = new Kernel(new MainForm(), null, null, null, null);

        const string dir = "c:\\";
        const string name = "TEST";
        var fileName = string.Format("{0}\\{1}.ini",dir, name);

        var reg = new Reg(dir);
        reg.Setstring("string", "value");
        reg.SetInt("Int", 123);

        Assert.AreEqual("value", reg.Getstring("string"));
        Assert.AreEqual(123, reg.GetInt("Int"));
        Assert.AreEqual("123", reg.Getstring("Int"));//SetIntで指定したものをGetstringで取得できるのは一応仕様です

        reg.SetInt("Int", 4);//上書き
        Assert.AreEqual(4, reg.GetInt("Int"));
        reg.Dispose();//保存

        //再読み込み
        var reg2 = new Reg(dir);
        Assert.AreEqual("value", reg2.Getstring("string"));
        Assert.AreEqual(4, reg2.GetInt("Int"));


        File.Delete(fileName);
    }*/
    [TestFixture]
    class RegTest{

        //テンポラリディレクトリ名
        private const string TmpDir = "RegTest";

        //テンポラリのフォルダの削除
        //このクラスの最後に１度だけ実行される
        //個々のテストでは、例外終了等で完全に削除出来ないので、ここで最後にディレクトリごと削除する
        [TearDown]
        public static void AfterClass(){
            //File file = new File(TestUtil.GetTmpDir(tmpDir));
            //Util.fileDelete(file);
            var dir = TestUtil.GetTmpDir(TmpDir);
            Directory.Delete(dir,true);
        }

        [Test]
        public void SetIntで保存した値をgetIntで読み出す(){

            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));
            sut.SetInt("key1", 1);
            var expected = 1;

            //exercise
            var actual = sut.GetInt("key1");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Setstringで保存した値をgetstringで読み出す(){

            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));
            sut.SetString("key2", "2");
            var expected = "2";

            //exercise
            var actual = sut.GetString("key2");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof (Exception))]
        public void GetIntで無効なkeyを指定すると例外が発生する(){

            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));

            //exercise
            sut.GetInt("key1");
        }

        [Test]
        [ExpectedException(typeof (Exception))]
        public void Getstringで無効なkeyを指定すると例外が発生する(){
            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));

            //exercise
            sut.GetString("key2");
        }

        [Test]
        [ExpectedException(typeof (Exception))]
        public void GetIntでKeyにnullを指定すると例外が発生する(){
            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));

            //exercise
            sut.GetInt(null);
        }

        [Test]
        [ExpectedException(typeof (Exception))]
        public void GetstringでKeyにnullを指定すると例外が発生する(){
            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));

            //exercise
            sut.GetString(null);
        }

        [Test]
        [ExpectedException(typeof (Exception))]
        public void SetIntでKeyにnullを指定すると例外が発生する(){
            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));

            //exercise
            sut.SetInt(null, 1);
        }

        [Test]
        public void SetIntでKeyにnullを指定して例外が発生しても元の値は破壊されない(){
            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));
            sut.SetInt("key1", 1); //元の値
            var expected = 1;

            try{
                sut.SetInt(null, 1);
            } catch (Exception){
                ; //nullを指定してsetIntすることで例外が発生する
            }

            //exercise
            var actual = sut.GetInt("key1");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof (Exception))]
        public void SetstringでKeyにnullを指定すると例外が発生する(){
            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));

            //exercise
            sut.SetString(null, "2");
        }

        [Test]
        public void SetstringでKeyにnullを指定して例外が発生しても元の値は破壊されない(){
            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));
            sut.SetString("key2", "2"); //元の値
            var expected = "2";

            try{
                sut.SetString(null, "3");
            } catch (Exception){
                ; //nullを指定してsetIntすることで例外が発生する
            }

            //exercise
            var actual = sut.GetString("key2");

            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void Setstringでvalにnullを指定すると空白が保存される(){
            //setUp
            var sut = new Reg(TestUtil.GetTmpPath(TmpDir));
            sut.SetString("key1", null);
            var expected = "";

            //exercise
            var actual = sut.GetString("key1");

            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
    }
}
