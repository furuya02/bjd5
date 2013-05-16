using System.Collections.Generic;

namespace SipServer {
    class JobRegister {
        readonly User _user;

        public string Via{get;private set;}
        public string UserAgent { get; private set; }

        
        public JobRegister(User user) {
            _user = user;

        }
        public void Read(List<string> lines) {
            foreach (var l in lines) {
                if (l.IndexOf("Via:") == 0) {
                    Via = l.Split(new[]{':'},2)[0].Trim();
                } else if (l.IndexOf("User-Agent") == 0) {
                    UserAgent = l.Split(new[] { ':' }, 2)[0].Trim();
                }
            }
        }
    }
}
