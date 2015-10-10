using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace Bjd
{
    public class Header : IEnumerable<OneHeader>
    {
        readonly List<OneHeader> _ar = new List<OneHeader>();

        public Header() {
        }
        public Header(Header header) {
            _ar = new List<OneHeader>(header);
        }
        public Header(byte[] buf) {
            _ar = new List<OneHeader>();

            //\r\n��r�������s�P�ʂɉ��H����
            var lines = from b in Inet.GetLines(buf) select Inet.TrimCrlf(b);
            var key = "";
            foreach (byte[] val in lines.Select(line => GetKeyVal(line, ref key))){
                Append(key, val);
            }
        }
        public Header(List<byte[]> lines) {
            _ar = new List<OneHeader>();

            var key = "";
            foreach (var l in lines) {

                //\r\n��r��
                var line = Inet.TrimCrlf(l);
                
                //�P�s���̃f�[�^����Key��Val��擾����
                byte[] val = GetKeyVal(line, ref key);
                Append(key, val);
            }
        }
        //IEnumerable<T>�̎���
        public IEnumerator<OneHeader> GetEnumerator(){
            return ((IEnumerable<OneHeader>) _ar).GetEnumerator();
        }

        //IEnumerable<T>�̎���
        System.Collections.IEnumerator
            System.Collections.IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
        //IEnumerable<T>�̎���(�֘A�v���p�e�B)
        public int Count {
            get {
                return _ar.Count;
            }
        }
        //IEnumerable<T>�̎���(�֘A���\�b�h)
        //public void ForEach(Action<OneHeader> action) {
        //    foreach (var o in ar) {
        //        action(o);
        //    }
        //}
        //GetVal��string�ɕϊ����ĕԂ�
        public string GetVal(string key) {
            //Key�̑��݊m�F
            var o = _ar.Find(h => h.Key.ToUpper() == key.ToUpper());
            return o == null ? null : Encoding.ASCII.GetString(o.Val);
        }
        //Ver5.4.4 �w�b�_�̍폜
        public void Remove(string key) {
            //Key�̑��݊m�F
            var o = _ar.Find(h => h.Key.ToUpper() == key.ToUpper());
            if (o != null) {
                _ar.Remove(o);//���݂���ꍇ�́A�폜����
            }
        }
        //5.4.4 �w�肵���w�b�_��u��������
        public void Replace(string beforeKey,string afterKey, string valStr) {

            //byte[] �ւ̕ϊ�
            var val = Encoding.ASCII.GetBytes(valStr);
            //Key�̑��݊m�F
            var o = _ar.Find(h => h.Key.ToUpper() == beforeKey.ToUpper());
            if (o == null) {
                Append(afterKey, val);//���݂��Ȃ��ꍇ�͒ǉ�
            } else {//���݂���ꍇ�͒u������
                o.Key = afterKey;
                o.Val = val;
            }
        }

        //����̃w�b�_���������ꍇ�͒u��������
        public void Replace(string key,string valStr) {
            //byte[] �ւ̕ϊ�
            var val = Encoding.ASCII.GetBytes(valStr);
            //Key�̑��݊m�F
            var o = _ar.Find(h=>h.Key.ToUpper()==key.ToUpper());
            if (o == null) {
                Append(key, val);//���݂��Ȃ��ꍇ�͒ǉ�
            } else {
                o.Val = val;//���݂���ꍇ�͒u������
            }
        }
        //����̃w�b�_�������Ă������ɒǉ�����
        public void Append(string key,byte[] val) {
            _ar.Add(new OneHeader(key,val));
        }
        public bool Recv(SockTcp sockTcp,int timeout,ILife iLife) {

            //�w�b�_�擾�i�f�[�^�͏����������j
            _ar.Clear();

            var key = "";
            while (iLife.IsLife()) {
                var line = sockTcp.LineRecv(timeout,iLife);
                if (line == null)
                    return false;
                line = Inet.TrimCrlf(line);
                if (line.Length==0)
                    return true;//�w�b�_�̏I��

                //�P�s���̃f�[�^����Key��Val��擾����
                byte[] val = GetKeyVal(line, ref key);
                if(key!=""){
                    Append(key, val);
                } else {
                    //Ver5.4.4 HTTP/1.0 200 OK��Q�s�Ԃ��T�[�o�������̂ɑΏ�
                    var s = Encoding.ASCII.GetString(line);
                    if(s.IndexOf("HTTP/")!=0)
                        return false;//�w�b�_�ُ�
                }
            }
            return false;
        }

        public byte[] GetBytes() {

            //�������̂��߁ABuffer.BlockCopy�ɏC��
            //byte[] b = new byte[0];
            //foreach(var o in Lines) {
            //    b = Bytes.Create(b,Encoding.ASCII.GetBytes(o.Key),": ",o.Val,"\r\n");
            //}
            //b = Bytes.Create(b,"\r\n");
            //return b;

            int size = 2;//�󔒍s \r\n
            _ar.ForEach(o=>{
                size += o.Key.Length+o.Val.Length+4; //':'+' '+\r+\n
            });
            var buf = new byte[size];
            int p = 0;//�������݃|�C���^
            _ar.ForEach(o=>{
                var k = Encoding.ASCII.GetBytes(o.Key);
                Buffer.BlockCopy(k, 0, buf, p, k.Length);
                p += k.Length;
                buf[p] = (byte)':';
                buf[p+1] = (byte)' ';
                p+=2;
                Buffer.BlockCopy(o.Val, 0, buf, p, o.Val.Length);
                p += o.Val.Length;
                buf[p] = (byte)'\r';
                buf[p+1] = (byte)'\n';
                p += 2;
            });
            buf[p] = (byte)'\r';
            buf[p + 1] = (byte)'\n';

            return buf;

        }
        public override string ToString() {
            var sb = new StringBuilder();
            _ar.ForEach(o=>{
                sb.Append(string.Format("{0}: {1}\r\n",o.Key,Encoding.ASCII.GetString(o.Val)));
            });
            sb.Append("\r\n");
            return sb.ToString();
        }
        //�P�s���̃f�[�^����Key��Val��擾����
        byte[] GetKeyVal(byte[] line, ref string key) {
            key = "";
            for (int i = 0; i < line.Length; i++) {
                if (key == "") {
                    if (line[i] == ':') {
                        var tmp = new byte[i];
                        Buffer.BlockCopy(line, 0, tmp, 0, i);
                        key = Encoding.ASCII.GetString(tmp);
                    }
                } else {
                    if (line[i] != ' ') {
                        var val = new byte[line.Length - i];
                        Buffer.BlockCopy(line, i, val, 0, line.Length - i);
                        return val;
                    }
                }
            }
            return new byte[0];
        }
    }
}
