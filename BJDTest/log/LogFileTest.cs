using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Bjd.log;
using BjdTest.test;
using NUnit.Framework;

namespace BjdTest.log {
    class LogFileTest{
        //テンポラリディレクトリ名
        const String TmpDir = "LogFileTest";

        //テンポラリのフォルダの削除
        //このクラスの最後に１度だけ実行される
        // 個々のテストでは、例外終了等で完全に削除出来ないので、ここで最後にディレクトリごと削除する
        //[TearDown]
        [TestFixtureTearDown]
        public static void AfterClass(){
            var dir = TestUtil.GetTmpDir(TmpDir);
            Directory.Delete(dir, true);
        }

        [Test]
        public void ログの種類日別で予想されたパターンのファイルが２つ生成される(){

            const int logKind = 0; //通常ログの種類
            const string pattern = "*.????.??.??.log";

            //setUp
            var dir = TestUtil.GetTmpPath(TmpDir);
            Directory.CreateDirectory(dir);
            var sut = new LogFile(dir, logKind, logKind, 0,true);

            const int expected = 2;

            //exercise
            var actual = Directory.GetFiles(dir,pattern).Count();

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            sut.Dispose();

        }

        [Test]
        public void ログの種類月別で予想されたパターンのファイルが２つ生成される(){

            const int logKind = 1; //通常ログの種類
            const string pattern = "*.????.??.log";

            //setUp
            var dir = TestUtil.GetTmpPath(TmpDir);
            Directory.CreateDirectory(dir);
            var sut = new LogFile(dir, logKind, logKind, 0,true);

            const int expected = 2;

            //exercise
            var actual = Directory.GetFiles(dir,pattern).Count();

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            sut.Dispose();

        }

        [Test]
        public void ログの種類固定で予想されたパターンのファイルが２つ生成される(){

            const int logKind = 2; //固定ログの種類
            const string pattern = "*.Log";

            //setUp
            var dir = TestUtil.GetTmpPath(TmpDir);
            Directory.CreateDirectory(dir);
            var sut = new LogFile(dir, logKind, logKind, 0,true);

            const int expected = 2;


            //exercise
            var actual = Directory.GetFiles(dir,pattern).Count();

            //verify
            Assert.That(actual, Is.EqualTo(expected));

            //tearDown
            sut.Dispose();

        }

        [Test]
        public void Appendで３行ログを追加すると通常ログが3行になる(){

            const int logKind = 2; //固定ログの種類
            const string fileName = "BlackJumboDog.Log";

            //setUp
            var dir = TestUtil.GetTmpPath(TmpDir);
            Directory.CreateDirectory(dir);
            var sut = new LogFile(dir, logKind, logKind, 0,true);
            sut.Append(
                new OneLog("2012/06/01 00:00:00\tDetail\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            sut.Append(
                new OneLog("2012/06/02 00:00:00\tError\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            sut.Append(
                new OneLog("2012/06/03 00:00:00\tSecure\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            sut.Dispose();

            const int expected = 3;

            //exercise
            var lines = File.ReadAllLines(String.Format("{0}\\{1}", dir, fileName));
            var actual = lines.Length;

            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void Appendで３行ログを追加するとセキュアログが1行になる(){

            const int logKind = 2; //固定ログの種類
            const string fileName = "secure.Log";

            //setUp
            var dir = TestUtil.GetTmpPath(TmpDir);
            Directory.CreateDirectory(dir);
            var sut = new LogFile(dir, logKind, logKind, 0,true);
            sut.Append(
                new OneLog("2012/06/01 00:00:00\tDetail\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            sut.Append(
                new OneLog("2012/06/02 00:00:00\tError\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            sut.Append(
                new OneLog("2012/06/03 00:00:00\tSecure\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            sut.Dispose();

            const int expected = 1;

            //exercise
            var lines = File.ReadAllLines(String.Format("{0}\\{1}", dir, fileName));
            
            var actual = lines.Length;

            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }
        
        [Test]
        public void 過去7日分のログを準備して本日からsaveDaysでtailする(){

            //setUp
            var dir = TestUtil.GetTmpPath(TmpDir);
            Directory.CreateDirectory(dir);

            //2012/09/01~7日分のログを準備
            var logFile = new LogFile(dir, 2, 2, 0,true); //最初は、保存期間指定なしで起動する
            logFile.Append(
                new OneLog("2012/09/01 00:00:00\tDetail\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            logFile.Append(
                new OneLog("2012/09/02 00:00:00\tError\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            logFile.Append(
                new OneLog("2012/09/03 00:00:00\tSecure\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            logFile.Append(
                new OneLog("2012/09/04 00:00:00\tSecure\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            logFile.Append(
                new OneLog("2012/09/05 00:00:00\tSecure\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            logFile.Append(
                new OneLog("2012/09/06 00:00:00\tSecure\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            logFile.Append(
                new OneLog("2012/09/07 00:00:00\tSecure\t3208\tWeb-localhost:88\t127.0.0.1\t0000018\texecute\tramapater"));
            logFile.Dispose();

            const int expected = 2;

            //exercise
            //リフレクションを使用してprivateメソッドにアクセスする
            var cls = logFile;  
            var type = cls.GetType();
            var methodInfo = type.GetMethod("Tail", BindingFlags.NonPublic | BindingFlags.Instance);  

            var dt = new DateTime(2012, 9, 7);//2012.9.7に設定する
            var path = string.Format("{0}\\BlackJumboDog.Log", dir);
            const int saveDays = 2; //保存期間２日
            methodInfo.Invoke(cls, new object[] { path, saveDays, dt });  

            var actual = File.ReadAllLines(path).Length;

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
