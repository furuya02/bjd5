using System;
using System.Collections.Generic;
using System.Text;


namespace SmtpServer {
    //subject 特別処理
    class Subject {
        public static string Decode(ref Encoding encoding,string text){
            //Subjectがエンコードされている場合
            if(text.IndexOf("=?") == 0 && text.LastIndexOf("?=") == text.Length - 2) {
                //デコード処理（複数行に対応）
                var lines = new List<string>();
                //Ver6.0.9
                //if(text.IndexOf("= =") != 0) {
                //    lines.AddRange(text.Split(' '));
                //} else {
                //    lines.Add(text);
                //}
                foreach (var l in text.Split('\n')) {
                    lines.Add(l.Trim(new []{'\r','\t'}));
                }
                //各行をそれぞれでコードしてsbに蓄積する
                var sb = new StringBuilder();
                foreach(var line in lines) {
                    var s = line.Split('?');
                    if (s.Length != 5 || s[2] != "B")
                        continue;
                    encoding = Encoding.GetEncoding(s[1]);

                    sb.Append(Encoding.ASCII.GetString(Convert.FromBase64String(s[3])));
                    //sb.Append(Base64.Decode(s[3],encoding));
                }
                return sb.ToString();//ピュアテキストの取得
            }
            return text;//デコードなしでピュアテキストとする
        }

        public static string Encode(Encoding encoding,string text) {
            if(encoding != null) {
                //エンコード処理する
                return string.Format("=?{0}?B?{1}?=", encoding.HeaderName, Convert.ToBase64String(encoding.GetBytes(text)));
            }
            return text;//ピュアテキストのまま返す
        }
    }
}
