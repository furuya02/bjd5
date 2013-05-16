using System;
using System.Text;

namespace Bjd.util{
    public static class Base64{
        //エンコードがASCIIの場合は、このクラスを使用する

        //Base64のデコード
        public static string Decode(string str){
            //Ver5.7.0 例外への対応
            try{
                return Encoding.UTF8.GetString(Convert.FromBase64String(str));
            } catch{
            }
            return "";
        }

        //Base64のエンコード(ASCII版)
        public static string Encode(string str){
            //Ver5.7.0 例外への対応
            try{
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
            } catch{
            }
            return "";

        }
    }
}
