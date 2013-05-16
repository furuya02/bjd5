using System.Collections.Generic;

namespace SipServer {
    class User {
        readonly List<OneUser> _ar = new List<OneUser>();
        public User() {
        }
        public int Add(OneUser oneUser) {
            _ar.Add(oneUser);
            return _ar.Count;
        }
        public int Count() {
            return _ar.Count;
        }
    }
}
