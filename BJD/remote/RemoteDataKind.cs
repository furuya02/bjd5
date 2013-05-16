namespace Bjd.remote {
    public enum RemoteDataKind : byte {
        DatAuth = 0,       //S->C (接続時)認証情報(SJIS)
        DatVer = 1,        //S->C (接続時)バージョン/ログイン完了(SJIS)
        DatLocaladdress = 2, //S->C (接続時)LocalAddressの初期化
        DatOption = 3,     //S->C (接続時)Option.iniの送信
        DatLog = 4,        //S->C ログ (SJIS)
        DatTrace = 5,      //S->C トレース
        DatTool = 6,      //S->C （ToolDlg用）のデータ
        DatBrowse = 7,    //S->C （BrowseDlg用）のデータ
        CmdAuth = 8,      //C->S DAT_AUTHに対するパスワード
        CmdRestart = 9,   //C->S 「再起動」メニュー選択
        CmdOption = 10,    //C->S Option.ini送信
        CmdTrace = 11,     //C->S TraceDlg表示,非表示(BOOL)
        CmdTool = 12,      //C->S （ToolDlg用）データ要求
        CmdBrowse = 13,     //C->S （BrowseDlg用）データ要求
        Unknown = 14
    }
}