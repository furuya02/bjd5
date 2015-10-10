namespace SmtpServer {
    class OneMlCmd {
        public MlCmdKind CmdKind { get; private set; }
        public string ParamStr { get; private set; }
        public MlOneUser MlOneUser { get; private set; }
        public OneMlCmd(MlCmdKind cmdKind, string paramStr, MlOneUser mlOneUser) {
            paramStr = paramStr.Trim();

            CmdKind = cmdKind;
            ParamStr = paramStr;
            MlOneUser = mlOneUser;
        }
    }
}
