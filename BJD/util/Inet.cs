using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using Bjd.log;
using Bjd.net;
using Bjd.sock;

namespace Bjd.util {
    public class Inet {

        private Inet(){}//�f�t�H���g�R���X�g���N�^�̉B��


        //**************************************************************************
        //�o�C�i��-������ϊ�(�o�C�i���f�[�^��e�L�X�g�����đ���M���邽�ߎg�p����)
        //**************************************************************************
        static public byte[] ToBytes(string str) {
            if(str==null){
                str = "";
            }
            return Encoding.Unicode.GetBytes(str);
        }
        static public string FromBytes(byte [] buf){
            if(buf==null){
                buf = new byte[0];
            }
            return Encoding.Unicode.GetString(buf);
        }
        //**************************************************************************

        //Ver5.0.0-a11 ������
        //�e�L�X�g�����N���X�@string��\r\n��List<string>�ɕ�������
        static public List<string> GetLines(string str){
            return str.Split(new[]{"\r\n"}, StringSplitOptions.None).ToList();
        }

        //�s�P�ʂł̐؂�o��(\r\n�͍폜���Ȃ�)
        static public List<byte []> GetLines(byte [] buf) {
            var lines = new List<byte[]>();

            if(buf==null || buf.Length==0){
                return lines;
            }
            int start = 0;
            for (var end = 0;; end++) {
                if (buf[end] == '\n') {
                    if (1 <= end && buf[end - 1] == '\r') {
                        var tmp = new byte[end - start + 1];//\r\n��폜���Ȃ�
                        Buffer.BlockCopy(buf,start,tmp,0,end - start + 1);//\r\n��폜���Ȃ�
                        lines.Add(tmp);
                        //string str = Encoding.ASCII.GetString(tmp);
                        //ar.Add(str);
                        start = end + 1;
                    //Unicode
                    } else if(2 <= end && end + 1 < buf.Length && buf[end + 1] == '\0' && buf[end - 1] == '\0' && buf[end - 2] == '\r') {
                        var tmp = new byte[end - start + 2];//\r\n��폜���Ȃ�
                        Buffer.BlockCopy(buf,start,tmp,0,end - start + 2);//\r\n��폜���Ȃ�
                        lines.Add(tmp);
                        start = end + 2;
                    } else {//\n�̂�
                        var tmp = new byte[end - start + 1];//\n��폜���Ȃ�
                        Buffer.BlockCopy(buf,start,tmp,0,end - start + 1);//\n��폜���Ȃ�
                        lines.Add(tmp);
                        start = end + 1;
                    }
                }
                if (end >= buf.Length-1) {
                    if (0 < (end - start + 1)) {
                        var tmp = new byte[end - start + 1];//\r\n��폜���Ȃ�
                        Buffer.BlockCopy(buf,start,tmp,0,end - start + 1);//\r\n��폜���Ȃ�
                        lines.Add(tmp);
                    }
                    break;
                }
            }
            return lines;
        }
        //\r\n�̍폜
        static public byte[] TrimCrlf(byte[] buf) {
            if(buf.Length >= 1 && buf[buf.Length - 1] == '\n') {
                var count=1;
                if(buf.Length >= 2 && buf[buf.Length - 2] == '\r') {
                    count++;
                }
                var tmp = new byte[buf.Length-count];
                Buffer.BlockCopy(buf,0,tmp,0,buf.Length - count);
                return tmp;
            }
            return buf;
        }
        //\r\n�̍폜
        static public string TrimCrlf(string str) {
            if(str.Length >= 1 && str[str.Length - 1] == '\n') {
                var count = 1;
                if(str.Length >= 2 && str[str.Length - 2] == '\r') {
                    count++;
                }
                return str.Substring(0,str.Length - count);
            }
            return str;
        }
        
        //�T�j�^�C�Y����(�P�s�Ή�)
        public static string Sanitize(string str) {
            str = Util.SwapStr("&", "&amp;", str);
            str = Util.SwapStr("<", "&lt;", str);
            str = Util.SwapStr(">", "&gt;", str);
            //Ver5.0.0-a17            
            str = Util.SwapStr("~", "%7E", str);
            return str;

        }
      
        //�b��
        static public SockTcp Connect(Kernel kernel,Ip ip,int port,int timeout,Ssl ssl){
            return new SockTcp(kernel,ip,port,timeout,ssl);
        }


        //�N���C�A���g�\�P�b�g��쐬���đ����ɐڑ�����
//        static public SockTcp Connect(Kernel kernel,ref bool life, Logger logger, Ip ip, Int32 port, Ssl ssl) {
//            int timeout = 3;
//            var sockTcp = new SockTcp(kernel,ip, port, timeout,ssl);
//
//            Thread.Sleep(0);
//            while (life) {
//                if (sockTcp.SockState == SockState.Connect) {
//                    return sockTcp;
//                }
//                if (sockTcp.SockState == SockState.Error) {
//                    sockTcp.Close();//2009.06.01�ǉ�
//                    return null;
//                }
//                Thread.Sleep(10);
//            }
//            sockTcp.Close();//2009.06.01�ǉ�
//            return null;
//        }
        

        //�w�肵�������̃����_���������擾����i�`�������W������p�j
        static public string ChallengeStr(int len) {
            const string val = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var bytes = new byte[len];
            var rngcsp = new RNGCryptoServiceProvider();
            rngcsp.GetNonZeroBytes(bytes);
            
            // �������ƂɎg�p������g�ݍ��킹��
            var str = String.Empty;
            foreach (var b in bytes) {
                var rnd = new Random(b);
                int index = rnd.Next(val.Length);
                str += val[index];
            }
            return str;
        }
        //�n�b�V��������̍쐬�iMD5�j
        static public string Md5Str(string str) {
            if(str==null){
                return "";
            }
            var md5EncryptionObject = new MD5CryptoServiceProvider();
            var originalStringBytes = Encoding.Default.GetBytes(str);
            var encodedStringBytes = md5EncryptionObject.ComputeHash(originalStringBytes);
            return BitConverter.ToString(encodedStringBytes);
        }

        //���N�G�X�g�s��URL�G���R�[�h����Ă���ꍇ�́A���̕����R�[�h��擾����
        static public Encoding GetUrlEncoding(string str) {
            var tmp = str.Split(' ');
            if(tmp.Length >= 3)
                str = tmp[1];

            var buf = new byte[str.Length];
            var len = 0;
            var find = false;
            for(int i = 0;i < str.Length;i++) {
                if(str[i] == '%') {
                    find = true;
                    var hex = string.Format("{0}{1}",str[i + 1],str[i + 2]);
                    var n = Convert.ToInt32(hex,16);
                    buf[len++] = (byte)n;
                    i += 2;
                } else {
                    buf[len++] = (byte)str[i];
                }
            }
            if(!find)
                return Encoding.ASCII;
            var buf2 = new byte[len];
            Buffer.BlockCopy(buf,0,buf2,0,len);
            return MLang.GetEncoding(buf2);
        }

        static public List<String> RecvLines(SockTcp cl,int sec,ILife iLife){
            var lines = new List<string>();
            while (true) {
                var buf = cl.LineRecv(sec, iLife);
                if (buf == null)
                    break;
                if (buf.Length==0)
                    break;
                var s = Encoding.ASCII.GetString(TrimCrlf(buf));
                //if (s == "")
                //    break;
                lines.Add(s);
            }
            return lines;
        } 
    }

}
