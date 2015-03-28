
namespace ProxyTelnetServer {
    partial class Server {
        protected override void CheckLang()
        {
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1: return Kernel.IsJp()?"Ú‘±‚µ‚Ü‚µ‚½":"Connected";
                case 2: return Kernel.IsJp()?"Ú‘±‚É¸”s‚µ‚Ü‚µ‚½(1)":"Failed in connection(1)";
                case 3: return Kernel.IsJp()?"Ú‘±‚É¸”s‚µ‚Ü‚µ‚½(2)":"Failed in connection(1)";
            }
            return "unknown";
        }

    }
}
