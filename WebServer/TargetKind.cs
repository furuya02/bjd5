namespace WebServer {
    enum TargetKind {
        Non = 0,
        Cgi = 1,
        Ssi = 2,
        Dir = 3,
        File = 4,
        Move = 5//指定されたファイル名はディレクトリの場違いの場合
    }
}
