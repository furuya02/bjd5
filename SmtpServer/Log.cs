
namespace SmtpServer {
    public partial class Server {
        //BJD.Lang.txt�ɕK�v�Ȓ�`�������Ă��邩�ǂ����̊m�F
        protected override void CheckLang()
        {
            for (var n = 0; n <= 17; n++)
            {
                Lang.Value(n);
            }
            for (var n = 19; n <= 23; n++)
            {
                Lang.Value(n);
            }
            for (var n = 25; n <= 34; n++)
            {
                Lang.Value(n);
            }
            for (var n = 37; n <= 57; n++)
            {
                Lang.Value(n);
            }
        }
        public override string GetMsg(int messageNo)
        {
            switch (messageNo) {
                case 0:
                case 1:
                case 2: 
                case 3: 
                case 4:
                case 5: 
                case 6: 
                case 7: 
                case 8: 
                case 9: 
                case 10: 
                case 11: 
                case 12: 
                case 13: 
                case 14: 
                case 15: 
                case 16: 
                case 17: 
                //case 18:
                case 19: 
                case 20: 
                case 21: 
                case 22: 
                case 23: 
                //case 24: 
                case 25: 
                case 26: 
                case 27: 
                case 28: 
                case 29: 
                case 30: 
                case 31: 
                case 32: 
                case 33: 
                case 34: 
                // case 35: 
                // case 36: 
                case 37: 
                case 38:
                case 39: 
                case 40: 
                case 41: 
                case 42: 
                case 43: 
                case 44: 
                case 45: 
                case 46: 
                case 47: 
                case 48: 
                case 49: 
                case 50: 
                case 51: 
                case 52: 
                case 53: 
                case 54: 
                case 55: 
                case 56:
                case 57:
                    return Lang.Value(messageNo);
            }
            return "unknown";
        }

    }
}
