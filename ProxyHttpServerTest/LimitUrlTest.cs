using Bjd.ctrl;
using Bjd.option;
using NUnit.Framework;
using ProxyHttpServer;

namespace ProxyHttpServerTest {
    
    public class LimitUrlTest {
        public enum LimitKind {
            Front = 0,//前方一致
            Rear = 1,//後方一致
            Part = 2,//部分一致
            Regular = 3//正規表現
        }
        
        Dat AddDat(Dat dat, LimitKind limitKind, string url) {
            dat.Add(true, string.Format("{0}\t{1}",url,(int)limitKind));
            return dat;
        }

        [TestCase("[[]/?*", LimitKind.Regular, "http://www.yahoo.com/", false)]//正規表現が無効で、初期化に失敗している
        [TestCase(".*", LimitKind.Regular, "http://www.yahoo.com/", true)]
        [TestCase(".*", LimitKind.Regular, "http://smtp.yahoo.com/", true)]
        [TestCase(".*", LimitKind.Regular, "http://www.goo.co.jp/", true)]
        [TestCase(".*", LimitKind.Regular, "http://www.yahoo.co.jp", true)]
        [TestCase(".com/", LimitKind.Rear, "http://www.yahoo.com/", true)]
        [TestCase(".com/", LimitKind.Rear, "http://smtp.yahoo.com/", true)]
        [TestCase(".com/", LimitKind.Rear, "http://www.goo.co.jp/", false)]
        [TestCase(".com/", LimitKind.Rear, "http://www.yahoo.co.jp", false)]
        [TestCase("yahoo.com", LimitKind.Part, "http://www.yahoo.com/", true)]
        [TestCase("yahoo.com", LimitKind.Part, "http://smtp.yahoo.com/", true)]
        [TestCase("yahoo.com", LimitKind.Part, "http://www.goo.co.jp/", false)]
        [TestCase("yahoo.com", LimitKind.Part, "http://www.yahoo.co.jp", false)]
        [TestCase("http://www.goo.com/", LimitKind.Front, "http://www.goo.com/", true)]
        [TestCase("http://www.goo.com/", LimitKind.Front, "http://www.goo.com/test", true)]
        [TestCase("http://www.goo.com/", LimitKind.Front, "http://www.go.co.jp/", false)]
        [TestCase("http://www.goo.com/", LimitKind.Front, "http://www.go.co", false)]
        public void AllowTest(string str, LimitKind limitKind, string target, bool isAllow) {
            var allow = new Dat(new[]{CtrlType.TextBox, CtrlType.Int });
            var deny = new Dat(new[] { CtrlType.TextBox, CtrlType.Int });
            allow = AddDat(allow, limitKind, str);
            var limitUrl = new LimitUrl(allow, deny);
            var errorStr = "";
            Assert.AreEqual(limitUrl.IsAllow(target, ref errorStr), isAllow);
        }

        [TestCase("[[]/?*", LimitKind.Regular, "http://www.yahoo.com/", true)]//正規表現が無効で、初期化に失敗している
        [TestCase(".*", LimitKind.Regular, "http://www.yahoo.com/", false)]
        [TestCase(".*", LimitKind.Regular, "http://smtp.yahoo.com/", false)]
        [TestCase(".*", LimitKind.Regular, "http://www.goo.co.jp/", false)]
        [TestCase(".*", LimitKind.Regular, "http://www.yahoo.co.jp", false)]
        [TestCase(".com/", LimitKind.Rear, "http://www.yahoo.com/", false)]
        [TestCase(".com/", LimitKind.Rear, "http://smtp.yahoo.com/", false)]
        [TestCase(".com/", LimitKind.Rear, "http://www.goo.co.jp/", true)]
        [TestCase(".com/", LimitKind.Rear, "http://www.yahoo.co.jp", true)]
        [TestCase("yahoo.com", LimitKind.Part, "http://www.yahoo.com/", false)]
        [TestCase("yahoo.com", LimitKind.Part, "http://smtp.yahoo.com/", false)]
        [TestCase("yahoo.com", LimitKind.Part, "http://www.goo.co.jp/", true)]
        [TestCase("yahoo.com", LimitKind.Part, "http://www.yahoo.co.jp", true)]
        [TestCase("http://www.goo.com/", LimitKind.Front, "http://www.goo.com/", false)]
        [TestCase("http://www.goo.com/", LimitKind.Front, "http://www.goo.com/test", false)]
        [TestCase("http://www.goo.com/", LimitKind.Front, "http://www.go.co.jp/", true)]
        [TestCase("http://www.goo.com/", LimitKind.Front, "http://www.go.co", true)]
        public void DenyTest(string str, LimitKind limitKind, string target, bool isAllow) {
            var allow = new Dat(new[] { CtrlType.TextBox, CtrlType.Int });
            var deny = new Dat(new[] { CtrlType.TextBox, CtrlType.Int });
            deny = AddDat(deny, limitKind, str);
            LimitUrl limitUrl = new LimitUrl(allow, deny);
            var errorStr = "";
            Assert.AreEqual(limitUrl.IsAllow(target, ref errorStr), isAllow);
        }

        [TestCase("go.com", LimitKind.Part,".*",LimitKind.Regular,"http://www.go.com/", true)]
        [TestCase("go.com", LimitKind.Part, ".*", LimitKind.Regular, "http://www.go.co", false)]
        public void AllowDenyTest(string allowStr, LimitKind allowKind, string denyStr, LimitKind denyKind, string target, bool isAllow) {
            var allow = new Dat(new[] { CtrlType.TextBox, CtrlType.Int });
            var deny = new Dat(new[] { CtrlType.TextBox, CtrlType.Int });
            allow = AddDat(allow, allowKind, allowStr);
            deny = AddDat(deny, denyKind, denyStr);
            var limitUrl = new LimitUrl(allow, deny);
            var errorStr = "";
            Assert.AreEqual(limitUrl.IsAllow(target, ref errorStr), isAllow);
        }


    }
}
