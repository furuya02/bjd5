using WebServer;
using NUnit.Framework;

namespace WebServerTest {
    [TestFixture]
    public class RequestTest {

        
        Request _request;

        [SetUp]
        public void SetUp() {


            _request = new Request(null,null);

        }

        [TearDown]
        public void TearDown() {

        
        }

        [TestCase("GET / HTTP/0.9", "HTTP/0.9")]
        [TestCase("GET / HTTP/1.0", "HTTP/1.0")]
        [TestCase("GET / HTTP/1.1", "HTTP/1.1")]
        [TestCase("GET / HTTP/2.0", null)] //未対応バージョン
        public void VerTest(string requestStr, string verStr) {
            if (verStr == null) {
                bool b = _request.Init(requestStr);
                Assert.AreEqual(b,false);
                return;
            }
            if (_request.Init(requestStr)) {
                Assert.AreEqual(_request.Ver, verStr);
                return;
            }
            Assert.AreEqual(_request.Ver,"ERROR");
        }

        [TestCase("GET / HTTP/1.1", HttpMethod.Get)]
        [TestCase("POST / HTTP/1.1", HttpMethod.Post)]
        [TestCase("PUT / HTTP/1.1", HttpMethod.Put)]
        [TestCase("OPTIONS / HTTP/1.1", HttpMethod.Options)]
        [TestCase("HEAD / HTTP/1.1", HttpMethod.Head)]
        [TestCase("MOVE / HTTP/1.1", HttpMethod.Move)]
        [TestCase("PROPFIND / HTTP/1.1", HttpMethod.Propfind)]
        [TestCase("PROPPATCH / HTTP/1.1", HttpMethod.Proppatch)]
        public void MethodTest(string requestStr, HttpMethod method) {
            if(!_request.Init(requestStr)){
                Assert.AreEqual(_request.Method, "ERROR");
                return;
            }
            Assert.AreEqual(_request.Method, method);
        }


        
        [TestCase(102, "Processiong")]
        [TestCase(200, "Document follows")]
        [TestCase(201, "Created")]
        [TestCase(204, "No Content")]
        [TestCase(206, "Partial Content")]
        [TestCase(207, "Multi-Status")]
        [TestCase(301, "Moved Permanently")]
        [TestCase(302, "Moved Temporarily")]
        [TestCase(304, "Not Modified")]
        [TestCase(400, "Missing Host header or incompatible headers detected.")]
        [TestCase(401, "Unauthorized")]
        [TestCase(402, "Payment Required")]
        [TestCase(403, "Forbidden")]
        [TestCase(404, "Not Found")]
        [TestCase(405, "Method Not Allowed")]
        [TestCase(412, "Precondition Failed")]
        [TestCase(422, "Unprocessable")]
        [TestCase(423, "Locked")]
        [TestCase(424, "Failed Dependency")]
        [TestCase(500, "Internal Server Error")]
        [TestCase(501, "Request method not implemented")]
        [TestCase(507, "Insufficient Storage")]
        [TestCase(0, "")]
        [TestCase(-1, "")]
        public void StatusMessageTest(int code, string msg) {
            var s = _request.StatusMessage(code);
            Assert.AreEqual(s,msg);
          
        }




    }
}
