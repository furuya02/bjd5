
namespace TftpServer {
    partial class Server {
        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                case 1: return Kernel.IsJp()?"オペコードの取得に失敗しました":"failed in the acquisition of an operation cord";
                case 2: return Kernel.IsJp()?"ファイル名の取得に失敗しました":"failed in the acquisition of a file name";
                case 3: return Kernel.IsJp()?"モード文字列の取得に失敗しました":"failed in the acquisition of a mode";
                case 4: return Kernel.IsJp()?"モード文字列の解釈に敗しました":"failed in interpretation of a mode";
                case 5: return Kernel.IsJp()?"作業フォルダが指定されていません":"A work folder is not appointed";
                case 6: return Kernel.IsJp()?"指定された作業フォルダが存在しません":"There is not an appointed work folder";
                case 7: return Kernel.IsJp()?"タイムアウトしました":"Timeout";
                case 8: return Kernel.IsJp()?"アップロード(WRQ)":"UPLOAD (WRQ)";
                case 9: return Kernel.IsJp()?"ダウンロード(RRQ)":"DOWNLOAD (RRQ)";
                case 10: return Kernel.IsJp()?"「読込み」が許可されていない":"Receive of a message prohibition";
                case 11: return Kernel.IsJp()?"「書込み」が許可されていない":"Transmission of a message prohibition";
                case 12: return Kernel.IsJp()?"「上書き」が許可されていない":"There is already a file";
                case 13: return Kernel.IsJp()?"ファイルが見つからない":"A file is not found";
                case 14: return Kernel.IsJp()?"ACK番号の不整合が発生しました":"Unmatch ACK number";
            }
            return "unknown";
        }
    }
}
