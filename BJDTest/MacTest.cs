using Bjd.net;
using NUnit.Framework;
using Bjd;

namespace BjdTest {
    
    [TestFixture]
    class MacTest {
        
        [SetUp]
        public void SetUp() {
        }
        
        [TearDown]
        public void TearDown() {
        
        }

        [TestCase("00-00-00-00-00-00")]
        [TestCase("FF-FF-FF-FF-FF-FF")]
        [TestCase("00-26-2D-3F-3F-67")]
        [TestCase("00-ff-ff-ff-3F-67")]
        public void ToStringTest(string macStr) {
            var target = new Mac(macStr);
            Assert.AreEqual(target.ToString(), macStr.ToUpper());
        }

        [TestCase("00-00-00-00-00-00")]
        [TestCase("FF-FF-FF-FF-FF-FF")]
        [TestCase("00-26-2D-3F-3F-67")]
        [TestCase("00-ff-ff-ff-3F-67")]
        public void OperandTest(string macStr) {
            const string dmy = "11-11-11-11-11-11";
            Assert.AreEqual(new Mac(macStr) == new Mac(macStr), true);
            Assert.AreEqual(new Mac(macStr) != new Mac(macStr), false);
            Assert.AreEqual(new Mac(dmy) == new Mac(macStr), false);
            Assert.AreEqual(new Mac(dmy) != new Mac(macStr), true);
            Assert.AreEqual(new Mac(macStr) == null, false);
            Assert.AreEqual(new Mac(macStr) != null, true);
        }

    }
}
