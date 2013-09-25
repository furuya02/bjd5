using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using Bjd.browse;
using Bjd.log;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using Bjd.plugin;
using Bjd.remote;
using Bjd.server;
using Bjd.service;
using Bjd.sock;
using Bjd.tool;
using Bjd.trace;
using Bjd.util;
using Bjd.wait;
using Menu = Bjd.menu.Menu;

namespace Bjd{
    public class Kernel : IDisposable{

        //プロセス起動時に初期化される変数
        public RunMode RunMode { get; set; } //通常起動;
        public bool EditBrowse { get; private set; } //「参照」のテキストボックスの編集
        public Wait Wait { get; private set; }
        public RemoteConnect RemoteConnect { get; set; } //リモート制御で接続されている時だけ初期化される
        public RemoteClient RemoteClient { get; private set; }
        public TraceDlg TraceDlg { get; private set; } //トレース表示
        public DnsCache DnsCache { get; private set; }
        public Ver Ver { get; private set; }
        public View View { get; private set; }
        public LogView LogView { get; private set; }
        public WindowSize WindowSize { get; private set; }
        public Menu Menu { get; private set; }
        private readonly bool _isTest; //TEST用のKernelを生成する場合、trueに設定される
        public MailBox MailBox { get; private set; }

        //サーバ起動時に最初期化される変数
        public ListOption ListOption { get; private set; }
        public ListServer ListServer { get; private set; }
        public ListTool ListTool { get; private set; } //ツール管理
        public LogFile LogFile { get; private set; }
        private bool _isJp = true;
        private Logger _logger;

        //Ver5.9.6
        public WebApi WebApi { get; private set; }
        
        //Ver5.8.6
        public IniDb IniDb { get; private set; }

    
        //private MailBox mailBox = null; //実際に必要になった時に生成される(SMTPサーバ若しくはPOP3サーバの起動時)


        public bool IsJp(){
            return _isJp;
            //return (lang == Lang.JP) ? true : false;
        }

        public string ServerName{
            get{
                var oneOption = ListOption.Get("Basic");
                if (oneOption != null){
                    return (String) oneOption.GetValue("serverName");
                }
                return "";
            }
        }

        //テスト用コンストラクタ
        public Kernel(){
            _isTest = true;
            DefaultInitialize(null, null, null, null);
        }

        //テスト用コンストラクタ(MailBoxのみ初期化)
        public Kernel(String option){
            _isTest = true;
            DefaultInitialize(null, null, null, null);

            if (option.IndexOf("MailBox") != -1){
                var op = ListOption.Get("MailBox");
                var conf = new Conf(op);
                var dir = ReplaceOptionEnv((String)conf.Get("dir"));
                var datUser = (Dat)conf.Get("user");
                MailBox = new MailBox(null, datUser, dir);
            }
        }


        //* 通常使用されるコンストラクタ
        public Kernel(MainForm mainForm, ListView listViewLog, MenuStrip menuStrip, NotifyIcon notifyIcon){
            DefaultInitialize(mainForm, listViewLog, menuStrip, notifyIcon);
        }

        //起動時に、コンストラクタから呼び出される初期化
        private void DefaultInitialize(MainForm mainForm, ListView listViewLog, MenuStrip menuStrip, NotifyIcon notifyIcon){

            RunMode = RunMode.Normal;
            RemoteConnect = null;//リモート制御で接続されている時だけ初期化される

            //loggerが生成されるまでのログを一時的に保管する
            //ArrayList<LogTemporary> tmpLogger = new ArrayList<>();

            //プロセス起動時に初期化される
            View = new View(this, mainForm, listViewLog, notifyIcon);
            //logView = new LogView(listViewLog);
            LogView = new LogView(this,listViewLog);
            Menu = new Menu(this, menuStrip); //ここでは、オブジェクトの生成のみ、menu.Initialize()は、listInitialize()の中で呼び出される
            DnsCache = new DnsCache();
            Wait = new Wait();

            Ver = new Ver(); //バージョン管理

            //Java fix
            //RunModeの初期化
            if (mainForm == null){
                RunMode = RunMode.Service; //サービス起動
            } else{
                if (Environment.GetCommandLineArgs().Length > 1){
                    RunMode = RunMode.Remote; //リモートクライアント
                } else{
                    //サービス登録の状態を取得する
                    var setupService = new SetupService(this);
                    if (setupService.IsRegist)
                        RunMode = RunMode.NormalRegist; //サービス登録完了状態
                }
            }

            //Ver5.8.6 Java fix
            //OptionIni.Create(this); //インスタンスの初期化
            
            IniDb = new IniDb(ProgDir(), (RunMode == RunMode.Remote) ? "$remote" : "Option");
            
            MailBox = null;

            ListInitialize(); //サーバ再起動で、再度実行される初期化 


            if (_isTest){
                return;
            }

            //ウインドサイズの復元
            var path = string.Format("{0}\\BJD.ini", ProgDir());
            try{
                //ウインドウの外観を保存・復元(Viewより前に初期化する)
                WindowSize = new WindowSize(new Conf(ListOption.Get("Basic")), path);
                View.Read(WindowSize);
            } catch (IOException){
                WindowSize = null;
                // 指定されたWindow情報保存ファイル(BJD.ini)にIOエラーが発生している
                _logger.Set(LogKind.Error, null, 9000022, path);
            }

            //TraceDlg = new TraceDlg(this, (mainForm != null) ? mainForm.getFrame() : null); //トレース表示
            TraceDlg = new TraceDlg(this); //トレース表示

            switch (RunMode){
                case RunMode.Normal:
                    MenuOnClick("StartStop_Start"); //メニュー選択イベント
                    break;
                case RunMode.Remote:
                    RemoteClient = new RemoteClient(this);
                    RemoteClient.Start();
                    break;
                //Java fix Ver5.8.3
                case RunMode.NormalRegist:
                case RunMode.Service:
                    break;
                default:
                    Util.RuntimeException("Kernel.defaultInitialize() not implement (RunMode)");
                    break;
            }

            //Java fix Ver5.8.3
            View.SetColor();//ウインド色の初期化

        }

        //サーバ再起動で、再度実行される初期化
        public void ListInitialize(){
            //Loggerが使用できない間のログは、こちらに保存して、後でLoggerに送る
            var tmpLogger = new TmpLogger();

            //************************************************************
            // 破棄
            //************************************************************
            if (ListOption != null){
                ListOption.Dispose();
                ListOption = null;
            }
            //Java fix
            if (ListTool != null){
                ListTool.Dispose();
                ListTool = null;
            }
            if (ListServer != null){
                ListServer.Dispose();
                ListServer = null;
            }
            if (MailBox != null){
                MailBox = null;
            }
            if (LogFile != null){
                LogFile.Dispose();
                LogFile = null;
            }

            //************************************************************
            // 初期化
            //************************************************************
            //ListPlugin は。ListOptionとListServerを初期化する間だけ生存する
            //isTest=trueの場合、パスを""にして、プラグイン0個で初期化さあせる

            //ListPlugin listPlugin = new ListPlugin((isTest) ? "" : string.Format("%s\\plugins", getProgDir()));
            var listPlugin = new ListPlugin(ProgDir());
            foreach (var o in listPlugin){
                //リモートクライアントの場合、このログは、ややこしいので表示しない
                if (RunMode == RunMode.Normal){
                    tmpLogger.Set(LogKind.Detail, null, 9000008, string.Format("{0}Server", o.Name));
                }
            }

            //ListOptionで各オプションを初期化する前に、isJpだけは初期化しておく必要があるので
            //最初にOptionBasicのlangだけを読み出す
            //Ver5.8.6 Java fix
            //_isJp = OptionIni.GetInstance().IsJp();
            _isJp = IniDb.IsJp();

            ListOption = new ListOption(this, listPlugin);

            //Ver5.9.1
            //初めてここを通過するとき、過去のバージョンのOptionを読み込むと
            //旧オプションはオブジェクトの中のOneOptionにのみ保持される
            //この状態で、何かのオプション指定でOKすると、そのオプション以外が
            //Option.iniに保存されないため破棄されてしまう
            //この問題に対処するため、ここで一度、Option.iniを保存することにする
            if (!_isTest){
                ListOption.Save(IniDb);
            }


            //OptionBasic
            var confBasic = new Conf(ListOption.Get("Basic"));
            EditBrowse = (bool) confBasic.Get("editBrowse");

            //OptionLog
            var confOption = new Conf(ListOption.Get("Log"));
            LogView.SetFont((Font) confOption.Get("font"));

            if (RunMode == RunMode.Normal || RunMode == RunMode.Service){
                //LogFileの初期化
                var saveDirectory = (String) confOption.Get("saveDirectory");
                saveDirectory = ReplaceOptionEnv(saveDirectory);
                var normalLogKind = (int) confOption.Get("normalLogKind");
                var secureLogKind = (int) confOption.Get("secureLogKind");
                var saveDays = (int) confOption.Get("saveDays");
                var useLogClear = (bool) confOption.Get("useLogClear");
                if (!useLogClear){
                    saveDays = 0; //ログの自動削除が無効な場合、saveDaysに0をセットする
                }
                if (saveDirectory == ""){
                    tmpLogger.Set(LogKind.Error, null, 9000045, "It is not appointed");
                } else{
                    tmpLogger.Set(LogKind.Detail, null, 9000032, saveDirectory);
                    try{
                        LogFile = new LogFile(saveDirectory, normalLogKind, secureLogKind, saveDays);
                    } catch (IOException e){
                        LogFile = null;
                        tmpLogger.Set(LogKind.Error, null, 9000031, e.Message);
                    }
                }

                //Ver5.8.7 Java fix
                //mailBox初期化
                foreach (var o in ListOption) {
                    //SmtpServer若しくは、Pop3Serverが使用される場合のみメールボックスを初期化する                
                    if (o.NameTag == "Smtp" || o.NameTag == "Pop3") {
                        if (o.UseServer) {
                            var conf = new Conf(ListOption.Get("MailBox"));
                            var dir = ReplaceOptionEnv((String) conf.Get("dir"));
                            var datUser = (Dat) conf.Get("user");
                            var logger = CreateLogger("MailBox", (bool)conf.Get("useDetailsLog"), null);
                            MailBox = new MailBox(logger,datUser, dir);
                            break;
                        }
                    }
                }

            }
            _logger = CreateLogger("kernel", true, null);
            tmpLogger.Release(_logger);


            //Ver5.8.7 Java fix リモートクライアントの場合もメールボックスを作成してしまうバグを修正
//            //mailBox初期化
//            foreach (var o in ListOption){
//                //SmtpServer若しくは、Pop3Serverが使用される場合のみメールボックスを初期化する                
//                if (o.NameTag == "Smtp" || o.NameTag == "Pop3"){
//                    if (o.UseServer){
//                        var conf = new Conf(ListOption.Get("MailBox"));
//                        MailBox = new MailBox(this, conf);
//                        break;
//                    }
//                }
//            }

            ListServer = new ListServer(this, listPlugin);

            ListTool = new ListTool();
            ListTool.Initialize(this);

            View.SetColumnText(); //Logビューのカラムテキストの初期化
            Menu.Initialize(IsJp()); //メニュー構築（内部テーブルの初期化）

            WebApi = new WebApi();

        }

        //Confの生成
        //事前にListOptionが初期化されている必要がある
        public Conf CreateConf(String nameTag){
            if (ListOption == null){
                Util.RuntimeException("createConf() ListOption==null");
                return null;
            }
            var oneOption = ListOption.Get(nameTag);
            if (oneOption != null){
                return new Conf(oneOption);
            }
            return null;
        }

        //Loggerの生成
        //事前にListOptionが初期化されている必要がある
        public Logger CreateLogger(String nameTag, bool useDetailsLog, ILogger logger){
            if (ListOption == null){
                Util.RuntimeException("CreateLogger() ListOption==null || LogFile==null");
            }
            var conf = CreateConf("Log");
            if (conf == null){
                //CreateLoggerを使用する際に、OptionLogが検索できないのは、設計上の問題がある
                Util.RuntimeException("CreateLogger() conf==null");
                return null;
            }
            var dat = (Dat) conf.Get("limitString");
            var isDisplay = ((int) conf.Get("isDisplay")) == 0;
            var logLimit = new LogLimit(dat, isDisplay);

            var useLimitString = (bool) conf.Get("useLimitString");
            return new Logger(this,logLimit, LogFile, LogView, _isJp, nameTag, useDetailsLog, useLimitString, logger);
        }

        //終了処理
        public void Dispose(){

            //	        if (RunMode != RunMode.Service && RunMode != RunMode.Remote) {
            //	            //**********************************************
            //	            // 一旦ファイルを削除して現在有効なものだけを書き戻す
            //	            //**********************************************
            //	            var iniDb = new IniDb(ProgDir(),"Option");
            //	            iniDb.DeleteIni();

            //Ver5.8.6 Java fix 
            if (RunMode == RunMode.Normal) {
                var iniTmp = new IniDb(ProgDir(), "$tmp");//バックアップを作成してiniファイルを削除する
                //一旦、別ファイルに現在有効なものだけを書き戻す
                ListOption.Save(iniTmp);
                //上書きする
                File.Copy(iniTmp.Path, IniDb.Path,true);
                iniTmp.Delete();
            }else if (RunMode == RunMode.Remote){
                IniDb.Delete(); //$Remote.iniの削除
            }
            

            //**********************************************
            // 破棄
            //**********************************************
            ListServer.Dispose(); //各サーバは停止される
            ListOption.Dispose();
            ListTool.Dispose();
            MailBox = null;
            //	        }
            if (RemoteClient != null)
                RemoteClient.Dispose();

            View.Dispose();
            if (TraceDlg != null){
                TraceDlg.Dispose();
            }
            if (Menu != null){
                Menu.Dispose();
            }
            if (WindowSize != null){
                View.Save(WindowSize);
                WindowSize.Dispose(); //DisposeしないとReg.Dispose(保存)されない
            }
        }

        public string ProgDir(){
            if (_isTest){
                var dir = Directory.GetCurrentDirectory(); //テストプログラムのディレクトリ
                var src = Directory.GetParent(dir).Parent.FullName; //テストコードのディレクトリ
                return Directory.GetParent(src) + "\\BJD\\out"; //実プログラムのディレクトリ
            }
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        //オプションで指定される変数を置き変える

        public String ReplaceOptionEnv(String str){
            var executablePath = ProgDir();
            executablePath = executablePath.Replace("\\\\", "\\\\\\\\");
            str = str.Replace("%ExecutablePath%", executablePath);
            return str;
        }

        //public string Env(string str) {
        //    str = Util.SwapStr("%ExecutablePath%", ProgDir(), str);
        //    return str;
        //}


        private void Start(){

            //サービス登録されている場合の処理
            if (RunMode == RunMode.NormalRegist){
                //            var setupService = new SetupService(this);
                //            if (setupService.Status != ServiceControllerStatus.Running) {
                //                setupService.Job(ServiceCmd.Start);
                //            }
            } else{
                if (ListServer.Count == 0){
                    _logger.Set(LogKind.Error, null, 9000030, "");
                } else{
                    ListServer.Start();
                }
            }
        }

        private void Stop(){

            //サービス登録されている場合の処理
            if (RunMode == RunMode.NormalRegist){
                //            var setupService = new SetupService(this);
                //            if (setupService.Status == ServiceControllerStatus.Running) {
                //                setupService.Job(ServiceCmd.Stop);
                //            }
            } else{
                ListServer.Stop();
            }
        }

        //リモート操作(データの取得)
        public string Cmd(string cmdStr){
            var sb = new StringBuilder();


            sb.Append(IsJp() ? "(1) サービス状態" : "(1) Service Status");
            sb.Append("\b");

            foreach (var sv in ListServer){
                sb.Append("  " + sv);
                sb.Append("\b");
            }
            sb.Append(" \b");

            sb.Append(IsJp() ? "(2) ローカルアドレス" : "(2) Local address");
            sb.Append("\b");
            foreach (string addr in Define.ServerAddressList()){
                sb.Append(string.Format("  {0}", addr));
                sb.Append("\b");
            }

            return sb.ToString();
        }


        //メニュー選択時の処理
        public void MenuOnClick(String cmd){
            if (cmd.IndexOf("Option_") == 0){
                if (RunMode == RunMode.Remote){
                    //Java fix RunMOde==Remoteの場合のメニュー処理
                    RemoteClient.MenuOnClick(cmd);
                } else{
                    var oneOption = ListOption.Get(cmd.Substring(7));
                    if (oneOption != null){
                        var dlg = new OptionDlg(this, oneOption);
                        if (DialogResult.OK == dlg.ShowDialog()){
                            //Ver5.8.6 Java fix
                            //oneOption.Save(OptionIni.GetInstance());
                            oneOption.Save(IniDb);
                            MenuOnClick("StartStop_Reload");
                        }
                    }
                }
            } else if (cmd.IndexOf("Tool_") == 0){
                if (RunMode == RunMode.Remote){
                    //Java fix RunMOde==Remoteの場合のメニュー処理
                    RemoteClient.MenuOnClick(cmd);
                } else{
                    var nameTag = cmd.Substring(5);
                    var oneTool = ListTool.Get(nameTag);
                    if (oneTool == null)
                        return;

                    //BJD.EXE以外の場合、サーバオブジェクトへのポインタが必要になる
                    OneServer oneServer = null;
                    if (nameTag != "BJD"){
                        oneServer = ListServer.Get(nameTag);
                        if (oneServer == null){
                            return;
                        }
                    }

                    ToolDlg dlg = oneTool.CreateDlg(oneServer);
                    dlg.ShowDialog();
                }
            } else if (cmd.IndexOf("StartStop_") == 0){
                if (RunMode == RunMode.Remote){
                    //Java fix RunMOde==Remoteの場合のメニュー処理
                    RemoteClient.MenuOnClick(cmd);
                } else{
                    switch (cmd){
                        case "StartStop_Start":
                            Start();
                            break;
                        case "StartStop_Stop":
                            Stop();
                            break;
                        case "StartStop_Restart":
                            Stop();
                            Thread.Sleep(300);
                            Start();
                            break;
                        case "StartStop_Reload":
                            Stop();
                            ListInitialize();
                            Start();
                            break;
                        case "StartStop_Service":
                            SetupService(); //サービスの設定
                            break;
                        default:
                            Util.RuntimeException(string.Format("cmd={0}", cmd));
                            break;

                    }
                    View.SetColor(); //ウインドのカラー初期化
                    Menu.SetEnable(); //状態に応じた有効・無効
                }
            } else{
                switch (cmd){
                    case "File_LogClear":
                        LogView.Clear();
                        break;
                    case "File_LogCopy":
                        LogView.SetClipboard();
                        break;
                    case "File_Trace":
                        TraceDlg.Open();
                        break;
                    case "File_Exit":
                        View.MainForm.Close();
                        break;
                    case "Help_Version":
                        var dlg = new VersionDlg(this);
                        dlg.ShowDialog();
                        break;
                    case "Help_Homepage":
                        Process.Start(Define.WebHome());
                        break;
                    case "Help_Document":
                        Process.Start(Define.WebDocument());
                        break;
                    case "Help_Support":
                        Process.Start(Define.WebSupport());
                        break;
                }
            }

        }

        public String ChangeTag(String src){
            var tagList = new[]{"$h", "$v", "$p", "$d", "$a", "$s"};

            foreach (var tag in tagList){
                while (true){
                    var index = src.IndexOf(tag);
                    if (index == -1){
                        break;
                    }
                    var tmp1 = src.Substring(0, index);
                    var tmp2 = "";
                    switch (tag){
                        case "$h":
                            var serverName = ServerName;
                            tmp2 = serverName == "" ? "localhost" : serverName;
                            break;
                        case "$v":
                            tmp2 = Ver.Version();
                            break;
                        case "$p":
                            tmp2 = Define.ApplicationName();
                            break;
                        case "$d":
                            tmp2 = Define.Date();
                            break;
                        case "$a":
                            var localAddress = LocalAddress.GetInstance();
                            tmp2 = localAddress.RemoteStr();
                            //tmp2 = Define.ServerAddress();
                            break;
                        case "$s":
                            tmp2 = ServerName;
                            break;
                        default:
                            Util.RuntimeException(string.Format("undefind tag = {0}", tag));
                            break;
                    }
                    var tmp3 = src.Substring(index + 2);
                    src = tmp1 + tmp2 + tmp3;
                }
            }
            return src;
        }

        //IPアドレスの一覧取得
        public List<Ip> GetIpList(String hostName){
            var ar = new List<Ip>();
            try{
                var ip = new Ip(hostName);
                ar.Add(ip);
            } catch (ValidObjException){
                ar = DnsCache.GetAddress(hostName).ToList();
            }
            return ar;
        }

        //ディレクトリ情報取得（リモートクライアント用）
        public string GetBrowseInfo(string path){
            var sb = new StringBuilder();
            try{
                if (path == ""){
//ドライブ一覧取得
                    var drives = Directory.GetLogicalDrives();
                    foreach (string s in drives){
                        var driveName = s.ToUpper().Substring(0, 1);
                        var info = new DriveInfo(driveName);

                        var name = driveName + ":";
                        const int size = 0;
                        var dt = new DateTime(0);
                        BrowseKind browseKind;

                        if (info.DriveType == DriveType.Fixed){
                            browseKind = BrowseKind.DriveFixed;
                        } else if (info.DriveType == DriveType.CDRom){
                            browseKind = BrowseKind.DriveCdrom;
                        } else if (info.DriveType == DriveType.Removable){
                            browseKind = BrowseKind.DriveRemovable;
                        } else{
                            continue;
                        }
                        var p = new OneBrowse(browseKind, name, size, dt); //１データ生成
                        sb.Append(p + "\t"); //送信文字列生成

                    }
                } else{
                    string[] dirs = Directory.GetDirectories(path);
                    Array.Sort(dirs);
                    foreach (string s in dirs){
                        var name = s.Substring(path.Length);
                        var info = new DirectoryInfo(s);
                        const long size = 0;
                        var dt = info.LastWriteTime;
                        var p = new OneBrowse(BrowseKind.Dir, name, size, dt); //１データ生成
                        sb.Append(p + "\t"); //送信文字列生成
                    }
                    var files = Directory.GetFiles(path);
                    Array.Sort(files);
                    foreach (var s in files){
                        var name = s.Substring(path.Length);
                        var info = new FileInfo(s);
                        var size = info.Length;
                        var dt = info.LastWriteTime;
                        var p = new OneBrowse(BrowseKind.File, name, size, dt); //１データ生成
                        sb.Append(p + "\t"); //送信文字列生成
                    }
                }
            } catch{
                sb.Length = 0;
            }
            return sb.ToString();
        }

        public void SetupService(){
            //設定用ダイアログの表示
            var dlg = new SetupServiceDlg(this);
            dlg.ShowDialog();
        }
    }
}


