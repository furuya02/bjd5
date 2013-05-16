using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BjdTest.test;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest {

public class RrCnameTest {

	//CNAME class=1 ttl=0x000000067 ytimg.l.google.com
    private const string Str0 = "00050001000000670014057974696d67016c06676f6f676c6503636f6d00";

    [Test]
	public void GetCnameの確認(){
		//setUp
		var expected = "ns.google.com.";
		var sut = new RrCname("aaa.com", 0, expected);
		//exercise
		var actual = sut.CName;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void バイナリ初期化との比較(){
		//setUp
		var sut = new RrCname("aaa.com", 64800, "1.");
		var expected = (new RrCname("aaa.com", 64800, new byte[] { 01, 49, 0 })).ToString();
		//exercise
		var actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void 実パケット生成したオブジェクトとの比較(){
		//setUp
		var sut = new RrCname("aaa.com", 0x00000067, "ytimg.l.google.com");
		var rr = new PacketRr(TestUtil.HexStream2Bytes(Str0), 0);
		var expected = (new RrCname("aaa.com", rr.Ttl, rr.Data)).ToString();
		//exercise
		var actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}
	
	[Test]
	public void ToStringの確認(){
		//setUp
		var expected = "Cname ns.aaa.com. TTL=222 www.aaa.com.";
		var sut = new RrCname("ns.aaa.com.", 222, "www.aaa.com.");
		//exercise
		var actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}
}
}
