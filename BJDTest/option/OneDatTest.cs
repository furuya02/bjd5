using System;
using Bjd.option;
using NUnit.Framework;



namespace BjdTest.option {
    [TestFixture]
    class OneDatTest {
        
        private static readonly String[] StrList = new[] { "user1", "pass" };
	    private static readonly bool[] IsSecretlList = new[] { true, false };

        [TestCase(false, "\tuser1\tpass")]
        [TestCase(true, "\t***\tpass")]
        public void IsSecretの違いによるToRegの確認Enableがtrueの場合(bool isSecret, string expected) {
            //setUp
			var enable = true; //Enable=TRUE
            var sut = new OneDat(enable, StrList, IsSecretlList);
            //exercise
            var actual = sut.ToReg(isSecret);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(false, "#\tuser1\tpass")]
        [TestCase(true, "#\t***\tpass")]
        public void IsSecretの違いによるToRegの確認Enableがfalseの場合(bool isSecret, string expected) {
            //setUp
            var enable = false; //Enable=FALSE
            var sut = new OneDat(enable, StrList, IsSecretlList);
            //exercise
            var actual = sut.ToReg(isSecret);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

		[TestCase(2, "\tuser1\tpass")]
		[TestCase(2, "#\tuser1\tpass")]
		[TestCase(3, "\tn1\tn2\tn3")]
        public void FromRegで初期化してToRegで出力する(int max, String str) {
            //setUp
		    var sut = new OneDat(true, new String[max], new bool[max]);
            sut.FromReg(str);
            var expected = str;
            //exercise
            var actual = sut.ToReg(false);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

		[TestCase(3, "\tuser1\tpass")] //カラム数宇一致
		[TestCase(2, null)]
		[TestCase(3, "_\tn1\tn2\tn3")] //無効文字列
		[TestCase(3, "")] //無効文字列
		[TestCase(3, "\t")] //無効文字列
        public void FromRegに無効な入力があった時falseが帰る(int max, String str) {
            //setUp
			var sut = new OneDat(true, new String[max], new bool[max]);
			var expected = false;
            //exercise
			var actual = sut.FromReg(str);
            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

    }
}
