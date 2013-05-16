namespace SmtpServer {
    enum SmtpClientResult {
        Success = 0,//成功
        ErrorCode = 1,//明確なエラーコードが返された
        Faild = 2 //原因不明の失敗
    }
}
