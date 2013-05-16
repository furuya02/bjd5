using BjdTest;
using Bjd;
using SampleServer;

namespace SampleServerTest {
    class TsServer : TsServerBase {
        public TsServer()
            : base("SampleServer", ProtocolKind.Tcp, 9999) {

        }
        protected override OneServer CreateServer(Kernel kernel, OneBind oneBind) {
            return new Server(kernel, NameTag, oneBind);
        }
        protected override OneOption CreateOption(Kernel kernel) {
            return new Option(kernel, "", NameTag);
        }
    }
}
