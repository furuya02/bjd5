using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Pop3Server {
    class APop {
        
        //認証
        public static bool Auth(String user, String pass, string authStr, string recvStr) {
            if (pass == null) {
                return false;
            }
            var data = Encoding.ASCII.GetBytes(authStr + pass);
            var md5 = new MD5CryptoServiceProvider();
            var result = md5.ComputeHash(data);
            var sb = new StringBuilder();
            for (int i = 0; i < 16; i++) {
                sb.Append(string.Format("{0:x2}", result[i]));
            }
            if (sb.ToString() == recvStr)
                return true;
            return false;
        }

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        //AUTH文字列の生成
        public static string CreateAuthStr(string serverName){
            var random = new Random();
            return string.Format("<{0}.{1}@{2}>", random.Next(GetCurrentThreadId()), DateTime.Now.Ticks, serverName);
        }
    }
}
