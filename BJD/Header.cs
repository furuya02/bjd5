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

            //\r\nを排除した行単位に加工する
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

                //\r\nを排除
                var line = Inet.TrimCrlf(l);
                
                //１行分のデータからKeyとValを取得する
                byte[] val = GetKeyVal(line, ref key);
                Append(key, val);
            }
        }
        //IEnumerable<T>の実装
        public IEnumerator<OneHeader> GetEnumerator(){
            return ((IEnumerable<OneHeader>) _ar).GetEnumerator();
        }

        //IEnumerable<T>の実装
        System.Collections.IEnumerator
            System.Collections.IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
        //IEnumerable<T>の実装(関連プロパティ)
        public int Count {
            get {
                return _ar.Count;
            }
        }
        //IEnumerable<T>の実装(関連メソッド)
        //public void ForEach(Action<OneHeader> action) {
        //    foreach (var o in ar) {
        //        action(o);
        //    }
        //}
        //GetValをstringに変換して返す
        public string GetVal(string key) {
            //Keyの存在確認
            var o = _ar.Find(h => h.Key.ToUpper() == key.ToUpper());
            return o == null ? null : Encoding.ASCII.GetString(o.Val);
        }
        //Ver5.4.4 ヘッダの削除
        public void Remove(string key) {
            //Keyの存在確認
            var o = _ar.Find(h => h.Key.ToUpper() == key.ToUpper());
            if (o != null) {
                _ar.Remove(o);//存在する場合は、削除する
            }
        }
        //5.4.4 指定したヘッダを置き換える
        public void Replace(string beforeKey,string afterKey, string valStr) {

            //byte[] への変換
            var val = Encoding.ASCII.GetBytes(valStr);
            //Keyの存在確認
            var o = _ar.Find(h => h.Key.ToUpper() == beforeKey.ToUpper());
            if (o == null) {
                Append(afterKey, val);//存在しない場合は追加
            } else {//存在する場合は置き換え
                o.Key = afterKey;
                o.Val = val;
            }
        }

        //同一のヘッダがあった場合は置き換える
        public void Replace(string key,string valStr) {
            //byte[] への変換
            var val = Encoding.ASCII.GetBytes(valStr);
            //Keyの存在確認
            var o = _ar.Find(h=>h.Key.ToUpper()==key.ToUpper());
            if (o == null) {
                Append(key, val);//存在しない場合は追加
            } else {
                o.Val = val;//存在する場合は置き換え
            }
        }
        //同一のヘッダがあっても無条件に追加する
        public void Append(string key,byte[] val) {
            _ar.Add(new OneHeader(key,val));
        }
        public bool Recv(SockTcp sockTcp,int timeout,ILife iLife) {

            //ヘッダ取得（データは初期化される）
            _ar.Clear();

            var key = "";
            while (iLife.IsLife()) {
                var line = sockTcp.LineRecv(timeout,iLife);
                if (line == null)
                    return false;
                line = Inet.TrimCrlf(line);
                if (line.Length==0)
                    return true;//ヘッダの終了

                //１行分のデータからKeyとValを取得する
                byte[] val = GetKeyVal(line, ref key);
                if(key!=""){
                    Append(key, val);
                } else {
                    //Ver5.4.4 HTTP/1.0 200 OKを２行返すサーバがいるものに対処
                    var s = Encoding.ASCII.GetString(line);
                    if(s.IndexOf("HTTP/")!=0)
                        return false;//ヘッダ異常
                }
            }
            return false;
        }

        public byte[] GetBytes() {

            //高速化のため、Buffer.BlockCopyに修正
            //byte[] b = new byte[0];
            //foreach(var o in Lines) {
            //    b = Bytes.Create(b,Encoding.ASCII.GetBytes(o.Key),": ",o.Val,"\r\n");
            //}
            //b = Bytes.Create(b,"\r\n");
            //return b;

            int size = 2;//空白行 \r\n
            _ar.ForEach(o=>{
                size += o.Key.Length+o.Val.Length+4; //':'+' '+\r+\n
            });
            var buf = new byte[size];
            int p = 0;//書き込みポインタ
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
        //１行分のデータからKeyとValを取得する
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
