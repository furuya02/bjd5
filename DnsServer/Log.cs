using Bjd;
using Bjd.option;
using Bjd.server;
using Bjd.util;

namespace DnsServer {
        partial class Server {
            
            //BJD.Lang.txtに必要な定義が揃っているかどうかの確認
            protected override void CheckLang()
            {
                for (var n = 2; n <= 6; n++){
                    Lang.Value(n);
                }
                for (var n = 19; n <= 21; n++){
                    Lang.Value(n);
                }
            }


            public override string GetMsg(int messageNo){
                switch (messageNo){
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        return Lang.Value(messageNo);
                    case 7:
                        return "zone database initialised.";
                    case 8:
                        return "Query recv";
                    case 9:
                        return "request to a domain under auto (localhost)";
                    case 10:
                        return "request to a domain under management";
                    case 11:
                        return "request to a domain under auto (localhost)";
                    case 12:
                        return "request to a domain under management";
                    case 13:
                        return "Create Response (AN)";
                    case 15:
                        return "Create Response (AN.CNAME)";
                    case 17:
                        return "Lookup send";
                    case 18:
                        return "Lookup recv";
                    case 19:
                    case 20:
                    case 21:
                        return Lang.Value(messageNo);
                    case 22:
                        return "Create Response (AR)";
                    case 23:
                        return "Append RR";
                    case 24:
                        return "_rootCache.Add";

                    default:
                        return "unknown";
                }
            }
    }
}
