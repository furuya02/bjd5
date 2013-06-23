using System;
using System.Text;
using System.Threading;
using System.IO;

using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using Bjd.server;

namespace FtpServer{


    public class Server : OneServer{

        private readonly String _bannerMessage;
        private readonly ListUser _listUser;
        private readonly ListMount _listMount;

        public Server(Kernel kernel, Conf conf, OneBind oneBind) : base(kernel, conf, oneBind){

            _bannerMessage = kernel.ChangeTag((String) Conf.Get("bannerMessage"));
            //ユーザ情報
            _listUser = new ListUser((Dat) Conf.Get("user"));
            //仮想フォルダ
            _listMount = new ListMount((Dat) Conf.Get("mountList"));


        }

        protected override void OnStopServer(){

        }


        protected override bool OnStartServer(){
            return true;
        }


        protected override void OnSubThread(SockObj sockObj){
            //セッションごとの情報
            var session = new Session((SockTcp) sockObj);

            //このコネクションの間、１つづつインクメントしながら使用される
            //本来は、切断したポート番号は再利用可能なので、インクリメントの必要は無いが、
            //短時間で再利用しようとするとエラーが発生する場合があるので、これを避ける目的でインクリメントして使用している

            //グリーティングメッセージの送信
            session.StringSend(string.Format("220 {0}", _bannerMessage));

            //コネクションを継続するかどうかのフラグ
            var result = true;

            while (IsLife() && result){
                //このループは最初にクライアントからのコマンドを１行受信し、最後に、
                //sockCtrl.LineSend(resStr)でレスポンス処理を行う
                //continueを指定した場合は、レスポンスを返さずに次のコマンド受信に入る（例外処理用）
                //breakを指定した場合は、コネクションの終了を意味する（QUIT ABORT 及びエラーの場合）

                Thread.Sleep(0);

                var cmd = recvCmd(session.SockCtrl);
                if (cmd == null){
                    //切断されている
                    break;
                }

                if (cmd.Str == ""){
                    session.StringSend("500 Invalid command: try being more creative.");
                    //受信待機中
                    //Thread.Sleep(100);
                    continue;
                }

                //コマンド文字列の解釈
                //var ftpCmd = (FtpCmd) Enum.Parse(typeof (FtpCmd), cmd.CmdStr);
                var ftpCmd = FtpCmd.Unknown;
                foreach (FtpCmd n in Enum.GetValues(typeof(FtpCmd))) {
                    if (n.ToString().ToUpper() != cmd.CmdStr.ToUpper())
                        continue;
                    ftpCmd = n;
                    break;
                }
                
                
                //FtpCmd ftpCmd = FtpCmd.parse(cmd.CmdStr);
                var param = cmd.ParamStr;

                //SYSTコマンドが有効かどうかの判断
                if (ftpCmd == FtpCmd.Syst){
                    if (!(bool) Conf.Get("useSyst")){
                        ftpCmd = FtpCmd.Unknown;
                    }
                }
                //コマンドが無効な場合の処理
                if (ftpCmd == FtpCmd.Unknown){
                    //session.StringSend("502 Command not implemented.");
                    session.StringSend("500 Command not understood.");
                }

                //QUITはいつでも受け付ける
                if (ftpCmd == FtpCmd.Quit){
                    session.StringSend("221 Goodbye.");
                    break;
                }

                if (ftpCmd == FtpCmd.Abor){
                    session.StringSend("250 ABOR command successful.");
                    break;
                }

                //			//これは、ログイン中しか受け付けないコマンドかも？
                //			//RNFRで指定されたパスの無効化
                //			if (ftpCmd != FtpCmd.Rnfr) {
                //				session.setRnfrName("");
                //			}

                // コマンド組替え
                if (ftpCmd == FtpCmd.Cdup){
                    param = "..";
                    ftpCmd = FtpCmd.Cwd;
                }

                //不正アクセス対処 パラメータに極端に長い文字列を送り込まれた場合
                if (param.Length > 128){
                    Logger.Set(LogKind.Secure, session.SockCtrl, 1, string.Format("{0} Length={1}", ftpCmd, param.Length));
                    break;
                }

                //デフォルトのレスポンス文字列
                //処理がすべて通過してしまった場合、この文字列が返される
                //String resStr2 = string.Format("451 {0} error", ftpCmd);

                // ログイン前の処理
                if (session.CurrentDir == null){
                    //ftpCmd == FTP_CMD.PASS
                    //未実装
                    //PASSの前にUSERコマンドを必要とする
                    //sockCtrl.LineSend("503 Login with USER first.");

                    if (ftpCmd == FtpCmd.User){
                        if (param == ""){
                            session.StringSend(string.Format("500 {0}: command requires a parameter.", ftpCmd.ToString().ToUpper()));
                            continue;
                        }
                        result = JobUser(session, param);
                    } else if (ftpCmd == FtpCmd.Pass){
                        result = JobPass(session, param);
                    } else{
                        //USER、PASS以外はエラーを返す
                        session.StringSend("530 Please login with USER and PASS.");
                    }
                    // ログイン後の処理
                } else{
                    // パラメータの確認(パラメータが無い場合はエラーを返す)
                    if (param == ""){
                        if (ftpCmd == FtpCmd.Cwd || ftpCmd == FtpCmd.Type || ftpCmd == FtpCmd.Mkd || ftpCmd == FtpCmd.Rmd || ftpCmd == FtpCmd.Dele || ftpCmd == FtpCmd.Port || ftpCmd == FtpCmd.Rnfr || ftpCmd == FtpCmd.Rnto || ftpCmd == FtpCmd.Stor || ftpCmd == FtpCmd.Retr){
                            //session.StringSend("500 command not understood:");
                            session.StringSend(string.Format("500 {0}: command requires a parameter.", ftpCmd.ToString().ToUpper()));
                            continue;
                        }
                    }

                    // データコネクションが無いとエラーとなるコマンド
                    if (ftpCmd == FtpCmd.Nlst || ftpCmd == FtpCmd.List || ftpCmd == FtpCmd.Stor || ftpCmd == FtpCmd.Retr){
                        if (session.SockData == null || session.SockData.SockState !=Bjd.sock.SockState.Connect){
                            session.StringSend("226 data connection close.");
                            continue;
                        }
                    }
                    // ユーザのアクセス権にエラーとなるコマンド
                    if (session.OneUser != null){
                        if (session.OneUser.FtpAcl == FtpAcl.Down){
                            if (ftpCmd == FtpCmd.Stor || ftpCmd == FtpCmd.Dele || ftpCmd == FtpCmd.Rnfr || ftpCmd == FtpCmd.Rnto || ftpCmd == FtpCmd.Rmd || ftpCmd == FtpCmd.Mkd){
                                session.StringSend("550 Permission denied.");
                                continue;
                            }
                        } else if (session.OneUser.FtpAcl == FtpAcl.Up){
                            if (ftpCmd == FtpCmd.Retr || ftpCmd == FtpCmd.Dele || ftpCmd == FtpCmd.Rnfr || ftpCmd == FtpCmd.Rnto || ftpCmd == FtpCmd.Rmd || ftpCmd == FtpCmd.Mkd){
                                session.StringSend("550 Permission denied.");
                                continue;
                            }
                        }
                    }

                    // ログイン中(認証完了）時は、USER、PASS を受け付けない
                    if (ftpCmd == FtpCmd.User || ftpCmd == FtpCmd.Pass){
                        session.StringSend("530 Already logged in.");
                        continue;
                    }

                    if (ftpCmd == FtpCmd.Noop){
                        session.StringSend("200 NOOP command successful.");
                    } else if (ftpCmd == FtpCmd.Pwd || ftpCmd == FtpCmd.Xpwd){
                        session.StringSend(string.Format("257 \"{0}\" is current directory.", session.CurrentDir.GetPwd()));
                    } else if (ftpCmd == FtpCmd.Cwd){
                        result = JobCwd(session, param);
                    } else if (ftpCmd == FtpCmd.Syst){
                        var os = Environment.OSVersion;
                        session.StringSend(string.Format("215 {0}", os.VersionString));
                    } else if (ftpCmd == FtpCmd.Type){
                        result = JobType(session, param);
                    } else if (ftpCmd == FtpCmd.Mkd || ftpCmd == FtpCmd.Rmd || ftpCmd == FtpCmd.Dele){
                        result = JobDir(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Nlst || ftpCmd == FtpCmd.List){
                        result = JobNlist(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Port || ftpCmd == FtpCmd.Eprt){
                        result = JobPort(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Pasv || ftpCmd == FtpCmd.Epsv){
                        result = JobPasv(session, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Rnfr){
                        result = jobRnfr(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Rnto){
                        result = JobRnto(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Stor){
                        result = JobStor(session, param, ftpCmd);
                    } else if (ftpCmd == FtpCmd.Retr){
                        result = JobRetr(session, param);
                    }
                }
            }
            //ログインしている場合は、ログアウトのログを出力する
            if (session.CurrentDir != null){
                //logout
                Logger.Set(LogKind.Normal, session.SockCtrl, 13, string.Format("{0}", session.OneUser.UserName));
            }
            session.SockCtrl.Close();
            if (session.SockData != null){
                session.SockData.Close();
            }
        }

        private static bool JobUser(Session session, String userName){

            //送信されたユーザ名を記憶する
            //ユーザが存在するかどうかは、PASSコマンドの時点で評価される
            session.UserName = userName;

            //ユーザ名の有効・無効に関係なくパスワードの入力を促す
            session.StringSend(string.Format("331 Password required for {0}.", userName));
            return true;

        }

        private bool JobPass(Session session, String password){

            //まだUSERコマンドが到着していない場合
            if (session.UserName == null){
                session.StringSend("503 Login with USER first.");
                return true;
            }

            //ユーザ情報検索
            session.OneUser = _listUser.Get(session.UserName);

            if (session.OneUser == null){
                //無効なユーザの場合
                Logger.Set(LogKind.Secure, session.SockCtrl, 14, string.Format("USER:{0} PASS:{1}", session.UserName, password));
            } else{
                //パスワード確認
                bool success = false;
                // *の場合、Anonymous接続として処理する
                if (session.OneUser.Password == "*"){
                    //oneUser.UserName = string.Format("{0}(ANONYMOUS)",oneUser.UserName);
                    Logger.Set(LogKind.Normal, session.SockCtrl, 5, string.Format("{0}(ANONYMOUS) {1}", session.OneUser.UserName, password));
                    success = true;
                } else if (session.OneUser.Password == password){
                    Logger.Set(LogKind.Secure, session.SockCtrl, 6, string.Format("{0}", session.OneUser.UserName));
                    success = true;
                }

                if (success){
                    //以下、パスワード認証に成功した場合の処理
                    //ホームディレクトリの存在確認
                    //サーバ起動（運営）中にディレクトリが削除されている可能性があるので、この時点で確認する
                    if (Util.Exists(session.OneUser.HomeDir) != ExistsKind.Dir){
                        //ホームディレクトリが存在しません（処理が継続できないため切断しました
                        Logger.Set(LogKind.Error, session.SockCtrl, 2, string.Format("userName={0} hoemDir={1}", session.OneUser.UserName, session.OneUser.HomeDir));
                        return false;
                    }

                    //ログイン成功 （カレントディレクトリは、ホームディレクトリで初期化される）
                    session.CurrentDir = new CurrentDir(session.OneUser.HomeDir, _listMount);

                    session.StringSend(string.Format("230 User {0} logged in.", session.UserName));
                    return true;
                }
                //以下認証失敗処理
                Logger.Set(LogKind.Secure, session.SockCtrl, 15, string.Format("USER:{0} PASS:{1}", session.UserName, password));
            }
            var reservationTime = (int) Conf.Get("reservationTime");

            //ブルートフォース防止のためのウエイト(5秒)
            for (int i = 0; i < reservationTime/100 && IsLife(); i++){
                Thread.Sleep(100);
            }
            //認証に失敗した場合の処理
            session.StringSend("530 Login incorrect.");
            return true;

        }

        private bool JobType(Session session, String param){
            String resStr;
            switch (param.ToUpper()[0]){
                case 'A':
                    session.FtpType = FtpType.Ascii;
                    resStr = "200 Type set 'A'";
                    break;
                case 'I':
                    session.FtpType = FtpType.Binary;
                    resStr = "200 Type set 'I'";
                    break;
                default:
                    resStr = "500 command not understood.";
                    break;
            }
            session.StringSend(resStr);
            return true;
        }

        private static bool JobCwd(Session session, String param){
            if (session.CurrentDir.Cwd(param)){
                session.StringSend("250 CWD command successful.");
            } else{
                session.StringSend(string.Format("550 {0}: No such file or directory.", param));
            }
            return true;
        }

        private bool JobDir(Session session, String param, FtpCmd ftpCmd){
            bool isDir = !(ftpCmd == FtpCmd.Dele);
            int retCode = -1;
            //パラメータから新しいパス名を生成する
            var path = session.CurrentDir.CreatePath(null, param, isDir);
            if (path == null){
                //TODO エラーログ取得力が必要
            } else{
                if (ftpCmd == FtpCmd.Mkd){
                    //ディレクトリは無いか?
                    if (!Directory.Exists(path)) {//ディレクトリは無いか?
                        Directory.CreateDirectory(path);
                        retCode = 257;
                    }
                } else if (ftpCmd == FtpCmd.Rmd) {
                    if (Directory.Exists(path)) {//ディレクトリは有るか?
                        try{
                            Directory.Delete(path);
                            retCode = 250;
                        } catch (Exception) {
                    
                        }
                    }
                } else if (ftpCmd == FtpCmd.Dele) {
                    if (File.Exists(path)) {//ファイルは有るか?
                        File.Delete(path);
                        retCode = 250;
                    }
                }

                if (retCode != -1){
                    //成功
                    Logger.Set(LogKind.Normal, session.SockCtrl, 7, string.Format("User:{0} Cmd:{1} Path:{2}", session.OneUser.UserName, ftpCmd, path));
                    session.StringSend(string.Format("{0} {1} command successful.", retCode, ftpCmd));
                    return true;
                }
                //失敗
                //コマンド処理でエラーが発生しました
                Logger.Set(LogKind.Error, session.SockCtrl, 3, string.Format("User:{0} Cmd:{1} Path:{2}", session.OneUser.UserName, ftpCmd, path));
            }
            session.StringSend(string.Format("451 {0} error.", ftpCmd));
            return true;
        }

        private bool JobNlist(Session session, String param, FtpCmd ftpCmd){
            // 短縮リストかどうか
            var wideMode = (ftpCmd == FtpCmd.List);
            var mask = "*.*";

            //パラメータが指定されている場合、マスクを取得する
            if (param != ""){
                foreach (var p in param.Split(' ')){
                    if (p == ""){
                        continue;
                    }
                    if (p.ToUpper().IndexOf("-L") == 0){
                        wideMode = true;
                    }else if(p.ToUpper().IndexOf("-A") == 0) {
                        wideMode = true;
                    } else{
                        //ワイルドカード指定
                        if (p.IndexOf('*') != -1 || p.IndexOf('?') != -1){
                            mask = param;
                        } else{
                            //フォルダ指定
                            //Ver5.9.0
                            try {
                                var existsKind = Util.Exists(session.CurrentDir.CreatePath(null, param, false));
                                switch (existsKind) {
                                    case ExistsKind.Dir:
                                        mask = param + "\\*.*";
                                        break;
                                    case ExistsKind.File:
                                        mask = param;
                                        break;
                                    default:
                                        session.StringSend(string.Format("500 {0}: command requires a parameter.", param));
                                        session.SockData = null;
                                        return true;
                                }
                            } catch (Exception ex) {
                                Logger.Set(LogKind.Error, session.SockCtrl, 18,String.Format("param={0} Exception.message={1}",param,ex.Message));
                                session.StringSend(string.Format("500 {0}: command requires a parameter.", param));
                                session.SockData = null;
                                return true;
                            }
                        }
                    }
                }
            }
            session.StringSend(string.Format("150 Opening {0} mode data connection for ls.", session.FtpType.ToString().ToUpper()));
            //ファイル一覧取得
            foreach (var s in session.CurrentDir.List(mask, wideMode)){
                session.SockData.StringSend(s, "Shift-Jis");
            }
            session.StringSend("226 Transfer complete.");

            session.SockData.Close();
            session.SockData = null;
            return true;
        }

        private bool JobPort(Session session, String param, FtpCmd ftpCmd){
            String resStr = "500 command not understood:";

            Ip ip = null;
            int port = 0;

            if (ftpCmd == FtpCmd.Eprt){
                var tmpBuf = param.Split(new[]{'|'},StringSplitOptions.RemoveEmptyEntries);
                if (tmpBuf.Length == 3){
                    port = Convert.ToInt32(tmpBuf[2]);
                    try{
                        ip = new Ip(tmpBuf[1]);
                    } catch (ValidObjException){
                        ip = null;
                    }
                }
                if (ip == null){
                    resStr = "501 Illegal EPRT command.";
                }
            } else{
                var tmpBuf = param.Split(',');
                if (tmpBuf.Length == 6){
                    try{
                        ip = new Ip(tmpBuf[0] + "." + tmpBuf[1] + "." + tmpBuf[2] + "." + tmpBuf[3]);
                    } catch (ValidObjException ){
                        ip = null;
                    }
                    port = Convert.ToInt32(tmpBuf[4]) * 256 + Convert.ToInt32(tmpBuf[5]);
                }
                if (ip == null){
                    resStr = "501 Illegal PORT command.";
                }
            }
            if (ip != null){

                Thread.Sleep(10);
                var sockData = Inet.Connect(Kernel,ip, port, Timeout, null);
                if (sockData != null){
                    resStr = string.Format("200 {0} command successful.", ftpCmd.ToString().ToUpper());
                }
                session.SockData = sockData;
            }
            session.StringSend(resStr);
            return true;

        }

        private bool JobPasv(Session session, FtpCmd ftpCmd){
            var port = session.Port;
            var ip = session.SockCtrl.LocalIp;
            // データストリームのソケットの作成
            for (int i = 0; i < 100; i++){
                port++;
                if (port >= 9999){
                    port = 2000;
                }
                //バインド可能かどうかの確認
                if (SockServer.IsAvailable(Kernel,ip, port)){
                    //成功
                    if (ftpCmd == FtpCmd.Epsv){
                        //Java fix Ver5.8.3
                        //session.StringSend(string.Format("229 Entering Extended Passive Mode. (|||{0}|)", port));
                        session.StringSend(string.Format("229 Entering Extended Passive Mode (|||{0}|)", port));
                    } else {
                        var ipStr = ip.ToString();
                        //Java fix Ver5.8.3
                        //session.StringSend(string.Format("227 Entering Passive Mode. ({0},{1},{2})", ipStr.Replace('.',','), port/256, port%256));
                        session.StringSend(string.Format("227 Entering Passive Mode ({0},{1},{2})", ipStr.Replace('.', ','), port / 256, port % 256));
                    }
                    //指定したアドレス・ポートで待ち受ける
                    var sockData = SockServer.CreateConnection(Kernel,ip, port, null, this);
                    if (sockData == null){
                        //接続失敗
                        return false;
                    }
                    if (sockData.SockState != Bjd.sock.SockState.Error){
                        //セッション情報の保存
                        session.Port = port;
                        session.SockData = sockData;
                        return true;
                    }
                }
            }
            session.StringSend("500 command not understood:");
            return true;
        }

        private bool JobRnto(Session session, String param, FtpCmd ftpCmd){
            if (session.RnfrName != ""){
                var path = session.CurrentDir.CreatePath(null, param, false);


                var existsKind = Util.Exists(path);
                if (existsKind == ExistsKind.Dir){
                    session.StringSend("550 rename: Is a derectory name.");
                    return true;
                }
                if (existsKind == ExistsKind.File){
                    File.Delete(path);
                }
                if (Directory.Exists(session.RnfrName)) {//変更の対象がディレクトリである場合
                    Directory.Move(session.RnfrName, path);
                } else {//変更の対象がファイルである場合
                    File.Move(session.RnfrName, path);
                }
                Logger.Set(LogKind.Normal, session.SockCtrl, 8, string.Format("{0} {1} -> {2}", session.OneUser.UserName, session.RnfrName, path));
                session.StringSend("250 RNTO command successful.");
                return true;
            }
            session.StringSend(string.Format("451 {0} error.", ftpCmd));
            return true;
        }

        private bool jobRnfr(Session session, String param, FtpCmd ftpCmd){
            String path = session.CurrentDir.CreatePath(null, param, false);
            if (Util.Exists(path) != ExistsKind.None){
                session.RnfrName = path;
                session.StringSend("350 File exists, ready for destination name.");
                return true;
            }
            session.StringSend(string.Format("451 {0} error.", ftpCmd));
            return true;
        }

        private bool JobStor(Session session, String param, FtpCmd ftpCmd){
            String path = session.CurrentDir.CreatePath(null, param, false);
            ExistsKind exists = Util.Exists(path);
            if (exists != ExistsKind.Dir){
                //File file = new File(path);
                if (exists == ExistsKind.File){
                    // アップロードユーザは、既存のファイルを上書きできない
                    if (session.OneUser.FtpAcl == FtpAcl.Up && File.Exists(path)){
                        session.StringSend("550 Permission denied.");
                        return true;
                    }
                }
                //String str = string.Format("150 Opening {0} mode data connection for {1}.", session.getFtpType(), param);
                session.SockCtrl.StringSend(string.Format("150 Opening {0} mode data connection for {1}.", session.FtpType.ToString().ToUpper(), param),"shift-jis");

                //Up start
                Logger.Set(LogKind.Normal, session.SockCtrl, 9, string.Format("{0} {1}", session.OneUser.UserName, param));

                try{
                    int size = RecvBinary(session.SockData, path);
                    session.StringSend("226 Transfer complete.");
                    //Up end
                    Logger.Set(LogKind.Normal, session.SockCtrl, 10, string.Format("{0} {1} {2}bytes", session.OneUser.UserName, param, size));
                } catch (IOException){
                    session.StringSend("426 Transfer abort.");
                    //Up end
                    Logger.Set(LogKind.Error, session.SockCtrl, 17, string.Format("{0} {1}", session.OneUser.UserName, param));
                }

                session.SockData.Close();
                session.SockData = null;

                return true;
            }
            session.StringSend(string.Format("451 {0} error.", ftpCmd));
            return true;
        }

        private bool JobRetr(Session session, String param){
            var path = session.CurrentDir.CreatePath(null, param, false);
            if (Util.Exists(path) == ExistsKind.File){

                var dirName = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);
                var di = new DirectoryInfo(dirName);
                var files = di.GetFiles(fileName);

                if (files.Length == 1){
                    String str = string.Format("150 Opening {0} mode data connection for {1} ({2} bytes).", session.FtpType.ToString().ToUpper(), param, files[0].Length);
                    session.StringSend(str); //Shift-jisである必要がある？

                    //DOWN start
                    Logger.Set(LogKind.Normal, session.SockCtrl, 11, string.Format("{0} {1}", session.OneUser.UserName, param));
                    try{
                        int size = SendBinary(session.SockData, path);
                        session.StringSend("226 Transfer complete.");
                        //DOWN end
                        Logger.Set(LogKind.Normal, session.SockCtrl, 12, string.Format("{0} {1} {2}bytes", session.OneUser.UserName, param, size));
                    } catch (IOException){
                        session.StringSend("426 Transfer abort.");
                        //DOWN end
                        Logger.Set(LogKind.Error, session.SockCtrl, 16, string.Format("{0} {1}", session.OneUser.UserName, param));
                    }
                    session.SockData.Close();
                    session.SockData = null;

                    return true;
                }
            }
            session.StringSend(string.Format("550 {0}: No such file or directory.", param));
            return true;
        }

        //ファイル受信（バイナリ）
        private int RecvBinary(SockTcp sockTcp, String fileName){
            var sb = new StringBuilder();
            sb.Append(string.Format("RecvBinary({0}) ", fileName));

            var fs = new FileStream(fileName, FileMode.Create);
            var bw = new BinaryWriter(fs);
            fs.Seek(0, SeekOrigin.Begin);

            var size = 0;
            const int timeout = 3000;
            while (IsLife()){
                int len = sockTcp.Length();
                if (len < 0){
                    break;
                }
                if (len == 0){
                    if (sockTcp.SockState != Bjd.sock.SockState.Connect){
                        break;
                    }
                    Thread.Sleep(10);
                    continue;
                }
                byte[] buf = sockTcp.Recv(len, timeout, this);
                if (buf.Length != len){
                    throw new IOException("buf.length!=len");
                }
                bw.Write(buf, 0, buf.Length);

                //トレース表示
                sb.Append(string.Format("Binary={0}byte ", len));
                size += len;

            }
            bw.Flush();
            bw.Close();
            fs.Close();
            
            //noEncode = true; //バイナリである事が分かっている
            //Trace(TraceKind.Send, Encoding.ASCII.GetBytes(sb.ToString()), true); //トレース表示

            return size;
        }

        private int SendBinary(SockTcp sockTcp, String fileName){
            var sb = new StringBuilder();
            sb.Append(string.Format("SendBinary({0}) ", fileName));

            int size = 0;

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)){
                using (var br = new BinaryReader(fs)){
                    var buf = new byte[3000000];
                    while (IsLife()) {
                        var len = br.Read(buf, 0, 3000000);
                        if (len <= 0) {
                            break;
                        }
                        //if (oneSsl != null) {
                        //}else{
                        sockTcp.Send(buf,len);
                        //}
                        //トレース表示
                        sb.Append(string.Format("Binary={0}byte ", len));
                        size += len;
                    }
                }
            }

            //noEncode = true; //バイナリである事が分かっている
            //Trace(TraceKind.Send, Encoding.ASCII.GetBytes(sb.ToString()), true); //トレース表示
            return size;
        }


        public override string GetMsg(int messageNo){
            switch (messageNo){
                case 1:
                    return IsJp ? "パラメータが長すぎます（不正なリクエストの可能性があるため切断しました)" : "A parameter is too long (I cut it off so that there was possibility of an unjust request in it)";
                case 2:
                    return IsJp ? "ホームディレクトリが存在しません（処理が継続できないため切断しました)" : "There is not a home directory (because I cannot continue processing, I cut it off)";
                case 3:
                    return IsJp ? "コマンド処理でエラーが発生しました" : "An error occurred by command processing";
                case 5:
                    return "login";
                case 6:
                    return "login";
                case 7:
                    return "success";
                case 8:
                    return "RENAME";
                case 9:
                    return "UP start";
                case 10:
                    return "UP end";
                case 11:
                    return "DOWN start";
                case 12:
                    return "DOWN end";
                case 13:
                    return "logout";
                case 14:
                    return IsJp ? "ユーザ名が無効です" : "A user name is null and void";
                case 15:
                    return IsJp ? "パスワードが違います" : "password is different";
                case 16:
                    return "sendBinary() IOException";
                case 17:
                    return "recvBinary() IOException";
                case 18:
                    return "Exception [session.CurrentDir.CreatePath]";
            }
            return null;
        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }
}

