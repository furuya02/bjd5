using SmtpServer;
using BjdTest;
using Bjd;

namespace SmtpServerTest {
    class TsServer : TsServerBase {
        public TsServer()
            : base("SmtpServer", ProtocolKind.Tcp, 25) {

        }
        protected override OneServer CreateServer(Kernel kernel, OneBind oneBind) {
            return new Server(kernel, NameTag, oneBind);
        }
        protected override OneOption CreateOption(Kernel kernel) {
            return new Option(kernel, "", NameTag);
        }
    }
}
