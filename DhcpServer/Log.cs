
using Bjd;
using Bjd.option;

namespace DhcpServer {
    partial class Server {
        private readonly Dat _macAcl;

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1:return IsJp ? "MACアドレスによる制限　利用者に登録されていないMACアドレスからの要求を破棄します":"Access deny by a MAC address";
                case 3:return IsJp ?"リクエスト ->":"request ->";
                case 4:return IsJp ?"<- レスポンス":"<- response";
                case 5:return IsJp ?"リースしました":"complete a Lease";
                case 6:return IsJp ?"開放しました":"complete a Release";
            }
            return "unknown";
        }

    }
}
