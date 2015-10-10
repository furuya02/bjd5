using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace Bjd.util {
    public class Crypt {

        private Crypt() {}//�f�t�H���g�R���X�g���N�^�̉B��

        static byte[] _key;
        static byte[] _iv;

        static void Init(){
            const string password = "password";
            //RijndaelManaged aes = new RijndaelManaged();
            _key = new byte[32];
            _iv = new byte[16];
            var len = password.Length;
            for (var i = 0; i < 32; i++) 
                _key[i] = (byte)password[i%len];
            for (var i = 0; i < 16; i++) 
                _iv[i] = (byte)password[i % len];

        }

        static public string Encrypt(string str) {
            
            Init();

            try {
                var src = Encoding.Unicode.GetBytes(str);

                var aes = new RijndaelManaged();
                var ms = new MemoryStream();
                var cs = new CryptoStream(ms, aes.CreateEncryptor(_key,_iv), CryptoStreamMode.Write);
                cs.Write(src, 0, src.Length);
                cs.FlushFinalBlock();
                var dest = ms.ToArray();

                return Convert.ToBase64String(dest);

            }catch{
                return "ERROR";
            }
        }
        static public string Decrypt(string str) {
            
            Init();

            try {
                var src = Convert.FromBase64String(str);

                var aes = new RijndaelManaged();
                var ms = new MemoryStream();
                var cs = new CryptoStream(ms, aes.CreateDecryptor(_key, _iv), CryptoStreamMode.Write);
                cs.Write(src, 0, src.Length);
                cs.FlushFinalBlock();
                var dest = ms.ToArray();
                return Encoding.Unicode.GetString(dest);
                
           } catch {
                return null;
            }
        }
    }
}
