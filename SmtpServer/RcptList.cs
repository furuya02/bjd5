using System.Collections.Generic;
using System.Linq;
using Bjd;
using Bjd.mail;

namespace SmtpServer {
    public class RcptList {
        readonly List<MailAddress> _ar = new List<MailAddress>();

        public void Add(MailAddress mailAddress){
            //d•¡’Ç‰Á‚Í‚Å‚«‚È‚¢
            if (_ar.Any(p => p.Compare(mailAddress))){
                return;
            }
            _ar.Add(mailAddress);
        }

        public MailAddress this[int n] {
            get {
                return _ar[n];
            }
        }
        public IEnumerator<MailAddress> GetEnumerator() {
            for (var i = 0;i<_ar.Count;i++)
                yield return _ar[i];
        }
        public void Clear() {
            _ar.Clear();
        }
        public int Count {
            get {
                return _ar.Count;
            }
        }

    }
}
