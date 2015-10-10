
namespace TunnelServer {
    partial class Server {
        //BJD.Lang.txt�ɕK�v�Ȓ�`�������Ă��邩�ǂ����̊m�F
        protected override void CheckLang()
        {
            Lang.Value(1);
            Lang.Value(2);
            for (var n = 4; n <= 7; n++)
            {
                Lang.Value(n);
            }
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1:
                case 2:
                case 4:
                case 5:
                case 6:
                case 7:
                    return Lang.Value(messageNo);
            }
            return "unknown";
        }

    }
}
