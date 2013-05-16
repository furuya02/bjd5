using System;


namespace Bjd.log{


    // ログ１行を表現するクラス
    public class OneLog : ValidObj{

        private DateTime _dt;
        private LogKind _logKind;
        private String _nameTag;
        private long _threadId;
        private String _remoteHostname;
        private int _messageNo;
        private String _message;
        private String _detailInfomation;

        public OneLog(DateTime dt, LogKind logKind, String nameTag, long threadId, String remoteHostname, int messageNo, String message, String detailInfomation){
            _dt = dt;
            _logKind = logKind;
            _nameTag = nameTag;
            _threadId = threadId;
            _remoteHostname = remoteHostname;
            _messageNo = messageNo;
            _message = message;
            _detailInfomation = detailInfomation;
        }

        //コンストラクタ
        //行の文字列(\t区切り)で指定される
        public OneLog(String str){
            var tmp = str.Split('\t');
            if (tmp.Length != 8){
                ThrowException(str); // 初期化失敗
            }
            _nameTag = tmp[3];
            _remoteHostname = tmp[4];
            _messageNo = int.Parse(tmp[5]);
            _message = tmp[6];
            _detailInfomation = tmp[7];
            if (!Enum.TryParse(tmp[1], out _logKind)){
                ThrowException(str); // 初期化失敗
            }
            try{
                _dt = DateTime.Parse(tmp[0]);
                _threadId = long.Parse(tmp[2]);
                _messageNo = int.Parse(tmp[5]);
            }
            catch (Exception){
                ThrowException(str); // 初期化失敗
            }

        }

        public String Dt(){
            CheckInitialise(); // 他のgetterは、これとセットで使用されるため、チェックはここだけにする
            return String.Format("{0:D4}/{1:D2}/{2:D2} {3:D2}:{4:D2}:{5:D2}", _dt.Year, _dt.Month, _dt.Day, _dt.Hour,
                                 _dt.Minute, _dt.Second);
        }

        public String Kind(){
            return _logKind.ToString();
        }

        public String NameTag(){
            return _nameTag;
        }

        public String ThreadId(){
            return _threadId.ToString();
        }

        public String RemoteHostname(){
            return _remoteHostname;
        }

        public String MessageNo(){
            return String.Format("{0:D7}", _messageNo);
        }

        public String Message(){
            return _message;
        }

        public String DetailInfomation(){
            return _detailInfomation;
        }



        //文字列化
        //\t区切りで出力される
        public override String ToString(){
            CheckInitialise();
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", Dt(), Kind(), ThreadId(),
                                 NameTag(), RemoteHostname(), MessageNo(), Message(), DetailInfomation());
        }

        //セキュリティログかどうかの確認
        public bool IsSecure(){
            CheckInitialise();
            if (_logKind == LogKind.Secure){
                return true;
            }
            return false;
        }

        protected override void Init(){
            _dt = new DateTime(0);
            _logKind = LogKind.Normal;
            _threadId = 0;
            _nameTag = "UNKNOWN";
            _remoteHostname = "";
            _messageNo = 0;
            _message = "";
            _detailInfomation = "";
        }
    }
}