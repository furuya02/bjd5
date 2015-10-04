using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Bjd.sock;
using Bjd.util;

namespace Bjd.log{
    public delegate string GetMsgDelegate(int no);

    //ログ出力用のクラス<br>
    //ファイルとディスプレイの両方を統括する
    //テスト用に、Logger.create()でログ出力を処理を一切行わないインスタンスが作成される
    public class Logger{
        private readonly Kernel _kernel;
        private readonly LogLimit _logLimit;
        private readonly LogFile _logFile;
        private readonly LogView _logView;
        private readonly bool _isJp;
        private readonly String _nameTag;
        private readonly bool _useDetailsLog;
        private readonly bool _useLimitString;
        private readonly ILogger _logger;

        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();

        //コンストラクタ
        //kernelの中でCreateLogger()を定義して使用する
        public Logger(Kernel kernel, LogLimit logLimit, LogFile logFile, LogView logView, bool isJp, String nameTag,
                      bool useDetailsLog, bool useLimitString, ILogger logger){
            _kernel = kernel;
            _logLimit = logLimit;
            _logFile = logFile;
            _logView = logView;
            _isJp = isJp;
            _nameTag = nameTag;
            _useDetailsLog = useDetailsLog;
            _useLimitString = useLimitString;
            _logger = logger;
        }

        //テスト用
        public Logger(){
            _logLimit = null;
            _logFile = null;
            _logView = null;
            _isJp = true;
            _nameTag = "";
            _useDetailsLog = false;
            _useLimitString = false;
            _logger = null;
        }

        //ログ出力
        //Override可能（テストで使用）
        public void Set(LogKind logKind, SockObj sockBase, int messageNo, String detailInfomation){
            //デバッグ等でkernelが初期化されていない場合、処理なし
            if (_logFile == null && _logView == null){
                return;
            }
            //詳細ログが対象外の場合、処理なし
            if (logKind == LogKind.Detail){
                if (!_useDetailsLog){
                    return;
                }
            }
            int threadId = GetCurrentThreadId();
            //long threadId = Thread.currentThread().getId(); 
            var message = _isJp ? "定義されていません" : "Message is not defined";
            if (messageNo < 9000000){
                if (_logger != null){
                    message = _logger.GetMsg(messageNo); //デリゲートを使用した継承によるメッセージ取得
                }
            }
            else{
                //(9000000以上)共通番号の場合の処理
                switch (messageNo){
                    case 9000000:
                        message = _isJp ? "サーバ開始" : "Server started it";
                        break;
                    case 9000001:
                        message = _isJp ? "サーバ停止" : "Server stopped";
                        break;
                    case 9000002:
                        message = "_subThread() started.";
                        break;
                    case 9000003:
                        message = "_subThread() stopped.";
                        break;
                    case 9000004:
                        message = _isJp
                                      ? "同時接続数を超えたのでリクエストをキャンセルします"
                                      : "Because the number of connection exceeded it at the same time, the request was canceled.";
                        break;
                    case 9000005:
                        message = _isJp
                                      ? "受信文字列が長すぎます（不正なリクエストの可能性があるため切断しました)"
                                      : "Reception character string is too long (cut off so that there was possibility of an unjust request in it)";
                        break;
                    case 9000006:
                        message = _isJp ? "このポートは、既に他のプログラムが使用しているため使用できません" : "Cannot use this port so that other programs already use it";
                        break;
                    case 9000007:
                        message = _isJp ? "callBack関数が指定されていません[UDP]" : "It is not appointed in callback function [UDP]";
                        break;
                    case 9000008:
                        message = _isJp ? "プラグインをインストールしました" : "setup initialize plugin";
                        break;
                    //case 9000009:
                    //    message = _isJp ? "Socket.Bind()でエラーが発生しました。[TCP]" : "An error occurred in Socket.Bind() [TCP]";
                    //    break;
                    //case 9000010:
                    //    message = _isJp
                    //                  ? "Socket.Listen()でエラーが発生しました。[TCP]"
                    //                  : "An error occurred in Socket..Listen() [TCP]";
                    //    break;
                    case 9000011:
                        message = "tcpQueue().Dequeue()=null";
                        break;
                    case 9000012:
                        message = "tcpQueue().Dequeue() SocektObjState != SOCKET_OBJ_STATE.CONNECT break";
                        break;
                    case 9000013:
                        message = "tcpQueue().Dequeue()";
                        break;
                        //			case 9000014:
                        //				message = "SendBinaryFile(string fileName) socket.Send()";
                        //				break;
                        //			case 9000015:
                        //				message = "SendBinaryFile(string fileName,long rangeFrom,long rangeTo) socket.Send()";
                        //				break;
                    case 9000016:
                        message = _isJp
                                      ? "このアドレスからの接続は許可されていません(ACL)"
                                      : "Connection from this address is not admitted.(ACL)";
                        break;
                    case 9000017:
                        message = _isJp
                                      ? "このアドレスからの接続は許可されていません(ACL)"
                                      : "Connection from this address is not admitted.(ACL)";
                        break;
                    case 9000018:
                        message = _isJp ? "この利用者のアクセスは許可されていません(ACL)" : "Access of this user is not admitted (ACL)";
                        break;
                    case 9000019:
                        message = _isJp ? "アイドルタイムアウト" : "Timeout of an idle";
                        break;
                    case 9000020:
                        message = _isJp ? "送信に失敗しました" : "Transmission of a message failure";
                        break;
                    case 9000021:
                        message = _isJp ? "ThreadBase::loop()で例外が発生しました" : "An exception occurred in ThreadBase::Loop()";
                        break;
                    case 9000022:
                        message = _isJp
                                      ? "ウインドウ情報保存ファイルにIOエラーが発生しました"
                                      : "An IO error occurred in a window information save file";
                        break;
                    case 9000023:
                        message = _isJp ? "証明書の読み込みに失敗しました" : "Reading of a certificate made a blunder";
                        break;
                    case 9000024:
                        message = _isJp ? "SSLの初期化に失敗しているためサーバは起動できません" : "A server cannot start in order to fail in initialization of SSL";
                        break;
                        //case 9000025: message = isJp ? "ファイル（秘密鍵）が見つかりません" : "Private key is not found"; break;
                    case 9000026:
                        message = _isJp ? "ファイル（証明書）が見つかりません" : "A certificate is not found";
                        break;
                        //case 9000027: message = isJp ? "OpenSSLのライブラリ(ssleay32.dll,libeay32.dll)が見つかりません" : "OpenSSL library (ssleay32.dll,libeay32.dll) is not found"; break;
                    case 9000028:
                        message = _isJp ? "SSLの初期化に失敗しています" : "Initialization of SSL made a blunder";
                        break;
                    case 9000029:
                        message = _isJp ? "指定された作業ディレクトリが存在しません" : "A work directory is not found";
                        break;
                    case 9000030:
                        message = _isJp ? "起動するサーバが見つかりません" : "A starting server is not found";
                        break;
                    case 9000031:
                        message = _isJp ? "ログファイルの初期化に失敗しました" : "Failed in initialization of logfile";
                        break;
                    case 9000032:
                        message = _isJp ? "ログ保存場所" : "a save place of LogFile";
                        break;
                    case 9000033:
                        message = _isJp ? "ファイル保存時にエラーが発生しました" : "An error occurred in a File save";
                        break;
                    case 9000034:
                        message = _isJp ? "ACL指定に問題があります" : "ACL configuration failure";
                        break;
                    case 9000035:
                        message = _isJp ? "Socket()でエラーが発生しました。[TCP]" : "An error occurred in Socket() [TCP]";
                        break;
                    //case 9000036:
                    //    message = _isJp ? "Socket()でエラーが発生しました。[UDP]" : "An error occurred in Socket() [UDP]";
                    //    break;
                    case 9000037:
                        message = _isJp ? "_subThread()で例外が発生しました" : "An exception occurred in _subThread()";
                        break;
                    case 9000038:
                        message = _isJp ? "【例外】" : "[Exception]";
                        break;
                    case 9000039:
                        message = _isJp ? "【STDOUT】" : "[STDOUT]";
                        break;
                    case 9000040:
                        message = _isJp ? "拡張SMTP適用範囲の指定に問題があります" : "ESMTP range configuration failure";
                        break;
                    case 9000041:
                        message = _isJp ? "disp2()で例外が発生しました" : "An exception occurred in disp2()";
                        break;
                    case 9000042:
                        message = _isJp
                                      ? "初期化に失敗しているためサーバを開始できません"
                                      : "Can't start a server in order to fail in initialization";
                        break;
                    case 9000043:
                        message = _isJp ? "クライアント側が切断されました" : "The client side was cut off";
                        break;
                    case 9000044:
                        message = _isJp ? "サーバ側が切断されました" : "The server side was cut off";
                        break;
                    case 9000045:
                        message = _isJp
                                      ? "「オプション(O)-ログ表示(L)-基本設定-ログの保存場所」が指定されていません"
                                      : "\"log save place\" is not appointed";
                        break;
                    case 9000046:
                        message = _isJp ? "socket.send()でエラーが発生しました" : "socket.send()";
                        break;
                    case 9000047:
                        message = _isJp ? "ユーザ名が無効です" : "A user name is null and void";
                        break;
                    case 9000048:
                        message = _isJp ? "ThreadBase::Loop()で例外が発生しました" : "An exception occurred in ThreadBase::Loop()";
                        break;
                    case 9000049:
                        message = _isJp ? "【例外】" : "[Exception]";
                        break;
                    case 9000050:
                        message = _isJp ? "ファイルにアクセスできませんでした" : "Can't open a file";
                        break;
                    case 9000051:
                        message = _isJp ? "インスタンスの生成に失敗しました" : "Can't create instance";
                        break;
                    case 9000052:
                        message = _isJp ? "名前解決に失敗しました" : "Non-existent domain";
                        break;
                    case 9000053:
                        message = _isJp ? "【例外】SockObj.Resolve()" : "[Exception] SockObj.Resolve()";
                        break;
                    case 9000054:
                        message = _isJp
                                      ? "Apache Killerによる攻撃の可能性があります"
                                      : "There is possibility of attack by Apache Killer in it";
                        break;
                    case 9000055:
                        message = _isJp ? "【自動拒否】「ACL」の禁止する利用者（アドレス）に追加しました" : "Add it to a deny list automatically";
                        break;
                    case 9000056:
                        message = _isJp
                                      ? "不正アクセスを検出しましたが、ACL「拒否」リストは追加されませんでした"
                                      : "I detected possibility of Attack, but the ACL [Deny] list was not added";
                        break;
                    case 9000057:
                        message = _isJp ? "【例外】" : "[Exception]";
                        break;
                    case 9000058:
                        message = _isJp ? "メールの送信に失敗しました" : "Failed in the transmission of a message of an email";
                        break;
                    case 9000059:
                        message = _isJp ? "メールの保存に失敗しました" : "Failed in a save of an email";
                        break;
                    case 9000060:
                        message = _isJp ? "【例外】" : "[Exception]";
                        break;
                    case 9000061:
                        message = _isJp ? "【例外】" : "[Exception]";
                        break;
                    //case 9000061:
                        //	message = isJp ? "ファイルの作成に失敗しました" : "Failed in making of a file";
                        //	break;
                }
            }
            var remoteHostname = (sockBase == null) ? "-" : sockBase.RemoteHostname;
            var oneLog = new OneLog(DateTime.Now, logKind, _nameTag, threadId, remoteHostname, messageNo, message,
                                       detailInfomation);

            // 表示制限にヒットするかどうかの確認
            var isDisplay = true;
            if (!oneLog.IsSecure()){
                //セキュリティログは表示制限の対象外
                if (_logLimit != null){
                    isDisplay = _logLimit.IsDisplay(oneLog.ToString());
                }
            }
            if (_logView != null && isDisplay){
                //isDisplayの結果に従う
                _logView.Append(oneLog);
            }

            //Ver5.8.8
            //LogViewの中で実行していたリモートクライアントへの送信をこちらに移動する
            //サービス起動の際に、ListViewがnullで、処理されないから
            //リモートクライアントへのログ送信
            if (_kernel != null && _kernel.RemoteConnect != null && _kernel.ListServer != null) {
                //クライアントから接続されている場合
                var sv = _kernel.ListServer.Get("Remote");
                if (sv != null)
                    sv.Append(oneLog);
            }


            if (_logFile != null){
                if (_useLimitString){
                    //表示制限が有効な場合
                    if (isDisplay){
                        //isDisplayの結果に従う
                        _logFile.Append(oneLog);
                    }
                }
                else{
                    //表示制限が無効な場合は、すべて保存される
                    _logFile.Append(oneLog);
                }
            }
        }


        //Ver5.3.2
        public void Exception(Exception ex, SockObj sockObj, int messageNo) {
            Set(LogKind.Error, sockObj, messageNo, ex.Message);
            string[] tmp = ex.StackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in tmp) {
                var lines = new List<string>();
                var l = Util.SwapStr("\r\n", "", s);
                while (true) {
                    if (l.Length < 80) {
                        lines.Add(l);
                        break;
                    }
                    lines.Add(l.Substring(0, 80));
                    l = l.Substring(80);
                }
                for (int i = 0; i < lines.Count; i++) {
                    if (i == 0) {
                        Set(LogKind.Error, sockObj, messageNo, lines[i]);
                    } else {
                        Set(LogKind.Error, sockObj, messageNo, "   -" + lines[i]);
                    }
                }
            }
        }
    }
}


