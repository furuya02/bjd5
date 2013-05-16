using System;
using System.IO;
using System.Text;

namespace Bjd.log{
    //生成時に１つのファイルをオープンしてset()で１行ずつ格納するクラス
    public class OneLogFile : IDisposable{
        private FileStream _fs;
        private StreamWriter _sw;
        private readonly string _fileName;

        public OneLogFile(String fileName){
            _fileName = fileName;
            _fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            _sw = new StreamWriter(_fs, Encoding.GetEncoding(932));
        }

        public void Dispose(){
            _sw.Flush();
            _sw.Close();
            _sw.Dispose();
            _sw = null;
            _fs.Close();
            _fs.Dispose();
            _fs = null;
        }

        public void Set(String str){
            _fs.Seek(0, SeekOrigin.End);
            _sw.WriteLine(str);
            _sw.Flush();
        }

        //public String GetPath(){
        //    return _fileName;
        //}
    }
}

