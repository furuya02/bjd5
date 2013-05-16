using System.Threading;
using Bjd.net;
using NUnit.Framework;
using Bjd;

namespace BjdTest {
    public class AttackDbTest {
        [Test]
        public void TotalTest() {

            const int max = 5; //制限回数5回
            var sec = 1;//期間１秒
            var ip = new Ip("192.168.0.1");
            var o = new AttackDb(sec, max);
            for (int i = 0; i < max - 1; i++)
                Assert.AreEqual(o.IsInjustice(false, ip), false);
            Assert.AreEqual(o.IsInjustice(false, ip), true);//maxを超えると不正アクセスと判断される
            Assert.AreEqual(o.IsInjustice(true, ip), false);//認証成功(蓄積された情報は破棄される)
            Assert.AreEqual(o.IsInjustice(false, ip), false);//正常アクセス　1回目の失敗

            o = new AttackDb(sec, max);

            for (int i = 0; i < max - 1; i++)
                Assert.AreEqual(o.IsInjustice(false, ip), false);
            Thread.Sleep(1000);//1秒経過（蓄積された情報は時間経過で破棄される）
            Assert.AreEqual(o.IsInjustice(false, ip), false);//不正アクセス


            sec = 2;//期間を２秒に設定する
            o = new AttackDb(sec, max);

            for (int i = 0; i < max - 1; i++)
                Assert.AreEqual(o.IsInjustice(false, ip), false);
            Thread.Sleep(1000);//1秒経過
            Assert.AreEqual(o.IsInjustice(false, ip), true);//指定時間が経過していない場合、不正アクセスと判断される

        }
    }
}
