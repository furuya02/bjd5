using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BjdTest.test;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest {


public class RrMxTest {

	//MX class=1 ttl=0x00000289 pref=30 alt3.gmail-smtp-in.l.google.com
	private string str0 = "000f0001000002890023001e04616c74330d676d61696c2d736d74702d696e016c06676f6f676c6503636f6d00";

	[Test]
	public void GetPreferenceの確認(){
		//setUp
		ushort expected = 10;
		RrMx sut = new RrMx("aaa.com", 0, expected, "exchange.host.");
		//exercise
		ushort actual = sut.Preference;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void GetMailExchangeHostの確認(){
		//setUp
		string expected = "exchange.host.";
		RrMx sut = new RrMx("aaa.com", 0, 10, expected);
		//exercise
		string actual = sut.MailExchangeHost;
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void バイナリ初期化との比較(){
		//setUp
		RrMx sut = new RrMx("aaa.com", 64800, 20, "1.");
		var expected = (new RrMx("aaa.com", 64800, new byte[] { 0, 20, 01, 49, 0 })).ToString();
		//exercise
		var actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	public void 実パケット生成したオブジェクトとの比較(){
		//setUp
		RrMx sut = new RrMx("aaa.com", 0x00000289, 30, "alt3.gmail-smtp-in.l.google.com");
		PacketRr rr = new PacketRr(TestUtil.HexStream2Bytes(str0), 0);
		var expected = (new RrMx("aaa.com", rr.Ttl, rr.Data)).ToString();
		//exercise
		var actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}
	
	[Test]
	public void ToStringの確認(){
		//setUp
		string expected = "Mx aaa.com TTL=10 10 smtp.aaa.com.";
		RrMx sut = new RrMx("aaa.com", 10,  10, "smtp.aaa.com.");
		//exercise
		string actual = sut.ToString();
		//verify
		Assert.That(actual, Is.EqualTo(expected));
	}
}
}
