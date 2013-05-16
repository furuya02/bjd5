using System;
using System.IO;

namespace FtpServer {


    public class OneMount : IDisposable{

        public string FromFolder { get; private set; }
        public string ToFolder { get; private set; }

        public OneMount(string fromFolder, string toFolder){
            FromFolder = fromFolder;
            ToFolder = toFolder;
        }



        public void Dispose(){

        }

        public bool IsToFolder(string dir){
            if ((ToFolder + "\\") == dir){
                return true;
            }
            return false;
        }

        public string Name{
            get { return Path.GetFileName(FromFolder); }
        }
        
        public DirectoryInfo Info {
            get {
                return new DirectoryInfo(FromFolder);
            }
        }
    }
}
