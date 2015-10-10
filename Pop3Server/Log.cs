
namespace Pop3Server {
    partial class Server {

        //BJD.Lang.txt�ɕK�v�Ȓ�`�������Ă��邩�ǂ����̊m�F
        protected override void CheckLang()
        {
            for (var n = 1; n <= 5; n++){
                Lang.Value(n);
            }
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1:
                case 2:
                case 3:
               case 4:
                case 5:
                    return Lang.Value(messageNo);
            }
            return "unknown";
        }
    }
}
