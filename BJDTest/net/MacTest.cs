using Bjd.net;
using NUnit.Framework;

namespace BjdTest.net {
    class MacTest {
	    [TestCase("00-00-00-00-00-00")]
	    [TestCase("00-26-2D-3F-3F-67")]
	    [TestCase("00-ff-ff-ff-3F-67")]
	    [TestCase("FF-FF-FF-FF-FF-FF")]
	    public void Mac_macStr_で初期化してtoStringで確かめる(string macStr) {
			//setUp
			var sut = new Mac(macStr);
			var expected = macStr.ToLower();
			//exercise
			var actual = sut.ToString().ToLower();
			//verify
			Assert.That(actual, Is.EqualTo(expected));
		}
	

	    [TestCase("12-34-56-78-9a-bc", true)]
	    [TestCase("12-34-56-78-9A-BC", true)]
	    [TestCase("00-26-2D-3F-3F-67", false)]
	    [TestCase("00-00-00-00-00-00", false)]
	    [TestCase("ff-ff-ff-ff-ff-ff", false)]
	    [TestCase(null, false)]
    	public void Equalのテスト12_34_56_78_9A_BCと比較する(string macStr,bool expected) {
    		//setUp
			var sut = new Mac("12-34-56-78-9A-BC");
			Mac target = null;
			if (macStr != null) {
				target = new Mac(macStr);
			}
			//exercise
			bool actual = sut.Equals(target);
			//verify
			Assert.That(actual, Is.EqualTo(expected));
		}

	}
}
