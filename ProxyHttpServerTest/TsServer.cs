using BjdTest;
using Bjd;
using ProxyHttpServer;

namespace ProxyHttpServerTest {
    class TsServer : TsServerBase {
        public TsServer()
            : base("ProxyHttpServer", ProtocolKind.Tcp, 8080) {

        }
        protected override OneServer CreateServer(Kernel kernel, OneBind oneBind) {
            return new Server(kernel, NameTag, oneBind);
        }
        protected override OneOption CreateOption(Kernel kernel) {
            return new Option(kernel, "", NameTag);
        }
    }
}
