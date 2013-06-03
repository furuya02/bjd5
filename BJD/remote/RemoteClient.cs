using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using Bjd.browse;
using Bjd.ctrl;
using Bjd.net;
using Bjd.option;
using Bjd.log;
using Bjd.sock;
using Bjd.util;

namespace Bjd.remote {
    public class RemoteClient : ThreadBase {
        readonly Logger _logger;
        readonly string _optionFileName;
        private readonly Kernel _kernel;

        private readonly Ip _ip = new Ip(IpKind.V4_0);
        readonly int _port = 10001;//デフォルト値(10001)　起動時のパラメータで指定されない場合は10001が使用される

        SockTcp _sockTcp;
        ToolDlg _toolDlg;
        BrowseDlg _browseDlg;
        
        public RemoteClient(Kernel kernel)
            :base(kernel.CreateLogger("RemoteClient",true,null)){

            _kernel = kernel;
            
            var args = Environment.GetCommandLineArgs();
            
            //this.kernel = kernel;
            IsConected = false;
            _logger = _kernel.CreateLogger("RemoteClient", true, this);
            _optionFileName = string.Format("{0}\\{1}.ini", _kernel.ProgDir(), "$remote");

            //Java fix IsJpは現時点では不明
            _kernel.Menu.InitializeRemote(true);//切断時の軽量メニュー
            //_kernel.Menu.OnClick += Menu_OnClick;
            
            //コマンドライン引数の処理
            if (args.Length != 2 && args.Length !=3) {
                _logger.Set(LogKind.Error,null,1,string.Format("args.Length={0}",args.Length));
                return;
            }
            //接続先アドレス
            try{
                _ip = new Ip(args[1]);
            }catch(ValidObjException){
                _logger.Set(LogKind.Error,null,2,string.Format("ip={0}", args[1]));
                return;
            }
            //_ip = new Ip(args[1]);
            //if (_ip.ToString() == "0.0.0.0") {
            //    _logger.Set(LogKind.Error,null,2,string.Format("ip={0}", args[1]));
            //    return;
            //}
            //接続先ポート番号
            if (args.Length == 3) {
                try {
                    _port = Convert.ToInt32(args[2]);
                } catch {
                    _logger.Set(LogKind.Error,null,3,string.Format("port={0}", args[2]));
                    _ip = new Ip(IpKind.V4_0);//初期化失敗
                }
            }
        }

        
        //****************************************************************
        //プロパティ
        //****************************************************************
        public bool IsConected { get; private set; }
        new public void Dispose() {//破棄時処理

            Stop();
            File.Delete(_optionFileName);

            base.Dispose();
        }
        override protected bool OnStartThread() {//前処理
            return true;
        }
        override protected void OnStopThread() {}

        //後処理
        override protected void OnRunThread() {//本体

            _kernel.View.SetColor();//【ウインド色】

            //[C#]
            ThreadBaseKind = ThreadBaseKind.Running;


            if (_ip == new Ip(IpKind.V4_0)) {
                return; //初期化失敗
            }
            
            while (IsLife()) {

                //TraceDlg traceDlg = null;
                Ssl ssl = null;
                var timeout = 3;
                _sockTcp = Inet.Connect(_kernel,_ip,_port,timeout,ssl);

                if (_sockTcp == null) {
                    //isRun = false;
                    _logger.Set(LogKind.Error, _sockTcp,4,string.Format("address={0} port={1}", _ip, _port));
                    //再接続を試みるのは、2秒後
                    for (int i = 0; i < 20 && IsLife(); i++) {
                        Thread.Sleep(100);
                    }
                } else {

                    _logger.Set(LogKind.Normal,_sockTcp,5,string.Format("address={0} port={1}",_ip,_port));

                    while (IsLife()) {//接続中

                        if (_sockTcp.SockState != SockState.Connect){
                            //接続が切れた場合は、少しタイミングを置いてから、再接続処理に戻る
                            //再接続を試みるのは、1秒後
                            _sockTcp.Close();
                            for (int i = 0; i < 10 && IsLife(); i++) {
                                Thread.Sleep(100);
                            }
                            break;
                        }

                        var o = RemoteData.Recv(_sockTcp,this);
                        if (o==null) {
                            Thread.Sleep(100);
                            continue;
                        }

                        switch(o.Kind){
                            case RemoteDataKind.DatAuth://認証情報（パスワード要求）
                                var dlg = new PasswordDlg(_kernel);
                                if (DialogResult.OK == dlg.ShowDialog()) {
                                    //ハッシュ文字列の作成（MD5）
                                    string md5Str = Inet.Md5Str(dlg.PasswordStr + o.Str);
                                    //DAT_AUTHに対するパスワード(C->S)
                                    RemoteData.Send(_sockTcp,RemoteDataKind.CmdAuth, md5Str);

                                } else {
                                    StopLife();//Ver5.8.4 暫定処置
                                }
                                break;
                            case RemoteDataKind.DatVer://バージョン情報
                                if (!_kernel.Ver.VerData(o.Str)) {
                                    //サーバとクライアントでバージョンに違いが有る場合、クライアント機能を停止する
                                    StopLife();//Ver5.8.4 暫定処置
                                } else {
                                    IsConected = true;//接続中
                                    _kernel.View.SetColor();//【ウインド色】

                                    //ログイン完了
                                    _logger.Set(LogKind.Normal,_sockTcp,10,"");
                                }
                                break;
                            case RemoteDataKind.DatLocaladdress://ローカルアドレス
                                LocalAddress.SetInstance(o.Str);
                                //_kernel.LocalAddress = new LocalAddress(o.Str);
                                break;
                            case RemoteDataKind.DatTool://データ受信
                                if (_toolDlg != null) {
                                    var tmp = o.Str.Split(new[] { '\t' }, 2);
                                    _toolDlg.CmdRecv(tmp[0],tmp[1]);
                                }
                                break;
                            case RemoteDataKind.DatBrowse://ディレクトリ情報受信
                                if(_browseDlg != null) {
                                    _browseDlg.CmdRecv(o.Str);
                                }
                                break;
                            case RemoteDataKind.DatTrace://トレース受信
                                _kernel.TraceDlg.AddTrace(o.Str);
                                break;
                            case RemoteDataKind.DatLog://ログ受信
                                _kernel.LogView.Append(new OneLog(o.Str));//ログビューへの追加
                                break;
                            case RemoteDataKind.DatOption://オプションの受信
                                //Option.iniを受信して$remote.iniに出力する
                                using (var sw = new StreamWriter(_optionFileName, false, Encoding.GetEncoding("Shift_JIS"))) {
                                    sw.Write(o.Str);
                                    sw.Close();
                                }
                                _kernel.ListInitialize();

                                break;
                            default:
                                _logger.Set(LogKind.Error, null,999,string.Format("kind = {0}",o.Kind));
                                break;
                        }

                    }
                //err:
                    _sockTcp.Close();
                    _sockTcp = null;
                    IsConected = false;//接続断
                    _kernel.Menu.InitializeRemote(_kernel.IsJp());
                    _kernel.View.SetColor();
                    _logger.Set(LogKind.Normal,null, 8,"");
                }
            }
            _logger.Set(LogKind.Normal,null, 7,"");//リモートクライアント停止
        }
        public void VisibleTrace2(bool enabled) {
            //TraceDlgの表示・非表示(C->S)
            RemoteData.Send(_sockTcp, RemoteDataKind.CmdTrace, enabled ? "1" : "0");
        }

        //RunModeがRemoteの場合、KernelのMenuOnClickから、こちらが呼ばれる
        public void MenuOnClick(String cmd){
            //オプションメニューの場合
            if (cmd.IndexOf("Option_") == 0){
                var oneOption = _kernel.ListOption.Get(cmd.Substring(7));
                if (oneOption != null) {
                    var dlg = new OptionDlg(_kernel, oneOption);
                    if (DialogResult.OK == dlg.ShowDialog()) {
                        oneOption.Save(_kernel.IniDb);//オプションを保存する
                        //サーバ側へ送信する
                        string optionStr;
                        using (var sr = new StreamReader(_optionFileName, Encoding.GetEncoding("Shift_JIS"))) {
                            optionStr = sr.ReadToEnd();
                            sr.Close();
                        }
                        //Optionの送信(C->S)
                        RemoteData.Send(_sockTcp, RemoteDataKind.CmdOption, optionStr);
                    }
                }
            //「ツール」メニューの場合
            }else if (cmd.IndexOf("Tool_") == 0){
                var oneTool = _kernel.ListTool.Get(cmd.Substring(5));
                if (oneTool != null) {
                    _toolDlg = oneTool.CreateDlg(_sockTcp);
                    _toolDlg.ShowDialog();
                    _toolDlg.Dispose();
                    _toolDlg = null;
                }
            //「起動／停止」の場合
            } else if (cmd.IndexOf("StartStop_") == 0) {
                string nameTag = cmd.Substring(10);
                if (nameTag == "Restart") {
                    if (_sockTcp != null) {
                        //「再起動」メニュー選択(C->S)
                        RemoteData.Send(_sockTcp, RemoteDataKind.CmdRestart, "");
                    }
                }
            }
        }

        public override string GetMsg(int messageNo) {
            switch (messageNo) {
                    case 1: return (_kernel.IsJp())?"リモートクライアントが起動できません（引数が足りません）" : "RemoteClient can't start(A lack of parameter)";
                    case 2: return (_kernel.IsJp())?"リモートクライアントが起動できません（アドレスに問題があります）":"RemoteClient can't start(There is a problem to an address)";
                    case 3: return (_kernel.IsJp())?"リモートクライアントが起動できません（ポート番号に問題があります）":"RemoteClient can't start(There is a problem to a port number)";
                    case 4: return (_kernel.IsJp())?"サーバへ接続できません":"Can't be connected to a server";
                    case 5: return (_kernel.IsJp())?"サーバへ接続しました":"Connected to a server";
                    case 6: return (_kernel.IsJp())?"リモートクライアント開始":"RemoteClient started it";
                    case 7: return (_kernel.IsJp())?"リモートクライアント停止":"RemoteClient stopped";
                    case 8: return (_kernel.IsJp())?"リモートサーバから切断されました":"Disconnected to a remote server";
                    case 9: return (_kernel.IsJp())?"リモートクライアントが起動できません（ポート番号[データ用]に問題があります）":"RemoteClient can't start(There is a problem to a port number [data port])";
                    case 10: return (_kernel.IsJp())?"ログイン":"Login";
                    case 11: return (_kernel.IsJp()) ? "無効なデータです" : "invalid data";
            }
            return "unknown";
        }
        
        public string ShowBrowseDlg(CtrlType ctrlType) {
            string resultStr = null;
            _browseDlg = new BrowseDlg(_kernel, _sockTcp, ctrlType);
            _browseDlg.Init();
            if(DialogResult.OK == _browseDlg.ShowDialog()) {
                resultStr = _browseDlg.Result;
            }
            _browseDlg = null;
            return resultStr;
        }
    }
}
