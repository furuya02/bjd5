using System;

namespace Bjd.sock{
    //SockTcpで使用されるデータキュー
    public class SockQueue{
        byte[] _db = new byte[0]; //現在のバッファの内容
        //private static int max = 1048560; //保持可能な最大数<=この辺りが適切な値かもしれない
        private const int max = 2000000; //保持可能な最大数
        //TODO modifyの動作に不安あり（これ必要なのか？） 
        bool _modify; //バッファに追加があった場合にtrueに変更される

        public int Max{ get { return max; } }

        //空いているスペース
        public int Space { get { return max - _db.Length; } }

        //現在のキューに溜まっているデータ量
        public int Length { get { return _db.Length; } }

        //キューへの追加
        public int Enqueue(byte[] buf, int len){
            
            if (Space == 0){
                return 0;
            }
            //空きスペースを越える場合は失敗する 0が返される
            if (Space < len){
                return 0;
            }

            lock (this){
                var tmpBuf = new byte[_db.Length + len]; //テンポラリバッファ
                Buffer.BlockCopy(_db, 0, tmpBuf, 0, _db.Length);//現有DBのデータをテンポラリ前部へコピー
                Buffer.BlockCopy(buf, 0, tmpBuf, _db.Length, len);//追加のデータをテンポラリ後部へコピー
                _db = tmpBuf; //テンポラリを現用DBへ変更
                _modify = true; //データベースの内容が変化した
                return len;
            }

        }

        //キューからのデータ取得
        public byte[] Dequeue(int len){
            if (_db.Length == 0 || len == 0 || !_modify){
                return new byte[0];
            }

            lock (this){
                //要求サイズが現有数を超える場合はカットする
                if (_db.Length < len){
                    len = _db.Length;
                }
                var retBuf = new byte[len]; //出力用バッファ
                var tmpBuf = new byte[_db.Length - len]; //テンポラリバッファ
                Buffer.BlockCopy(_db, 0, retBuf, 0, len);//現有DBから出力用バッファへコピー
                Buffer.BlockCopy(_db, len, tmpBuf, 0, _db.Length - len);//残りのデータをテンポラリへ
                _db = tmpBuf; //テンポラリを現用DBへ変更
                if (_db.Length == 0){
                    _modify = false; //次に何か受信するまで処理の必要はない
                }

                return retBuf;
            }
        }

        //キューからの１行取り出し(\r\nを削除しない)
        public byte[] DequeueLine(){
            if (!_modify){
                return new byte[0];
            }
            lock (this){
                for (var i = 0; i < _db.Length; i++){
                    if (_db[i] != '\n'){
                        continue;
                    }
                    var retBuf = new byte[i + 1]; //\r\nを削除しない
                    Buffer.BlockCopy(_db, 0, retBuf, 0, i + 1);//\r\nを削除しない
                    var tmpBuf = new byte[_db.Length - (i + 1)]; //テンポラリバッファ
                    Buffer.BlockCopy(_db, (i + 1), tmpBuf, 0, _db.Length - (i + 1));//残りのデータをテンポラリへ
                    _db = tmpBuf; //テンポラリを現用DBへ変更

                    return retBuf;

                }
                _modify = false; //次に何か受信するまで処理の必要はない
                return new byte[0];
            }
        }
    }
}
/*    public class TcpQueue {
        byte[] _db = new byte[0];//現在のバッファの内容
        //Ver5.1.3
        //int max = 1000000;//保持可能な最大数<=この辺りが適切な値かもしれない
        private const int Max = 1048560; //保持可能な最大数<=この辺りが適切な値かもしれない
        bool _modify;//バッファに追加があった場合にtrueに変更される

        //空いているスペース
        public int Space {
            get {
                return Max - _db.Length;
            }
        }
        //現在のキューに溜まっているデータ量
        public int Length {
            get {
                return _db.Length;
            }
        }

        //キューへの追加
        //return 追加したバイト数
        public int Enqueue(byte[] buf, int len) {
            if (Space == 0)
                return 0;
            lock (this) {
                //空きスペースを越える場合はカットする
                if (Space < len) {
                    len -= Space;
                }

                var tmpBuf = new byte[_db.Length + len];//テンポラリバッファ
                Buffer.BlockCopy(_db, 0, tmpBuf, 0, _db.Length);//現有DBのデータをテンポラリ前部へコピー
                Buffer.BlockCopy(buf, 0, tmpBuf, _db.Length, len);//追加のデータをテンポラリ後部へコピー
                _db = tmpBuf;//テンポラリを現用DBへ変更

                _modify = true;//データベースの内容が変化した

                return len;
            }
        }

        //キューからのデータ取得
        public byte[] Dequeue(int len) {
            if (_db.Length == 0 || len == 0 || !_modify) {
                return null;
            }

            lock (this) {
                //要求サイズが現有数を超える場合はカットする
                if (_db.Length < len) {
                    len = _db.Length;
                }
                var retBuf = new byte[len];//出力用バッファ
                var tmpBuf = new byte[_db.Length - len];//テンポラリバッファ
                Buffer.BlockCopy(_db, 0, retBuf, 0, len);//現有DBから出力用バッファへコピー
                Buffer.BlockCopy(_db, len, tmpBuf, 0, _db.Length - len);//残りのデータをテンポラリへ
                _db = tmpBuf;//テンポラリを現用DBへ変更

                if (_db.Length == 0)
                    _modify = false;//次に何か受信するまで処理の必要はない

                return retBuf;
            }
        }

        //バッファの最後が\r\nかどうかの判断
        //DequeueLine()で使用される
        //bool IsCrlf(byte[] buffer, int len) {
        //    if (len < 1)
        //        return false;
        //    if (buffer[len - 1] == '\r' && buffer[len] == '\n')
        //        return true;
        //    return false;
        //}

        //キューからの１行取り出し(\r\nを削除しない)
        public byte[] DequeueLine() {
            if (!_modify)
                return new byte[0];
            lock (this) { //排他制御
                for (int i = 0; i < _db.Length; i++) {
                    if (_db[i] != '\n')
                        continue;
                    var retBuf = new byte[i + 1];//\r\nを削除しない
                    Buffer.BlockCopy(_db, 0, retBuf, 0, i + 1);//\r\nを削除しない
                    var tmpBuf = new byte[_db.Length - (i + 1)];//テンポラリバッファ
                    Buffer.BlockCopy(_db, (i + 1), tmpBuf, 0, _db.Length - (i + 1));//残りのデータをテンポラリへ
                    _db = tmpBuf;//テンポラリを現用DBへ変更

                    return retBuf;
                }
                _modify = false;//次に何か受信するまで処理の必要はない
                return new byte[0];
            }
        }
    }
*/

