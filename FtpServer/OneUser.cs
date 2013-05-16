using System;

namespace FtpServer{

    //Datオブジェクトの各プロパティをObject形式ではない本来の型で強制するため、これを表現するクラスを定義する

    public class OneUser : IDisposable{

        public FtpAcl FtpAcl { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string HomeDir { get; private set; }


        public OneUser(FtpAcl ftpAcl, string userName, string password, string homeDir){
            FtpAcl = ftpAcl;
            UserName = userName;
            Password = password;
            //ホームディレクトリの指定は、必ず最後が\\になるようにする
            if (homeDir[homeDir.Length - 1] != '\\'){
                homeDir = homeDir + "\\";
            }
            HomeDir = homeDir;
        }

        public void Dispose(){

        }
    }
}