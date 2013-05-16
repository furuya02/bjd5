namespace FtpServer{

    //FTPの転送モード
    //Windows上でのFTPサーバは、改行コードが\r\nあるため、アスキーモードもバイナリ-モードも操作は同じになる
    //従って、単純に表示用の変数でしかない
    public enum FtpType{
        Ascii,
        Binary
    }
}