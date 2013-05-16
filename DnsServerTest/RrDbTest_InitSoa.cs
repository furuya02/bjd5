using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.net;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    public class RrDbTest_initSoa{

        [Test]
        public void 予め同一ドメインのNSレコードが有る場合成功する(){
            //setUp
            RrDb sut = new RrDb();
            bool expected = true;
            sut.Add(new RrNs("aaa.com.", 0, "ns.aaa.com."));
            //exercise
            bool actual = RrDbTest.InitSoa(sut, "aaa.com.", "mail.", 1, 2, 3, 4, 5);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 予め同一ドメインのNSレコードが無い場合失敗する_レコードが無い(){
            //setUp
            RrDb sut = new RrDb();
            bool expected = false;
            //exercise
            bool actual = RrDbTest.InitSoa(sut, "aaa.com.", "mail.", 1, 2, 3, 4, 5);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 予め同一ドメインのNSレコードが無い場合失敗する_NSレコードはあるがドメインが違う(){
            //setUp
            RrDb sut = new RrDb();
            bool expected = false;
            sut.Add(new RrNs("bbb.com.", 0, "ns.bbb.com.")); //NSレコードはあるがドメインが違う
            //exercise
            bool actual = RrDbTest.InitSoa(sut, "aaa.com.", "mail.", 1, 2, 3, 4, 5);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 予め同一ドメインのNSレコードが無い場合失敗する_ドメインは同じだがNSレコードではない(){
            //setUp
            RrDb sut = new RrDb();
            bool expected = false;
            sut.Add(new RrA("aaa.com.", 0, new Ip("192.168.0.1"))); //ドメインは同じだがNSレコードではない
            //exercise
            bool actual = RrDbTest.InitSoa(sut, "aaa.com.", "mail.", 1, 2, 3, 4, 5);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 追加に成功したばあのSOAレコードの検証(){
            //setUp
            RrDb sut = new RrDb();
            sut.Add(new RrNs("aaa.com.", 0, "ns.aaa.com."));
            //exercise
            RrDbTest.InitSoa(sut, "aaa.com.", "root@aaa.com", 1, 2, 3, 4, 5);
            //verify
            Assert.That(RrDbTest.Size(sut), Is.EqualTo(2)); //NS及びSOAの2件になっている
            RrSoa o = (RrSoa) RrDbTest.Get(sut, 1);
            Assert.That(o.NameServer, Is.EqualTo("ns.aaa.com."));
            Assert.That(o.PostMaster, Is.EqualTo("root.aaa.com.")); //変換が完了している(@=>. 最後に.追加）
            Assert.That(o.Serial, Is.EqualTo(1));
            Assert.That(o.Refresh, Is.EqualTo(2));
            Assert.That(o.Retry, Is.EqualTo(3));
            Assert.That(o.Expire, Is.EqualTo(4));
            Assert.That(o.Minimum, Is.EqualTo(5));
        }
    }
}