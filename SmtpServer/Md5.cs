using System;
using System.Text;

namespace SmtpServer {
    class Md5 {
        //MD5によるハッシュ作成
        static public string Hash(string passStr,string timestampStr) {
            const int range = 64;
            var pass = Encoding.ASCII.GetBytes(passStr);
            var timestamp = Encoding.ASCII.GetBytes(timestampStr);
            var h = new System.Security.Cryptography.MD5CryptoServiceProvider();
            var k = new byte[range];
            if (range < pass.Length)
                throw new InvalidOperationException("key length is too long");
            var ipad = new byte[range];
            var opad = new byte[range];
            pass.CopyTo(k,0);
            for (var i = pass.Length; i < range; i++) {
                k[i] = 0x00;
            }
            for (var i = 0; i < range; i++) {
                ipad[i] = (byte)(k[i] ^ 0x36);
                opad[i] = (byte)(k[i] ^ 0x5c);
            }
            var hi = new byte[ipad.Length + timestamp.Length];
            ipad.CopyTo(hi,0);
            timestamp.CopyTo(hi,ipad.Length);
            var hash = h.ComputeHash(hi);
            var ho = new byte[opad.Length + hash.Length];
            opad.CopyTo(ho,0);
            hash.CopyTo(ho,opad.Length);
            h.Initialize();
            var tmp = h.ComputeHash(ho);

            var sb = new StringBuilder();
            foreach (var b in tmp) {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
