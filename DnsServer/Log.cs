
namespace DnsServer {
    partial class Server {
        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 0: return (Kernel.Jp) ? "標準問合(OPCODE=0)以外のリクエストには対応できません" : "Because I am different from 0 in OPCODE,can't process it.";
                case 1: return (Kernel.Jp) ? "質問エントリーが１でないパケットは処理できません":"Because I am different from 1 a question entry,can't process it.";
                case 2: return (Kernel.Jp) ? "パケットのサイズに問題があるため、処理を継続できません":"So that size includes a problem,can't process it.";
                case 3: return (Kernel.Jp) ? "パケットのサイズに問題があるため、処理を継続できません":"So that size includes a problem,can't process it.";
                case 4: return (Kernel.Jp) ? "パケットのサイズに問題があるため、処理を継続できません":"So that size includes a problem,can't process it.";
                case 5: return (Kernel.Jp) ? "Lookup() パケット受信でタイムアウトが発生しました。":"Timeout occurred in Lookup()";
                case 6: return (Kernel.Jp) ? "ルートキャッシュを読み込みました":"root cache database initialised.";
                case 7: return "zone database initialised.";
                case 8: return "Query";
                case 9: return "request to a domain under auto (localhost)";
                case 10: return "request to a domain under management";
                case 11: return "request to a domain under auto (localhost)";
                case 12: return "request to a domain under management";
                case 13: return "Search LocalCache";
                case 14: return  "Answer";
                case 15: return "Search LocalCache";
                case 16: return "Answer CNAME";
                case 17: return "Lookup";
                case 18: return "Lookup";
                case 19: return (Kernel.Jp) ? "A(PTR)レコードにIPv6アドレスを指定できません" : "IPv6 cannot address it in an A(PTR) record";
                case 20: return (Kernel.Jp) ? "AAAAレコードにIPv4アドレスを指定できません" : "IPv4 cannot address it in an AAAA record";
                case 21:  return (Kernel.Jp) ? "ルートキャッシュが見つかりません" : "Root chace is not found";

            }
            return "unknown";
        }
    }
}
