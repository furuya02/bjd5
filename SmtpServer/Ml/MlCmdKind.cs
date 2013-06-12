namespace SmtpServer {
    //***************************************************************
    //
    //***************************************************************
    enum MlCmdKind {
        Exit,
        Quit,
        Guide,
        Help,
        Get,
        Members,
        Member,
        Summary,
        Subject,//拡張
        Bye,
        Unsubscribe,
        Subscribe,
        Confirm,
        Password,
        Add,
        Del,
    }
}