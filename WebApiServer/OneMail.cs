using System;
using System.IO;
using System.Text;
using Bjd.mail;

namespace WebApiServer {
    class OneMail{

        private readonly Mail _mail;
        private readonly MailInfo _mailInfo;

        public String Owner { get; private set; }
        public String Subject {
            get{
                var s = _mail.GetHeader("subject");
                if (s != null){
                    return DecodeMailSubject(s);
                }
                return "";
            }
        }
        public String Date {
            get {
                return _mailInfo.Date;
            }
        }
        public String From {
            get {
                return _mailInfo.From.ToString();
            }
        }
        public String To {
            get {
                return _mailInfo.To.ToString();
            }
        }
        public int Size {
            get{
                return (int)_mailInfo.Size;
            }
        }

        public OneMail(String owner,String fileName){
            Owner = owner;
            _mailInfo = new MailInfo(fileName);
            fileName = fileName.Replace("\\DF_","\\MF_");

            _mail = new Mail();
            if (File.Exists(fileName)) {
                _mail.Init2(Encoding.ASCII.GetBytes(File.ReadAllText(fileName)));
            }
        }
        string DecodeMailSubject(string subject) {
            string[] s = subject.Split('?');
            byte[] b;
            if (s[2] == "B") { //Base64形式
                b = Convert.FromBase64String(s[3]);
            } else {
                return subject; //未対応
            }
            //s[1]をEncoding名として、デコード
            return Encoding.GetEncoding(s[1]).GetString(b);
        }

    }
}
