using System.IO;
using System.Threading;
using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    class FetchDbTest{

        [SetUp]
        public void SetUp(){

        }

        [Test]
        public void IndexOfによる検索(){
            var dir = Path.GetTempPath();
            //setUp
            var sut = new FetchDb(dir,"TEST");
            sut.Add("0-1234567890");
            sut.Add("1-1234567890");
            var expected = 1;
            //exercise
            var actual = sut.IndexOf("1-1234567890");
            //verify
            Assert.That(actual,Is.EqualTo(expected));
            
            //tearDown
            File.Delete(sut.FileName);
        }

        [Test]
        public void IndexOfによる検索_存在しない場合() {
            var dir = Path.GetTempPath();
            //setUp
            var sut = new FetchDb(dir, "TEST");
            sut.Add("0-1234567890");
            sut.Add("1-1234567890");
            var expected = -1;
            //exercise
            var actual = sut.IndexOf("2-1234567890");
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            File.Delete(sut.FileName);
        }

        [Test]
        public void Saveによる保存() {
            var dir = Path.GetTempPath();
            //setUp
            //いったん保存された状態を作る
            var dmy = new FetchDb(dir, "TEST");
            dmy.Add("0-1234567890");
            dmy.Add("1-1234567890");
            dmy.Save();
            //改めて読み込む
            var sut = new FetchDb(dir, "TEST");
            var expected = 0;
            //exercise
            //存在するかどうか検索してみる
            var actual = sut.IndexOf("0-1234567890");
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            File.Delete(sut.FileName);
        }

        [Test]
        public void Delによる削除() {
            var dir = Path.GetTempPath();
            //setUp
            //いったん保存された状態を作る
            var sut = new FetchDb(dir, "TEST");
            sut.Add("0-1234567890");
            sut.Add("1-1234567890");
            var expected = true;

            //exercise
            //存在するデータの削除
            var actual = sut.Del("0-1234567890");
            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //存在するかどうか検索してみる(存在しない)
            Assert.That(sut.IndexOf("0-1234567890"), Is.EqualTo(-1));

            //tearDown
            File.Delete(sut.FileName);
        }

        [Test]
        public void Delによる削除_失敗_存在しない() {
            var dir = Path.GetTempPath();
            //setUp
            //いったん保存された状態を作る
            var sut = new FetchDb(dir, "TEST");
            sut.Add("0-1234567890");
            sut.Add("1-1234567890");
            var expected = false;

            //exercise
            //存在するデータの削除
            var actual = sut.Del("XXX");
            //verify
            Assert.That(actual, Is.EqualTo(expected));
            //存在するかどうか検索してみる(存在する)
            Assert.That(sut.IndexOf("0-1234567890"), Is.EqualTo(0));

            //tearDown
            File.Delete(sut.FileName);
        }

        [Test]
        public void IsPastによる確認_指定時間を経過した() {
            var dir = Path.GetTempPath();
            //setUp
            //いったん保存された状態を作る
            var sut = new FetchDb(dir, "TEST");
            sut.Add("0-1234567890");
            Thread.Sleep(1100); //1.1秒経過
            var expected = true;

            //exercise
            var actual = sut.IsPast("0-1234567890",1);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            File.Delete(sut.FileName);
        }

        [Test]
        public void IsPastによる確認_指定時間を経過していない() {
            var dir = Path.GetTempPath();
            //setUp
            //いったん保存された状態を作る
            var sut = new FetchDb(dir, "TEST");
            sut.Add("0-1234567890");
            var expected = false;

            //exercise
            var actual = sut.IsPast("0-1234567890", 1);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            File.Delete(sut.FileName);
        }


        [Test]
        public void 複合試験() {
            var dir = Path.GetTempPath();
            
            //当初２件で作成
            var sut = new FetchDb(dir, "TEST");
            sut.Add("0-1234567890");
            sut.Add("1-1234567890");
            sut.Save();

            sut = new FetchDb(dir, "TEST");//改めて読み込む
            sut.Del("0-1234567890");//１件目削除
            sut.Add("2-1234567890");//追加
            sut.Save();

            sut = new FetchDb(dir, "TEST");//改めで読み込む

            //verify
            //最終的に２件が検索できるはず
            Assert.That(sut.IndexOf("0-1234567890"),Is.EqualTo(-1));
            Assert.That(sut.IndexOf("1-1234567890"), Is.EqualTo(0));
            Assert.That(sut.IndexOf("2-1234567890"), Is.EqualTo(1));

            
            //tearDown
            File.Delete(sut.FileName);
        }




    }
}
