using Bjd;
using Bjd.net;

namespace SipServer {
    class OneUser {
        public string UserName { get; private set; }
        public string Password { get; private set; }//空の場合、パスワードなし
        public Ip Ip { get; private set; }//0.0.0.0を指定すると、何処からでもREGISTERを受け付ける（DHCPクライアントの用）

        public OneUser(string userName,string password,Ip ip) {
            UserName = userName;
            Password = password;
            Ip = ip;
        }
    }
}
