namespace FtpServer{

    //FTPコマンド
    internal enum FtpCmd{
        Quit,
        Noop,
        User,
        Pass,
        Cwd,
        Port,
        Eprt,
        Pasv,
        Epsv,
        Retr,
        Stor,
        Rnfr,
        Rnto,
        Abor,
        Dele,
        Rmd,
        Mkd,
        Pwd,
        Xpwd,
        List,
        Nlst,
        Type,
        Cdup,
        Syst,
        Unknown
    }
}
