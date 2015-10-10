using System.Collections.Generic;
using Bjd;
using Bjd.mail;
using Bjd.net;
using SmtpServer;

namespace SmtpServerTest {
    //MailSave�̃��b�N�I�u�W�F�N�g
    class TsMailSave : MailSave {
        readonly List<OneMail> _ar = new List<OneMail>();
        public TsMailSave()
            : base(null, null,null,null, null, null) {
 
        }
        override public bool Save(MailAddress from, MailAddress to, Mail mail, string host, Ip addr) {
            _ar.Add(new OneMail(from, to, mail));
            return true;
        }

        public void Dispose(){
            Clear();
        }
        public void Clear() {
            _ar.Clear();
        }

        public Mail GetMail(int i) {
            if (i < _ar.Count)
                return _ar[i].Mail;
            return null;
        }

        public MailAddress GetFrom(int i) {
            if (i < _ar.Count)
                return _ar[i].From;
            return null;
        }

        public MailAddress GetTo(int i) {
            if (i < _ar.Count)
                return _ar[i].To;
            return null;
        }

        public int Count() {
            return _ar.Count;
        }
        //***********************************************
        // OneMail
        //***********************************************
        class OneMail {
            public Mail Mail { get; private set; }
            public MailAddress From { get; private set; }
            public MailAddress To { get; private set; }
            public OneMail(MailAddress from, MailAddress to, Mail mail) {
                From = from;
                To = to;
                Mail = mail;
            }
        }

    }

}