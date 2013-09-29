using System;
using System.IO;
using System.Text;
using Bjd.mail;

namespace WebApiServer {
    class OneMail{

        private readonly Mail _mail;
        private readonly MailInfo _mailInfo;

        public object Get(string field) {
            switch (field){
                case "subject":
                    var s = _mail.GetHeader("subject");
                    if (s != null){
                        return DecodeMailSubject(s);
                    }
                    return "";
                case "date":
                    return _mailInfo.Date;
                case "from":
                    return _mailInfo.From.ToString();
                case "to":
                    return _mailInfo.To.ToString();
                case "size":
                    return (int)_mailInfo.Size;
                case "all":
                    return Encoding.ASCII.GetString(_mail.GetBytes());
                case "body":
                    return Encoding.ASCII.GetString(_mail.GetBody());
                case "uid":
                    return _mailInfo.Uid;
                case "filename":
                    return _mailInfo.FileName;
                    

            }
            return "";
        }

        public String Owner { get; private set; }

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
