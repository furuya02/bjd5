using Bjd.acl;
using Bjd.ctrl;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using BjdTest.test;
using NUnit.Framework;
using Bjd;


namespace BjdTest.acl{
    
    public class AclListTest{

        [TestCase("192.168.0.1", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.0.300", "192.168.0.1", AclKind.Deny)] //無効リスト
        [TestCase("192.168.0.0/24", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.1.0/24", "192.168.0.1", AclKind.Deny)]
        [TestCase("192.168.1.0/200", "192.168.1.0", AclKind.Deny)] //無効リスト
        [TestCase("192.168.0.0-192.168.0.100", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.0.2-192.168.0.100", "192.168.0.1", AclKind.Deny)]
        [TestCase("192.168.0.0-192.168.2.100", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.0.1-5", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.0.2-5", "192.168.0.1", AclKind.Deny)]
        [TestCase("192.168.0.*", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.1.*", "192.168.0.1", AclKind.Deny)]
        [TestCase("192.168.*.*", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.*.*.*", "192.168.0.1", AclKind.Allow)]
        [TestCase("*.*.*.*", "192.168.0.1", AclKind.Allow)]
        [TestCase("*", "192.168.0.1", AclKind.Allow)]
        [TestCase("xxx", "192.168.0.1", AclKind.Deny)] //無効リスト
        [TestCase("172.*.*.*", "192.168.0.1", AclKind.Deny)]
        public void enableNum_0で_のみを許可する_を検証する(string aclStr, string ipStr, AclKind expected){
            //setUp
			int enableNum = 0; //enableNum=0 のみを許可する
            Dat dat = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.AddressV4 });
            if (!dat.Add(true, string.Format("NAME\t{0}", aclStr))) {
                Assert.Fail("このエラーが発生したら、テストの実装に問題がある");
            }
            var ip = TestUtil.CreateIp(ipStr);
            AclList sut = new AclList(dat, enableNum, new Logger());

			//exercise
			AclKind actual = sut.Check(ip);
			//verify
			Assert.That(actual, Is.EqualTo(expected));
        }

                [TestCase("192.168.0.1", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.0.300", "192.168.0.1", AclKind.Deny)] //無効リスト
        [TestCase("192.168.0.0/24", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.1.0/24", "192.168.0.1", AclKind.Deny)]
        [TestCase("192.168.1.0/200", "192.168.1.0", AclKind.Deny)] //無効リスト
        [TestCase("192.168.0.0-192.168.0.100", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.0.2-192.168.0.100", "192.168.0.1", AclKind.Deny)]
        [TestCase("192.168.0.0-192.168.2.100", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.0.1-5", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.0.2-5", "192.168.0.1", AclKind.Deny)]
        [TestCase("192.168.0.*", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.168.1.*", "192.168.0.1", AclKind.Deny)]
        [TestCase("192.168.*.*", "192.168.0.1", AclKind.Allow)]
        [TestCase("192.*.*.*", "192.168.0.1", AclKind.Allow)]
        [TestCase("*.*.*.*", "192.168.0.1", AclKind.Allow)]
        [TestCase("*", "192.168.0.1", AclKind.Allow)]
        [TestCase("xxx", "192.168.0.1", AclKind.Deny)] //無効リスト
        [TestCase("172.*.*.*", "192.168.0.1", AclKind.Deny)]
        public void enableNum_1で_のみを禁止する_を検証する(string aclStr, string ipStr, AclKind ex){
            			//setUp
			//ACLは逆転する
			AclKind expected = (ex == AclKind.Allow) ? AclKind.Deny : AclKind.Allow;
			int enableNum = 1; //enableNum=1 のみを禁止する
            Dat dat = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.AddressV4 });
            if (!dat.Add(true, string.Format("NAME\t{0}", aclStr))) {
                Assert.Fail("このエラーが発生したら、テストの実装に問題がある");
            }
            var ip = TestUtil.CreateIp(ipStr);
            AclList sut = new AclList(dat, enableNum, new Logger());

			//exercise
			AclKind actual = sut.Check(ip);
			//verify
			Assert.That(actual, Is.EqualTo(expected));
        }

    }
}
