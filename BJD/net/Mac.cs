using System;

namespace Bjd.net{
    //MACアドレスを表現するクラス
    //ValidObjを継承

    public class Mac : ValidObj{

        private byte[] m = new byte[6];

        //コンストラクタ(文字列)
        //初期化文字列でMACアドレスを初期化する
        //文字列が無効で初期化に失敗した場合は、例外(IllegalArgumentException)がスローされる
        //初期化に失敗したオブジェクトを使用すると「実行時例外」が発生するので、生成時に必ず例外処理しなければならない
        //ValidObjException 初期化失敗
        public Mac(String macStr){
            if (macStr.Length != 17){
                ThrowException("buf.length!=6"); //初期化失敗
            }
            try{
                for (int i = 0; i < 6; i++){
                    m[i] = (byte) Convert.ToInt32(macStr.Substring(i*3, 2), 16);
                }
            }
            catch (Exception){
                ThrowException("buf.length!=6"); //初期化失敗
            }

        }

        //コンストラクタ (バイトオーダ)
        //buf 6バイトで表現されたMACアドレス
        public Mac(byte[] buf){
//            if (buf.Length != 6){
//                ThrowException("buf.length!=6"); //初期化失敗
//            }
            if (buf.Length < 6) {
                ThrowException("buf.length < 6"); //初期化失敗
            }
            for (int i = 0; i < 6; i++) {
                m[i] = buf[i];
            }
        }

        //初期化
        protected override void Init(){
            for (int i = 0; i < 6; i++){
                m[i] = 0;
            }
        }

        //バイトオーダの取得
        public byte[] GetBytes(){
            CheckInitialise();
            return m;
        }

        //比較
        // ReSharper disable InconsistentNaming
        public override bool Equals(Object obj){
            // ReSharper restore InconsistentNaming
            CheckInitialise();
            if (obj == null){
                return false;
            }
            if (obj is Mac){
                byte[] o = ((Mac) obj).GetBytes();
                for (int i = 0; i < 6; i++){
                    if (o[i] != m[i]){
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        
        public override int GetHashCode() {
            return base.GetHashCode();
        }


        //public int HashCode(){
        //    return -1; //super.hashCode();
        //}

        //文字列化
        public new String ToString(){
            CheckInitialise();
            return string.Format("{0:X2}-{1:X2}-{2:X2}-{3:X2}-{4:X2}-{5:X2}", m[0], m[1], m[2], m[3], m[4], m[5]);
        }
        //Ver5.4.1追加
        public static bool operator ==(Mac a, Mac b) {
            if (ReferenceEquals(a, b)) {
                return true;
            }
            if (((object)a == null) || ((object)b == null)) {
                return false;
            }
            var al = a.GetBytes();
            var bl = b.GetBytes();
            for (int i = 0; i < 6; i++) {
                if (al[i] != bl[i])
                    return false;
            }
            return true;
        }
        //Ver5.4.1追加
        public static bool operator !=(Mac a, Mac b) {
            return !(a == b);
        }
    }
}

