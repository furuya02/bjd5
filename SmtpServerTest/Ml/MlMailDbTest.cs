using Bjd.log;
using Bjd.mail;
using BjdTest.test;
using NUnit.Framework;
using SmtpServer;
using Bjd;
using BjdTest;
using System.IO;

namespace SmtpServerTest {
    [TestFixture]
    class MlMailDbTest {
        
        [Test]
        public void SaveReadTest(){
            var tmpDir = TestUtil.GetTmpDir("$tmp");
            var logger = new Logger();


            var mail = new Mail(logger);
            const string mlName = "1ban";
            var mlMailDb = new MlMailDb(logger, tmpDir, mlName);
            mlMailDb.Remove();//もし、以前のメールが残っていたらTESTが誤動作するので、ここで消しておく

            Assert.AreEqual(mlMailDb.Count(), 0);
            
            const int max = 10; //試験件数10件
            //保存と、
            for (int i = 0; i < max; i++) {
                var b = mlMailDb.Save( mail);
                Assert.AreEqual(b,true);//保存が成功しているか
                Assert.AreEqual(mlMailDb.Count(), i+1);//連番がインクリメントしているか
            }
            //範囲外のメール取得でnullが返るか
            //no==1..10が取得可能
            var m = mlMailDb.Read(0);//範囲外
            Assert.IsNull(m);
            //範囲内
            for (int no = 1; no <= max; no++) {
                //m = mlMailDb.Read(no);
                mlMailDb.Read(no);
                Assert.NotNull(mlMailDb.Read(no));
            }
            //範囲外
            m = mlMailDb.Read(11);
            Assert.IsNull(m);


            //TearDown
            mlMailDb.Remove();
            mlMailDb.Dispose();
            Directory.Delete(tmpDir,true);
        }
        
        //コンストラクタ
        [TestCase("TestDir",true,true)]//存在するフォルダを指定すると、Status=trueとなる
        [TestCase("$$$$",false,true)]  //存在しないフォルダを指定すると、フォルダが作成され、Status=trueとなる
        [TestCase("???", false,false)]  //作成できないフォルダを指定すると、Status=falseとなる
        public void CtorTest(string folder, bool exists,bool status) {
            //Testプロジェクトの下に、TEST用フォルダを作成する

            var dir = string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), folder);

            if (!exists){//存在しないフォルダをTESTする場合は、フォルダをあらかじめ削除してお
                if (Directory.Exists(dir)) {
                    Directory.Delete(dir,true);
                }
            }
            const string mlName = "2ban";
            var mlMailDb = new MlMailDb(null, dir,mlName);//コンストラクタ
            Assert.AreEqual(mlMailDb.Status, status);//初期化成功
            mlMailDb.Remove();
            
            if (!exists) {//存在しないフォルダをTESTする場合は、最後にフォルダを削除しておく
                if (Directory.Exists(dir)) {
                    Directory.Delete(dir, true);
                }
            }
            mlMailDb.Dispose();
        }
    }
}
