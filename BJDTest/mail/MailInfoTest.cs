using System.Linq;
using Bjd.mail;
using NUnit.Framework;
using System.IO;

namespace BjdTest.mail {
    
    class MailInfoTest{
        
        private string _dfFile;

        [SetUp]
        public void SetUp() {
            const string srcDir = "C:\\tmp2\\bjd5\\BJDTest";
            //テンポラリテストデータの準備
            //ファイルの内容が変更されるので、テンポラリファイルで作業する
            var src = string.Format("{0}\\DF_MailInfoTest.dat", srcDir);
            _dfFile = string.Format("{0}\\$$$", srcDir);
            File.Copy(src, _dfFile);
        }

        [TearDown]
        public void TearDown() {
            //テンポラリテストデータの削除
            File.Delete(_dfFile);//テンポラリ削除
        }

        [TestCase("Date", "Sat, 28 Apr 2012 14:16:34 +0900")]
        [TestCase("From", "sin@comco.ne.jp")]
        [TestCase("Host", "win7-201108")]
        //[TestCase("Name", "MailInfoTest.dat")]
        [TestCase("RetryCounter", "0")]
        [TestCase("Size", "310")]
        [TestCase("To", "user1@example.com")]
        [TestCase("Uid", "bjd.00634712193942765633.000")]
        [TestCase("Addr", "127.0.0.1")]
        public void プロパティによる値取得(string tag, string expected) {
			//setUp
            var sut = new MailInfo(_dfFile);
            //exercise
            var actual = sut.GetType().GetProperty(tag).GetValue(sut, null).ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        
        [TestCase(0,true)]
        [TestCase(100, true)]
        public void IsProcessにより処理対象かどうかを判断する(double threadSpan, bool expected) {
            //setUp
            var sut = new MailInfo(_dfFile);
            //exercise
            var actual = sut.IsProcess(threadSpan,_dfFile);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void Saveによる保存() {

            //setUp
            var tmpFile = Path.GetTempFileName();
            var sut = new MailInfo(_dfFile);
            //exercise
            sut.Save(tmpFile);

            //verify
            var src = File.ReadAllLines(_dfFile);
            var dst = File.ReadAllLines(tmpFile);
            Assert.AreEqual(src.Count(), dst.Count());
            for (int i = 0; i < src.Count(); i++) {
                Assert.AreEqual(src[i], dst[i]);
            }

            //tearDown
            File.Delete(tmpFile);
        }

        [Test]
        public void ToStringによる文字列化() {
            //setUp
            var sut = new MailInfo(_dfFile);
            var expected = "from:sin@comco.ne.jp to:user1@example.com size:310 uid:bjd.00634712193942765633.000";
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void パラメータ指定によるコンストラクタの動作確認() {
            //setUp
            var a = new MailInfo(_dfFile);
            //var sut = new MailInfo(a.Uid, a.Size, a.Host, a.Addr, a.Date,a.From, a.To);
            var sut = new MailInfo(a.Uid, a.Size, a.Host, a.Addr, a.From, a.To);
            var expected = "from:sin@comco.ne.jp to:user1@example.com size:310 uid:bjd.00634712193942765633.000";
            //exercise
            var actual = sut.ToString();
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

    }

}
