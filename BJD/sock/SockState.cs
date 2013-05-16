namespace Bjd.sock{
    //ソケットオブジェクト（SockObj）の状態
    public enum SockState{
        //TODO 移植完了後　リファクタリングで大文字に変更
        //初期状態
        Idle,
        //接続完了
        Connect,
        //bind完了
        Bind,
        //エラー（切断）状態　使用できない
        Error,
    }

}
