using DhcpServer;
using BjdTest;
using Bjd;

namespace DhcpServerTest {
    class TsServer : TsServerBase {
        public TsServer()
            : base("DhcpServer", ProtocolKind.Udp, 67) {

        }
        protected override OneServer CreateServer(Kernel kernel, OneBind oneBind) {
            return new Server(kernel, NameTag, oneBind);
        }
        protected override OneOption CreateOption(Kernel kernel) {
            return new Option(kernel, "", NameTag);
        }
    }
}
