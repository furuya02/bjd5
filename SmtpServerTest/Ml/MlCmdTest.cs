using System.Linq;
using System.Text;
using Bjd;
using Bjd.mail;
using NUnit.Framework;
using SmtpServer;


namespace SmtpServerTest {
    [TestFixture]
    internal class MlCmdTest{
        
        private MlOneUser _user1;

        [SetUp]
        public void SetUp(){
            _user1 = new MlOneUser(true, "USER1", new MailAddress("user1@example.com"), false, true, true, "password");
        }

        [TearDown]
        public void TearDown(){}

        //１行コマンド
        [TestCase("   get 3",MlCmdKind.Get,"3")]//余分な空白を含む
        [TestCase("   get 3    ", MlCmdKind.Get, "3")]//余分な空白を含む
        [TestCase("GET 3", MlCmdKind.Get, "3")]
        [TestCase("GeT 3", MlCmdKind.Get, "3")]
        [TestCase("geT 3", MlCmdKind.Get, "3")]
        [TestCase("get 3", MlCmdKind.Get, "3")]
        [TestCase("get 3-10", MlCmdKind.Get, "3-10")]
        [TestCase("get", MlCmdKind.Get, "")]
        [TestCase("add", MlCmdKind.Add, "")]
        public void Test(string cmdStr, MlCmdKind mlCmdKind, string paramStr) {
            var mail = new Mail(null);
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            mail.Init(Encoding.ASCII.GetBytes(cmdStr));//区切り行(ヘッダ終了)
            var mlCmd = new MlCmd(null, mail, _user1);

            Assert.AreEqual(mlCmd.Cast<object>().Count(),1); // コマンド数は１
            
            foreach (OneMlCmd oneMlCmd in mlCmd) {
                Assert.AreEqual(oneMlCmd.CmdKind,mlCmdKind);
                Assert.AreEqual(oneMlCmd.ParamStr,paramStr);
                break;
            }
        }
        //複数行コマンド
        [TestCase("get 3\r\nadd\r\nmember",3)]
        [TestCase("get 3\r\n\r\nmember", 2)]//空行を含む
        [TestCase("\r\n\r\n\r\n\r\nmember", 1)]//空行を含む
        public void Test(string cmdStr, int count) {
            var mail = new Mail(null);
            mail.Init(Encoding.ASCII.GetBytes("\r\n"));//区切り行(ヘッダ終了)
            mail.Init(Encoding.ASCII.GetBytes(cmdStr));//区切り行(ヘッダ終了)
            var mlCmd = new MlCmd(null, mail, _user1);

            Assert.AreEqual(mlCmd.Cast<object>().Count(),count); // コマンド数
        }
    }

}
