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
    //1通のメールを表現（保持）するクラス
    //**********************************************************************************
    public class Mail : LastError,IDisposable {
        //ヘッダとボディの間の空白行は含まない
        //\r\nは含む
        List<string> _header = new List<string>();
        List<byte[]> _body = new List<byte[]>();
        //複数行のヘッダを整理する前の、テンポラリ
        List<string> _lines = new List<string>();
        bool _isHeader = true;//当初ヘッダ行として扱う

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

        //行追加　\r\nを含むままで追加する
        //ヘッダと本文の区切りを見つけた時、return true;
        public bool AppendLine(byte[] data) {
            if (_isHeader) {//ヘッダ追加
                var str = Encoding.ASCII.GetString(data);
                
                //Ver6.1.3 無効なヘッダ行が来た場合、ヘッダを終了とみなす
                var isEspecially = false;
                if (str != "\r\n" && str.IndexOf(':') == -1) {
                    isEspecially = true;
                    str = "\r\n";
                }


                if (str == "\r\n") {//ヘッダ終了
                    //複数行にまたがるヘッダを１行にまとめる
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
                    _isHeader = false;//ヘッダ行終了

                    //Ver6.1.3 無効なヘッダ行が来た場合、ヘッダを終了とみなす
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
            //ヘッダ行
            _header.ForEach(s => mail.AppendLine(Encoding.ASCII.GetBytes(s)));
            //区切り行
            mail.AppendLine(Encoding.ASCII.GetBytes("\r\n"));
            //本文
            _body.ForEach(d => mail.AppendLine(d));
            return mail;
        }

        //メールのサイズ
        public long Length {
            get {
                long length = 0;
                _header.ForEach(s => length += s.Length);//ヘッダ
                length += 2;//区切り行
                _body.ForEach(d => length += d.Length);//本文
                return length;
            }
        }
        //ヘッダ取得（存在しない場合は,nullが返される）
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
        //ヘッダ追加
        public void AddHeader(string tag, string str) {
            var buf = string.Format("{0}: {1}\r\n", tag, str);
            if (tag.ToUpper() == "RECEIVED") {
                //最上部に追加する
                _header.Insert(0, buf);
            } else {
                _header.Add(buf);
            }
        }

        //ヘッダの置き換え
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
        //ヘッダ置換(正規表現によるパターンマッチ)
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

        //ファイルへの追加書き込み
        public bool Append(string fileName) {
            return Save1(fileName, FileMode.Append);
        }
        //ファイルへの保存
        public bool Save(string fileName) {
            return Save1(fileName, FileMode.Create);
        }
        //ファイルへの保存(内部メソッド)
        bool Save1(string fileName, FileMode fileMode) {
            try {
                using (var bw = new BinaryWriter(new FileStream(fileName, fileMode, FileAccess.Write))) {

                    _header.ForEach(s => bw.Write(Encoding.ASCII.GetBytes(s)));

                    bw.Write(Encoding.ASCII.GetBytes("\r\n"));//区切り行
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

        //ファイルからの取得
        public bool Read(string fileName) {

            //現在の内容をすべて破棄して読み直す
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
        //送信
        //count 本文の行数（-1のときは全部）
        public bool Send(SockTcp sockTcp, int count) {
            try {
                _header.ForEach(s => sockTcp.SendUseEncode(Encoding.ASCII.GetBytes(s)));

                sockTcp.SendUseEncode(Encoding.ASCII.GetBytes("\r\n"));//区切り行

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
        //ヘッダを含む全部の取得
        public Byte[] GetBytes() {

            var buf = new byte[Length];
            var pos = 0;
            //ヘッダ
            _header.ForEach(s => {
                var d = Encoding.ASCII.GetBytes(s);
                Buffer.BlockCopy(d, 0, buf, pos, d.Length);
                pos += d.Length;
            });
            //区切り
            buf[pos] = 0x0d;
            pos++;
            buf[pos] = 0x0a;
            pos++;
            //本文
            _body.ForEach(d => {
                Buffer.BlockCopy(d, 0, buf, pos, d.Length);
                pos += d.Length;
            });

            return buf;

        }

        //本文のみの取得
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
