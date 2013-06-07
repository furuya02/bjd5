using System.Text;
using System.Text.RegularExpressions;


namespace SmtpServer {
    class MlSubject {
        readonly int _kind;
        readonly string _mlName;
        public MlSubject(int kind,string mlName) {
            _kind = kind;
            _mlName = mlName;
        }
        //タイトル（連番）文字列を取得
        public string Get(int no) {
            switch (_kind) {
                case 0://(NAME)
                    return string.Format("({0})", _mlName);
                case 1://[NAME]
                    return string.Format("[{0}]", _mlName);
                case 2://(00000)
                    return string.Format("({0:D5})", no);
                case 3://[00000]
                    return string.Format("[{0:D5}]", no);
                case 4://(NAME:00000)
                    return string.Format("({0}:{1:D5})", _mlName, no);
                case 5://[NAME:00000]
                    return string.Format("[{0}:{1:D5}]", _mlName, no);
                case 6://none
                    return "";
            }
            return "ERROR";
        }
        //連番を付加したSubjectの生成
        public string Get(string subject,int no) {

            Encoding encoding = null;//エンコードされていない場合は、nullのまま変化せず
            var text="";
            if (subject!=null) {
                text = Subject.Decode(ref encoding, subject);//Subjectのデコード処理
            }

            //連番の削除
            var regex = CreateRegex();
            text = regex.Replace(text, "", 1);
            //重複したRe:の削除
            regex = new Regex("[Rr][Ee]: *");
            var tmp = regex.Replace(text, "", 1);
            if (regex.IsMatch(tmp)) {
                text = tmp;
            }
            text = Get(no) + " " + text;// string.Format("[{0}:{1:D5}] {2}",name,no,text);//連番を追加
            return Subject.Encode(encoding, text);//Subjectのエンコード処理
        }
        //タイトル（連番）の正規表現を取得
        Regex CreateRegex() {
            switch (_kind) {
                case 0://(NAME)
                    return new Regex(string.Format("\\({0}\\) ", _mlName));
                case 1://[NAME]
                    return new Regex(string.Format("\\[{0}\\] ", _mlName));
                case 2://(00000)
                    return new Regex("\\([0-9]*\\) ");
                case 3://[00000]
                    return new Regex("\\[[0-9]*\\] ");
                case 4://(NAME:00000)
                    return new Regex(string.Format("\\({0}:[0-9]*\\) ", _mlName));
                case 5://[NAME:00000]
                    return new Regex(string.Format("\\[{0}:[0-9]*\\] ", _mlName));
                case 6://none
                    return new Regex("");
            }
            return null;
        }
    }
}
