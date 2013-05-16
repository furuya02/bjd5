using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Bjd{
    //オプションを記憶するＤＢ
    //明示的にDispose()若しくはSave()を呼ばないと、保存されない
    //コンストラクタで指定したファイルが存在しない場合は、新規に作成される
    public class Reg : IDisposable{

        readonly String _path;
        readonly Dictionary<string, string> _ar = new Dictionary<string, string>();

        //pathに指定したファイルが見つからない場合は、新規に作成される
        public Reg(String path){
            _path = path;
            if (!File.Exists(_path)){
                //ファイルが存在しない場合は、新規に作成する
                File.Create(path).Close();
                //File.Create(path);
            }
            foreach (var s in File.ReadAllLines(path, Encoding.GetEncoding(932))){
                var index = s.IndexOf('=');
                if (index < 1)
                    break;
                var key = s.Substring(0, index);
                var val = s.Substring(index + 1);
                _ar.Add(key, val);
            }
        }

        //終了処理
        public void Dispose(){
            Save();
        }

        public void Save(){

            using (var sw = new StreamWriter(_path, false, Encoding.GetEncoding(932))) {
                foreach (var s in _ar.Select(a => string.Format("{0}={1}", a.Key, a.Value))) {
                    sw.WriteLine(s);
                }
                sw.Flush();
                sw.Close();
            }
        }

        //String値を読み出す
        //指定したKeyが無効(（key==null、Key=="")の場合、例外(RegExceptionKind.InvalidKey)がスローされる
        //値が見つからなかった場合、例外(RegExceptionKind.ValueNotFound)がスローされる
        public String GetString(String key){
            if (string.IsNullOrEmpty(key)){
                throw new Exception("key = IsNullOrEmpty");
            }
            foreach (var a in _ar.Where(a => a.Key == key)){
                return a.Value;
            }
            //検索結果がヒットしなかった場合、例外がスローされる
            throw new Exception(string.Format("key={0}", key));
        }

        //String値の設定(既に値が設定されている場合は、上書きとなる)<br>
        //指定したKeyが無効(（key==null、Key=="")の場合、例外(RegExceptionKind.InvalidKey)がスローされる<br>
        //val==nullの場合は、val=""として保存される<br>
        public void SetString(String key, String val){
            if (string.IsNullOrEmpty(key)){
                throw new Exception("key=IsNullOrEmpty");
            }
            if (val == null){
                //val==nullの場合は、""を保存する
                val = "";
            }
            _ar.Remove(key);
            _ar.Add(key, val);
        }
       //int値を読み出す<br>
        //指定したKeyが無効(（key==null、Key=="")の場合、例外(RegExceptionKind.InvalidKey)がスローされる<br>
        //値が見つからなかった場合、例外(RegExceptionKind.ValueNotFound)がスローされる<br>
        //読み出した値がｉｎｔ型でなかった場合、例外(RegExceptionKind.NotNumberFormat)がされる<br>
        public int GetInt(String key){
            var str = GetString(key);

            try{
                return Convert.ToInt32(str);
            }
            catch (Exception){
                throw new Exception(string.Format("val={0}", str));
            }
        }

        //int値を設定する(既に値が設定されている場合は、上書きとなる)<br>
        //指定したKeyが無効(（key==null、Key=="")の場合、例外(RegExceptionKind.InvalidKey)がスローされる<br>
        public void SetInt(String key, int val){
            SetString(key, val.ToString());
        }
    }
}

