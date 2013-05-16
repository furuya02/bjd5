using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Bjd;
using Bjd.util;

namespace WebServer {
    class WebDavDb : IDisposable {
        readonly List<OneWebDavDb> _ar = new List<OneWebDavDb>();
        readonly string _fileName;
        public string NameTag { get; private set; }
        public WebDavDb(Kernel kernel, string nameTag) {
            NameTag = nameTag;
            _fileName = string.Format("{0}\\webdav.{1}.db", kernel.ProgDir(), Util.SwapChar(':', '-', nameTag));
            //ファイルからの読み込み
            if (File.Exists(_fileName)) {
                using (var sr = new StreamReader(_fileName, Encoding.GetEncoding("Shift_JIS"))) {
                    while (true) {
                        string str = sr.ReadLine();
                        if (str == null)
                            break;
                        var oneWebDavDb = new OneWebDavDb(Inet.TrimCrlf(str));
                        if (oneWebDavDb.Uri != "") {
                            _ar.Add(oneWebDavDb);
                        }
                    }
                    sr.Close();
                }
            }
        }
        public void Dispose() {
            //ファイルへの保存
            using (var fs = new FileStream(_fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
                using (var sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS"))) {
                    foreach (var o in _ar) {
                        sw.WriteLine(o.ToString());
                    }
                    sw.Flush();
                    sw.Close();
                }
                fs.Close();
            }
        }
        public void Set(string uri, string nameSpace, string name, string value) {
            lock (this){
                //Removeと同じだが排他制御のため、助長だがここにも同じ記述が必要になる
                foreach (var o in _ar.Where(o => o.Uri == uri && o.NameSpace == nameSpace && o.Name == name)){
                    _ar.Remove(o);
                    break;
                }
                _ar.Add(new OneWebDavDb(uri, nameSpace, name, value));
            }
        }
        public void Remove(string uri, string nameSpace, string name) {
            lock (this) {
                foreach (var o in _ar.Where(o => o.Uri == uri && o.NameSpace == nameSpace && o.Name == name)){
                    _ar.Remove(o);
                    break;
                }
            }
        }
        public void Remove(string uri) {
            lock (this) {
                for (int i = _ar.Count - 1; i >= 0; i--) {
                    if (_ar[i].Uri == uri) {
                        _ar.RemoveAt(i);
                    }
                }
            }
        }
        public List<OneWebDavDb> Get(string uri) {
            lock (this){
                return _ar.Where(oneWebDavDb => oneWebDavDb.Uri == uri).ToList();
            }
        }
    }
}
