using BjdTest;
using Bjd;
using FtpServer;

namespace FtpServerTest {
    class TsServer : TsServerBase {
        public TsServer()
            : base("FtpServer", ProtocolKind.Tcp,21) {

        }
        protected override OneServer CreateServer(Kernel kernel, OneBind oneBind) {
            return new Server(kernel, NameTag, oneBind);
        }
        protected override OneOption CreateOption(Kernel kernel) {
            return new Option(kernel, "", NameTag);
        }
    }
}
