using BjdTest;
using Bjd;
using DnsServer;

namespace DnsServerTest {
    class TsServer : TsServerBase {
        public TsServer()
            : base("DnsServer", ProtocolKind.Udp,53) {

        }
        protected override OneServer CreateServer(Kernel kernel, OneBind oneBind) {
            return new Server(kernel, NameTag, oneBind);
        }
        protected override OneOption CreateOption(Kernel kernel) {
            return new Option(kernel, "", NameTag);
        }
    }
}
