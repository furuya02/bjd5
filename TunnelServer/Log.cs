
namespace TunnelServer {
    partial class Server {
        protected override void CheckLang()
        {
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1:  return Kernel.IsJp()?"接続先サーバが指定されていません":"Connection ahead server is not appointed";
                case 2:  return Kernel.IsJp()?"接続先ポートが指定されていません":"Connection ahead port is not appointed";
                case 4:  return Kernel.IsJp()?"サーバへの接続に失敗しました(1)":"Failed in connection to a server (1)";
                case 5:  return Kernel.IsJp()?"サーバへの接続に失敗しました(2)":"Failed in connection to a server (2)";
                case 6:  return Kernel.IsJp()?"TCPストリームをトンネルしました":"A tunnel(TCP stream)";
                case 7:  return Kernel.IsJp()?"UDPパケットをトンネルしました":"A tunnel(UDP packet)";
            }
            return "unknown";
        }

    }
}
