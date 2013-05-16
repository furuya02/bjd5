using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BjdTest.test;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest {


public class RrSoaTest {

	//MX class=1 ttl=0x00000289 pref=30 alt3.gmail-smtp-in.l.google.com
	private string str0 = "000f0001000002890023001e04616c74330d676d61696c2d736d74702d696e016c06676f6f676c6503636f6d00";

	[Test]
	public void getNameServerの確認(){
		//setUp
		string expected = "ns.aaa.com.";
		RrSoa sut = new RrSoa("aaa.com", 0, expected, "post.master.", 1, 2, 3, 4, 5);
		//exercise
		string actual = sut.NameServer;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void getPostMasterの確認(){
		//setUp
		string expected = "root.aaa.com.";
		RrSoa sut = new RrSoa("aaa.com.", 0, "ns.aaa.com.", expected, 1, 2, 3, 4, 5);
		//exercise
		string actual = sut.PostMaster;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void getSerialの確認(){
		//setUp
		var expected = 100u;
		RrSoa sut = new RrSoa("aaa.com.", 0, "ns.aaa.com.", "postmaster.", expected, 2, 3, 4, 5);
		//exercise
		uint actual = sut.Serial;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void getRefreshの確認(){
		//setUp
		var expected = 300u;
		RrSoa sut = new RrSoa("aaa.com.", 0, "ns.aaa.com.", "postmaster.", 1, expected, 3, 4, 5);
		//exercise
		uint actual = sut.Refresh;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void getRetryの確認(){
		//setUp
		uint expected = 400u;
		var sut = new RrSoa("aaa.com.", 0, "ns.aaa.com.", "postmaster.", 1, 2, expected, 4, 5);
		//exercise
		uint actual = sut.Retry;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void getExpireの確認(){
		//setUp
		uint expected = 500u;
		RrSoa sut = new RrSoa("aaa.com", 0, "ns.aaa.com.", "postmaster.", 1, 2, 3, expected, 5);
		//exercise
		uint actual = sut.Expire;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void getMinimumの確認(){
		//setUp
		uint expected = 300u;
		var sut = new RrSoa("aaa.com", 0, "ns.aaa.com.", "postmaster.", 1, 2, 3, 4, expected);
		//exercise
		uint actual = sut.Minimum;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void バイナリ初期化との比較(){
		//setUp
		var sut = new RrSoa("aaa.com", 10, "1", "2", 1, 2, 3, 4, 5);
		var expected = (new RrSoa("aaa.com", 10, new byte[] { 1, 49, 0, 1, 50, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 4, 0, 0, 0, 5 })).ToString();
		//exercise
		var actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void 実パケット生成したオブジェクトとの比較(){
		//setUp
		var sut = new RrMx("aaa.com", 0x00000289, 30, "alt3.gmail-smtp-in.l.google.com");
		var rr = new PacketRr(TestUtil.HexStream2Bytes(str0), 0);
		var expected = (new RrMx("aaa.com", rr.Ttl, rr.Data)).ToString();
		//exercise
		var actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void ToStringの確認(){
		//setUp
		var expected = "Soa aaa.com. TTL=0 ns.aaa.com. postmaster. 00000001 00000002 00000003 00000004 00000005";
		var sut = new RrSoa("aaa.com.", 0, "ns.aaa.com.", "postmaster.", 1, 2, 3, 4, 5);
		//exercise
		var actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}
}
}
