namespace SmtpServer {
    //投稿者への定型メール
    enum MlDocKind {
        Deny = 0,//無効な投稿者
        Guide = 1,//ガイド
        Welcome = 2,//登録完了
        Confirm = 3,//Confirm
        Append = 4, //管理者宛の登録依頼
        Help = 5,//ヘルプ(ユーザ用)
        Admin = 6,//ヘルプ(管理者用)
    }
}
