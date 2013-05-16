namespace Bjd {
    public enum RunMode {
        Normal = 0,//通常起動  (ウインドあり)
        NormalRegist = 1,//通常起動（サ-ビス登録済み）(ウインドあり)
        Remote = 2,//リモート（ウインドあり）
        Service = 3//サービス起動　(ウインドなし)
    }
}
