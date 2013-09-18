using System;
using Bjd.mail;

namespace WebApiServer {
    class OneMail{
        public MailInfo MailInfo { get; private set; }
        public String FileName { get; private set; }
        public String Owner { get; private set; }

        public OneMail(String owner,String fileName){
            Owner = owner;
            MailInfo = new MailInfo(fileName);
            FileName = fileName.Replace("\\DF","\\MF_");
        }
    }
}
