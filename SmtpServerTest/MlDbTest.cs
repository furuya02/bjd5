using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Linq;
using SmtpServer;
using Bjd;
using BjdTest;
using System.IO;

namespace SmtpServerTest {
    [TestFixture]
    class MlDbTest {
        
        Kernel kernel;
        TsDir tsDir;
        Logger logger;
        string dir;
        
        [SetUp]
        public void SetUp() {
            kernel = new Kernel(null, null, null, null);
            tsDir = new TsDir();
            logger = new Logger(kernel, "LOG", false, null);
            dir = string.Format("{0}\\TestDir", tsDir.Src);
            
        }

        [TearDown]
        public void TearDown() {
        }

        [TestCase()]
        public void SaveRead_Test() {
            var mail = new Mail(logger);
            var mlName = "1ban";
            MlDb mlDb = new MlDb(logger, dir,mlName);
            mlDb.Remove();//もし、以前のメールが残っていたらTESTが誤動作するので、ここで消しておく

            Assert.AreEqual(mlDb.Count(), 0);
            
            var max = 10;//試験件数10件
            //保存と、
            for (int i = 0; i < max; i++) {
                var b = mlDb.Save( mail);
                Assert.AreEqual(b,true);//保存が成功しているか
                Assert.AreEqual(mlDb.Count(), i+1);//連番がインクリメントしているか
            }
            //範囲外のメール取得でnullが返るか
            //no==1..10が取得可能
            var m = mlDb.Read(0);//範囲外
            Assert.IsNull(m);
            //範囲内
            for (int no = 1; no <= max; no++) {
                m = mlDb.Read(no);
                Assert.NotNull(mlDb.Read(no));
            }
            //範囲外
            m = mlDb.Read(11);
            Assert.IsNull(m);


            mlDb.Remove();
        }
        
        //コンストラクタ
        [TestCase("TestDir",true,true)]//存在するフォルダを指定すると、Status=trueとなる
        [TestCase("$$$$",false,true)]  //存在しないフォルダを指定すると、フォルダが作成され、Status=trueとなる
        [TestCase("???", false,false)]  //作成できないフォルダを指定すると、Status=falseとなる
        public void Cst_Test(string folder, bool exists,bool status) {
            //Testプロジェクトの下に、TEST用フォルダを作成する
            string dir = string.Format("{0}\\{1}", tsDir.Src,folder);
            if (!exists){//存在しないフォルダをTESTする場合は、フォルダをあらかじめ削除してお
                if (Directory.Exists(dir)) {
                    Directory.Delete(dir,true);
                }
            }
            string mlName = "2ban";
            MlDb mlDb = new MlDb(null, dir,mlName);//コンストラクタ
            Assert.AreEqual(mlDb.Status, status);//初期化成功
            mlDb.Remove();
            
            if (!exists) {//存在しないフォルダをTESTする場合は、最後にフォルダを削除しておく
                if (Directory.Exists(dir)) {
                    Directory.Delete(dir, true);
                }
            }
        }
    }
}
