using Bjd.net;
using NUnit.Framework;
using SipServer;
using Bjd;

namespace SipServerTest {
    [TestFixture]
    class JobRegisterTest {

        readonly User _user = new User();
        
        [SetUp]
        public void SetUp() {
            _user.Add(new OneUser("3000","3000xxx",new Ip("0.0.0.0")));
            _user.Add(new OneUser("3001","3001xxx",new Ip("0.0.0.0")));
            _user.Add(new OneUser("3002","3002xxx",new Ip("0.0.0.0")));
            _user.Add(new OneUser("3003","3003xxx",new Ip("0.0.0.0")));
           
        }
        [TearDown]
        public void TearDown() {
        }


        [TestCase()]
        public void Test(){
            
            
        }
    }

}
