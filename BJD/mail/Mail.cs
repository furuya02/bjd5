using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Bjd.sock;
using Bjd.util;

//using System.Linq;

namespace Bjd.mail {
    //**********************************************************************************
    //1�ʂ̃��[����\���i�ێ��j����N���X
    //**********************************************************************************
    public class Mail : LastError,IDisposable {
        //�w�b�_�ƃ{�f�B�̊Ԃ̋󔒍s�͊܂܂Ȃ�
        //\r\n�͊܂�
        List<string> _header = new List<string>();
        List<byte[]> _body = new List<byte[]>();
        //�����s�̃w�b�_�𐮗�����O�́A�e���|����
        List<string> _lines = new List<string>();
        bool _isHeader = true;//�����w�b�_�s�Ƃ��Ĉ���

        public void Dispose() {
            _header.Clear();
            _header = null;
            _body.Clear();
            _body = null;
        }

        public Encoding GetEncoding() {
            var encoding = Encoding.ASCII;
            var str = GetHeader("Content-Type");
            if (str != null) {
                str = str.ToUpper();
                var index = str.IndexOf("CHARSET");
                if (index != -1) {
                    str = str.Substring(index + 8);
                    var sb = new StringBuilder();
                    foreach (char t in str){
                        if (t == ' ')
                            continue;
                        if (t == '"') {
                            if (sb.Length != 0)
                                break;
                            continue;
                        }
                        sb.Append(t);
                    }
                    try {
                        encoding = Encoding.GetEncoding(sb.ToString());
                    } catch {
                        encoding = Encoding.ASCII;
                    }
                }
            }
            return encoding;

        }

        public void Init2(byte[] buf) {
            var lines = Inet.GetLines(buf);
            foreach (var l in lines) {
                AppendLine(l);
            }
        }

        //�s�ǉ��@\r\n��܂ނ܂܂Œǉ�����
        //�w�b�_�Ɩ{���̋�؂����������Areturn true;
        public bool AppendLine(byte[] data) {
            if (_isHeader) {//�w�b�_�ǉ�
                var str = Encoding.ASCII.GetString(data);
                
                //Ver6.1.3 �����ȃw�b�_�s�������ꍇ�A�w�b�_��I���Ƃ݂Ȃ�
                var isEspecially = false;
                //if (str != "\r\n" && str.IndexOf(':') == -1) {
                //Ver6.1.4
                //if (str != "\r\n" && str.IndexOf(' ')!=0 && str.IndexOf(':') == -1) {
                //Ver6.1.5
                if (str != "\r\n" && str.IndexOf(' ') != 0 && str.IndexOf('\t') != 0 && str.IndexOf(':') == -1){
                    isEspecially = true;
                    str = "\r\n";
                }


                if (str == "\r\n") {//�w�b�_�I��
                    //�����s�ɂ܂�����w�b�_��P�s�ɂ܂Ƃ߂�
                    foreach (string t in _lines){
                        if (t[0] == ' ' || t[0] == '\t') {
                            var buf = _header[_header.Count - 1];
                            //Ver5.9.6
                            //buf = Inet.TrimCrlf(buf) + " " + t.Substring(1);
                            buf = Inet.TrimCrlf(buf) + "\r\n" + t.Substring(0);
                            _header[_header.Count - 1] = buf;
                        } else {
                            _header.Add(t);
                        }
                    }
                    _lines = null;
                    _isHeader = false;//�w�b�_�s�I��

                    //Ver6.1.3 �����ȃw�b�_�s�������ꍇ�A�w�b�_��I���Ƃ݂Ȃ�
                    if (isEspecially) {
                        _body.Add(data);
                    }
                    return true;
                }
                _lines.Add(str);
            } else {
                _body.Add(data);
            }
            return false;
        }

        public Mail CreateClone() {
            var mail = new Mail();
            //�w�b�_�s
            _header.ForEach(s => mail.AppendLine(Encoding.ASCII.GetBytes(s)));
            //��؂�s
            mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));
            //�{��
            _body.ForEach(d => mail.AppendLine(d));
            return mail;
        }

        //���[���̃T�C�Y
        public long Length {
            get {
                long length = 0;
                _header.ForEach(s => length += s.Length);//�w�b�_
                length += 2;//��؂�s
                _body.ForEach(d => length += d.Length);//�{��
                return length;
            }
        }
        //�w�b�_�擾�i���݂��Ȃ��ꍇ��,null���Ԃ����j
        public string GetHeader(string tag) {
            foreach (var line in _header) {
                var i = line.IndexOf(':');
                if (0 > i)
                    continue;
                if (line.Substring(0, i).ToUpper() == tag.ToUpper()) {
                    return Inet.TrimCrlf(line).Substring(i + 1).Trim(' ');
                }
            }
            return null;
        }
        //�w�b�_�ǉ�
        public void AddHeader(string tag, string str) {
            var buf = string.Format("{0}: {1}\r\n", tag, str);
            if (tag.ToUpper() == "RECEIVED") {
                //�ŏ㕔�ɒǉ�����
                _header.Insert(0, buf);
            } else {
                _header.Add(buf);
            }
        }

        //�w�b�_�̒u������
        public void ConvertHeader(string tag, string str) {

            if (null == GetHeader(tag)) {
                AddHeader(tag, str);
                return;
            }

            var tmp = new List<string>();
            foreach (string line in _header) {
                int i = line.IndexOf(':');
                if (0 <= i) {
                    if (line.Substring(0, i).ToUpper() == tag.ToUpper()) {
                        string buf = string.Format("{0}: {1}\r\n", tag, str);
                        tmp.Add(buf);
                    } else {
                        tmp.Add(line);
                    }
                }
            }
            _header = tmp;
        }
        //�w�b�_�u��(���K�\���ɂ��p�^�[���}�b�`)
        public bool RegexHeader(string pattern, string after) {

            var regex = new Regex(pattern);
            for (var i = 0; i < _header.Count; i++){

                
                if (!regex.Match(_header[i]).Success)
                    continue;
                if (after == "") {
                    _header.RemoveAt(i);
                } else {
                    _header[i] = Regex.Replace(_header[i], pattern, after);
                    //_header[i] = after;
                }
                return true;
            }
            return false;
        }

        //�t�@�C���ւ̒ǉ���������
        public bool Append(string fileName) {
            return Save1(fileName, FileMode.Append);
        }
        //�t�@�C���ւ̕ۑ�
        public bool Save(string fileName) {
            return Save1(fileName, FileMode.Create);
        }
        //�t�@�C���ւ̕ۑ�(������\�b�h)
        bool Save1(string fileName, FileMode fileMode) {
            try {
                using (var bw = new BinaryWriter(new FileStream(fileName, fileMode, FileAccess.Write))) {

                    _header.ForEach(s => bw.Write(Encoding.ASCII.GetBytes(s)));

                    bw.Write(Encoding.ASCII.GetBytes("\r\n"));//��؂�s
                    _body.ForEach(bw.Write);

                    bw.Flush();
                    bw.Close();
                }
                return true;
            } catch (Exception ex) {
                //Ver5.9.2
                SetLastError(ex.Message);
            }
            return false;
        }

        //�t�@�C������̎擾
        public bool Read(string fileName) {

            //���݂̓�e����ׂĔj�����ēǂݒ���
            _header.Clear();
            _body.Clear();
            _body = new List<byte[]>();

            if (File.Exists(fileName)) {
                var tmpBuf = new byte[0];
                using (var br = new BinaryReader(new FileStream(fileName, FileMode.Open))) {
                    var info = new FileInfo(fileName);
                    while (true) {
                        var len = info.Length - tmpBuf.Length;
                        if (len <= 0)
                            break;
                        if (len > 65535)
                            len = 65535;
                        var tmp = br.ReadBytes((int)len);
                        tmpBuf = Bytes.Create(tmpBuf, tmp);
                    }
                    br.Close();

                    var lines = Inet.GetLines(tmpBuf);
                    var head = true;
                    foreach (byte[] line in lines) {
                        if (head) {
                            var str = Encoding.ASCII.GetString(line);
                            if (str == "\r\n") {
                                head = false;
                                continue;
                            }
                            _header.Add(str);
                        } else {
                            _body.Add(line);
                        }
                    }
                    return true;
                }
            }
            return false;

        }
        //���M
        //count �{���̍s���i-1�̂Ƃ��͑S���j
        public bool Send(SockTcp sockTcp, int count) {
            try {
                _header.ForEach(s => sockTcp.SendUseEncode(Encoding.ASCII.GetBytes(s)));

                sockTcp.SendUseEncode(Encoding.ASCII.GetBytes("\r\n"));//��؂�s

                if (count == -1) {
                    _body.ForEach(d => sockTcp.SendUseEncode(d));
                } else {
                    for (int i = 0; i < count && i < _body.Count; i++) {
                        sockTcp.SendUseEncode(_body[i]);
                    }
                }
                return true;
            } catch (Exception ex) {
                //Ver5.9.2
                SetLastError(ex.Message);
                return false;
            }


        }
        //�w�b�_��܂ޑS���̎擾
        public Byte[] GetBytes() {

            var buf = new byte[Length];
            var pos = 0;
            //�w�b�_
            _header.ForEach(s => {
                var d = Encoding.ASCII.GetBytes(s);
                Buffer.BlockCopy(d, 0, buf, pos, d.Length);
                pos += d.Length;
            });
            //��؂�
            buf[pos] = 0x0d;
            pos++;
            buf[pos] = 0x0a;
            pos++;
            //�{��
            _body.ForEach(d => {
                Buffer.BlockCopy(d, 0, buf, pos, d.Length);
                pos += d.Length;
            });

            return buf;

        }

        //�{���݂̂̎擾
        public Byte[] GetBody() {
            var length = 0;
            _body.ForEach(d => length += d.Length);

            var buf = new byte[length];
            var pos = 0;
            _body.ForEach(d => {
                Buffer.BlockCopy(d, 0, buf, pos, d.Length);
                pos += d.Length;
            });
            return buf;
        }

    }
}
