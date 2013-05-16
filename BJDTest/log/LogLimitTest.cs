using System;
using Bjd.ctrl;
using Bjd.log;
using Bjd.option;
using NUnit.Framework;


namespace BjdTest.log{
    internal class LogLimitTest{
        //初期化
        private LogLimit create(bool isDisplay){
            var dat = new Dat(new[]{CtrlType.TextBox});
            dat.Add(true, "AAA");
            dat.Add(true, "表示");
            dat.Add(true, "123");
            dat.Add(true, "アイウ");
            return new LogLimit(dat, isDisplay);
        }

        [Test]
        public void 指定文字列を表示する_で初期化された場合_AAA_は表示する(){

            //setUp
            const bool isDisplay = true; //表示する
            var sut = create(isDisplay);

            const bool expected = true;

            //exercise
            var actual = sut.IsDisplay("AAA");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 指定文字列を表示する_で初期化された場合_表示A_は表示する(){

            //setUp
            const bool isDisplay = true; //表示する
            var sut = create(isDisplay);

            const bool expected = true;

            //exercise
            var actual = sut.IsDisplay("表示A");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 指定文字列を表示する_で初期化された場合_表A123_は表示する(){

            //setUp
            const bool isDisplay = true; //表示する
            var sut = create(isDisplay);

            const bool expected = true;

            //exercise
            var actual = sut.IsDisplay("表A123");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 指定文字列を表示する_で初期化された場合_123_は表示する(){

            //setUp
            const bool isDisplay = true; //表示する
            var sut = create(isDisplay);

            const bool expected = true;

            //exercise
            var actual = sut.IsDisplay("123");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 指定文字列を表示する_で初期化された場合_12アイウ_は表示する(){

            //setUp
            const bool isDisplay = true; //表示する
            var sut = create(isDisplay);

            const bool expected = true;

            //exercise
            var actual = sut.IsDisplay("12アイウ");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void 指定文字列を表示しない_で初期化された場合_AAA_は表示しない(){

            //setUp
            const bool isDisplay = false; //表示しない
            var sut = create(isDisplay);

            const bool expected = false;

            //exercise
            var actual = sut.IsDisplay("AAA");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 指定文字列を表示しない_で初期化された場合_表示A_は表示しない(){

            //setUp
            const bool isDisplay = false; //表示しない
            var sut = create(isDisplay);

            const bool expected = false;

            //exercise
            var actual = sut.IsDisplay("表示A");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 指定文字列を表示しない_で初期化された場合_表A123_は表示しない(){

            //setUp
            const bool isDisplay = false; //表示しない
            var sut = create(isDisplay);

            const bool expected = false;

            //exercise
            var actual = sut.IsDisplay("表A123");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }



        [Test]
        public void 指定文字列を表示しない_で初期化された場合_123_は表示しない(){

            //setUp
            const bool isDisplay = false; //表示しない
            var sut = create(isDisplay);

            const bool expected = false;

            //exercise
            var actual = sut.IsDisplay("123");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 指定文字列を表示しない_で初期化された場合_12アイウ_は表示しない(){

            //setUp
            const bool isDisplay = false; //表示しない
            var sut = create(isDisplay);

            const bool expected = false;

            //exercise
            var actual = sut.IsDisplay("12アイウ");

            //verify
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void 指定した文字列が表示対象か否かの判断(){

            var dat = new Dat(new[]{CtrlType.TextBox});
            dat.Add(true, "AAA");
            dat.Add(true, "表示");
            dat.Add(true, "123");
            dat.Add(true, "アイウ");
            const bool isDisplay = true;
            var logLimit = new LogLimit(dat, isDisplay);

            //表示する
            var expected = true;
            Check(logLimit, "AAA", expected);
            Check(logLimit, "表示A", expected);
            Check(logLimit, "表A123", expected);
            Check(logLimit, "123", expected);
            Check(logLimit, "12アイウ", expected);

            //表示しない
            expected = false;
            Check(logLimit, "AA", expected);
            Check(logLimit, "表a示A", expected);
            Check(logLimit, "表A23", expected);
            Check(logLimit, "", expected);
            Check(logLimit, "12アイ", expected);
            Check(logLimit, null, expected);

        }

        private static void Check(LogLimit logLimit, String str, bool expected){
            var actual = logLimit.IsDisplay(str);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}