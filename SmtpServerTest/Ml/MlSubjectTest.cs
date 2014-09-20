using NUnit.Framework;
using SmtpServer;


namespace SmtpServerTest {
    
    [TestFixture]
    class MlSubjectTest {
        

  //      readonly List<string> _domainList = new List<string>() { "example.com" };


        [SetUp]
        public void SetUp(){
        }

        [TearDown]
        public void TearDown() {
        }

        [TestCase(100, "本日は晴天なり", "1ban", 0, "(1ban) 本日は晴天なり")]
        [TestCase(100, "本日は晴天なり", "1ban", 1, "[1ban] 本日は晴天なり")]
        [TestCase(100, "本日は晴天なり", "1ban", 2, "(00100) 本日は晴天なり")]
        [TestCase(100, "本日は晴天なり", "1ban", 3, "[00100] 本日は晴天なり")]
        [TestCase(100, "本日は晴天なり", "1ban", 4, "(1ban:00100) 本日は晴天なり")]
        [TestCase(100, "本日は晴天なり", "1ban", 5, "[1ban:00100] 本日は晴天なり")]
        [TestCase(100, "本日は晴天なり", "1ban", 6, " 本日は晴天なり")]
        public void Get2Test(int no, string subject, string mlName, int kind, string ansStr) {
            var mlSubject = new MlSubject(kind,mlName);
            //連番を付加したSubjectの生成
            Assert.AreEqual(ansStr, mlSubject.Get(subject, no));
        }


        [TestCase(100)]
        [TestCase(100000)]
        [TestCase(1000000000)]
        [TestCase(0)]
        public void GetTest(int no) {
            const string mlName = "1ban";   
 
            for(var kind =0 ; kind<7 ; kind++){
                var mlSubject = new MlSubject(kind,mlName);
                var s = mlSubject.Get(no);
                switch(kind){
                    case 0: Assert.AreEqual(s,string.Format("({0})",mlName));
                            break;
                    case 1: Assert.AreEqual(s,string.Format("[{0}]",mlName));
                            break;
                    case 2: Assert.AreEqual(s,string.Format("({0:D5})",no));
                            break;
                    case 3: Assert.AreEqual(s,string.Format("[{0:D5}]",no));
                            break;
                    case 4: Assert.AreEqual(s,string.Format("({0}:{1:D5})",mlName,no));
                            break;
                    case 5: Assert.AreEqual(s,string.Format("[{0}:{1:D5}]",mlName,no));
                            break;
                    case 6: Assert.AreEqual(s,string.Format(""));
                            break;
                }
            }
        }
        
    }
}
