using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bjd.util {
    public enum LangKind {
        Jp,
        En
    }
    public class Lang {
        List<OneLang> ar = new List<OneLang>();
        private const string FileName = "BJD.Lang.txt";
        private readonly string _category;

        //Ver6.1.8　定義ファイルがない場合
        private bool _fileExsists = false;

        
        //private readonly LangKind _langKind;
        public LangKind LangKind { get; private set; }
        
        public Lang(LangKind langKind,string category) {
            
            LangKind = langKind;
            _category = category;
            var index = _category.IndexOf('-');
            if (index != -1) {
                //OptionResource-a.com  =>  OptionResource
                _category = _category.Substring(0, index);
            }

            var path = string.Format("{0}\\{1}",Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),FileName);

            //Ver6.1.8
            _fileExsists = File.Exists(path);
            if(!_fileExsists){
                return;
            }
            
            var lines = File.ReadAllLines(path, Encoding.GetEncoding("Shift-JIS"));
            foreach (var line in lines) {
                if (line.Length > 0 && line[0] == '#') {
                    continue;//コメント
                }
                var tmp = line.Split('\t');
                if (tmp.Length == 3) {
                    var t = tmp[0].Split('_');
                    if (t.Length == 2) {
                        if (t[0] == _category) {
                            var key = t[1];
                            var value = tmp[langKind == LangKind.Jp ? 1 : 2];
                            ar.Add(new OneLang(key,value));
                        }
                    }
                }
            }
        }
        public String Value(int key) {
            return Value(string.Format("{0:D4}", key));
        }

        public String Value(String key) {
            //Ver6.1.8 とりあえず例外はやめる
            //キーが存在しない場合、例外として処理する
            //try {
            //    return ar.Find(n => n.Key == key).Value;
            //}catch (Exception) {
            //    throw new Exception(string.Format("Langクラス例外\r\n(BJD.Lang.txt に文字列が定義されていません)\r\n Kind={0} Category={1} Key={2}\r\n\r\n",LangKind,_category, key));            
            //}
            
            if (!_fileExsists){
                return "File not found(BJD.Land.txt)";
            }

            try {
                return ar.Find(n => n.Key == key).Value;
            }catch (Exception) {
                return string.Format("未定義 Kind={0} Category={1} Key={2}",LangKind,_category, key);            
            }
        }
    }

    class OneLang {
        public String Key { get; private set; }
        public String Value{ get; private set; }
        public OneLang(string key, string value) {
            Key = key;
            Value = value;
        }
    }
}


