using BjdTest;
using Bjd;
using SipServer;

namespace SipServerTest {
    class TsServer : TsServerBase {
        public TsServer()
            : base("SipServer", ProtocolKind.Udp, 5060) {

        }
        protected override OneServer CreateServer(Kernel kernel, OneBind oneBind) {
            return new Server(kernel, NameTag, oneBind);
        }
        protected override OneOption CreateOption(Kernel kernel) {
            return new Option(kernel, "", NameTag);
        }
    }
}
