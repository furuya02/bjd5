
namespace SipServer {
    
    class SipVer {
        public float No { get; private set; }    
        public SipVer(string str) {
            No = 0;

            if (str.IndexOf("SIP/") == 0) {
                try {
                    No = float.Parse(str.Substring(4));
                } catch {
                    No = 0;
                }
            }
        }
        public SipVer() {
            No = 0;
        }
    }
}
