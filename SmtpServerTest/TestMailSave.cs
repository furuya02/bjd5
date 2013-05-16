using System.Collections.Generic;
using Bjd;
using SmtpServer;

namespace SmtpServerTest {
    //MailSaveのモックオブジェクト
    class TestMailSave : MailSave {
        List<RetMail> Ar { get; set; }
        public TestMailSave()
            : base(null, null, null, null, null) {
            Ar = new List<RetMail>();
        }

        override public bool Save(MailAddress from, MailAddress to, Mail mail, string host, Ip addr) {
            Ar.Add(new RetMail(from, to, mail));
            return true;
        }

        public void Clear() {
            Ar.Clear();
        }

        public Mail GetMail(int i) {
            if (i < Ar.Count)
                return Ar[i].Mail;
            return null;
        }

        public MailAddress GetFrom(int i) {
            if (i < Ar.Count)
                return Ar[i].From;
            return null;
        }

        public MailAddress GetTo(int i) {
            if (i < Ar.Count)
                return Ar[i].To;
            return null;
        }

        public int Count() {
            return Ar.Count;
        }
    }
}