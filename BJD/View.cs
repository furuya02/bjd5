using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace Bjd{
    public class View : IDisposable{
        private readonly Kernel _kernel;
        private readonly NotifyIcon _notifyIcon;
        public ListView ListView { get; private set; }
        public MainForm MainForm { get; private set; }

        //    public View(Kernel kernel, MainForm mainForm, ListView listView, NotifyIcon notifyIcon) {
        public View(Kernel kernel, MainForm mainForm, ListView listView, NotifyIcon notifyIcon){
            _kernel = kernel;
            _notifyIcon = notifyIcon;
            MainForm = mainForm;
            ListView = listView;

            if (listView == null){
                return;
            }
            //		for (int i = 0; i < 8; i++) {
            //			listView.addColumn("");
            //		}
            //		listView.setColWidth(0, 120);
            //		listView.setColWidth(1, 60);
            //		listView.setColWidth(2, 60);
            //		listView.setColWidth(3, 80);
            //		listView.setColWidth(4, 80);
            //		listView.setColWidth(5, 70);
            //		listView.setColWidth(6, 200);
            //		listView.setColWidth(7, 300);
        }

        //フォームがアクティブにされた時
        private bool _isFirst = true; //最初の１回だげ実行する

        public void Activated(){
            if (_isFirst){
                _isFirst = false;

                //デフォルトで表示
                if (_notifyIcon != null)
                    _notifyIcon.Visible = false;

                if (_kernel.RunMode != RunMode.Remote){
                    var option = _kernel.ListOption.Get("Basic");
                    if (option != null){
                        var useLastSize = (bool) option.GetValue("useLastSize");
                        if (useLastSize){
                            //Ver5.6.0
                            _kernel.WindowSize.Read(ListView); //カラム幅の復元
                            _kernel.WindowSize.Read(MainForm); //終了時のウインドウサイズの復元
                        }
                        //「起動時にウインドウを開く」が指定されていない場合は、アイコン化する
                        if (!(bool) option.GetValue("isWindowOpen")){
                            SetVisible(false); //非表示
                        }
                    }
                }
            }
        }


        public void Dispose(){
            if (ListView == null)
                return;
            //タスクトレイのアイコンを強制的に非表示にする
            _notifyIcon.Visible = false;

            _kernel.WindowSize.Save(MainForm); //ウインドウサイズの保存
            _kernel.WindowSize.Save(ListView); //カラム幅の保存
            
        }


        //リストビューのカラー変更  
        public void SetColor(){
            if (ListView == null)
                return;
            if (ListView.InvokeRequired){
                ListView.BeginInvoke(new MethodInvoker(SetColor));
            }
            else{
                var color = SystemColors.Window;
                switch (_kernel.RunMode){
                    case RunMode.Normal:
                        //サーバプログラムが１つも起動していない場合
                        if (!_kernel.ListServer.IsRunnig())
                            color = Color.LightGray;
                        //リモート接続を受けている場合
                        if (_kernel.RemoteConnect != null)
                            color = Color.LightCyan;
                        break;
                    case RunMode.NormalRegist:
                        color = Color.LightSkyBlue;
                        break;
                    case RunMode.Remote:
                        color = (_kernel.RemoteClient != null && _kernel.RemoteClient.IsConected)
                                    ? Color.LightGreen
                                    : Color.DarkGreen;
                        break;
                }
                ListView.BackColor = color;
            }
        }


        //カラムのタイトル初期化
        public void SetColumnText(){
            if (ListView == null)
                return;

            //Ver5.8.6 Java fix
            if (ListView.Columns.Count != 8){
                return;
            }

            if (ListView.InvokeRequired){
                ListView.Invoke(new MethodInvoker(SetColumnText));
            }
            else{
                //リストビューのカラム初期化（言語）
                ListView.Columns[0].Text = (_kernel.IsJp()) ? "日時" : "DateTime";
                ListView.Columns[1].Text = (_kernel.IsJp()) ? "種類" : "Kind";
                ListView.Columns[2].Text = (_kernel.IsJp()) ? "スレッドID" : "Thread ID";
                ListView.Columns[3].Text = (_kernel.IsJp()) ? "機能(サーバ)" : "Function(Server)";
                ListView.Columns[4].Text = (_kernel.IsJp()) ? "アドレス" : "Address";
                ListView.Columns[5].Text = (_kernel.IsJp()) ? "メッセージID" : "Message ID";
                ListView.Columns[6].Text = (_kernel.IsJp()) ? "説明" : "Explanation";
                ListView.Columns[7].Text = (_kernel.IsJp()) ? "詳細情報" : "Detailed information";
            }
        }




        public void SetVisible(bool enabled){
            if (ListView == null)
                return;

            //this.enabled = enabled;


            if (_kernel.RunMode == RunMode.Remote){
                //リモートクライアントはタスクトレイに格納しない
                MainForm.WindowState = !enabled ? FormWindowState.Minimized : FormWindowState.Normal;
            }
            else{
                if (!enabled){
                    MainForm.Visible = false; //非表示
                    _notifyIcon.Visible = true; //タスクトレイにアイコン表示
                    MainForm.ShowInTaskbar = false; //タスクバーにアイコン非表示

                }
                else{
                    _notifyIcon.Visible = false; //タスクトレイにアイコン非表示
                    MainForm.ShowInTaskbar = true; //タスクバーにアイコン表示
                    MainForm.Visible = true; //表示
                }
            }
        }


        public void Save(WindowSize windowSize){
            if (MainForm == null || ListView == null || windowSize == null){
                return;
            }
            windowSize.Save(MainForm);
            windowSize.Save(ListView);
        }

        public void Read(WindowSize windowSize){
            if (MainForm == null || ListView == null || windowSize == null){
                return;
            }
            windowSize.Read(MainForm);
            windowSize.Read(ListView);
        }

        public void Close(){
            if (ListView == null)
                return;
            MainForm.Close();
        }
    }
}

/*
    public class View:IDisposable {
        readonly Kernel _kernel;
        readonly MainForm _mainForm;
        readonly ListView _listView;
        readonly NotifyIcon _notifyIcon;
        
        //bool enabled = true;//デフォルトで表示

        public View(Kernel kernel, MainForm mainForm, ListView listView, NotifyIcon notifyIcon) {
            _kernel = kernel;
            _mainForm = mainForm;
            _listView = listView;
            _notifyIcon = notifyIcon;

            if (listView == null)
                return;

            SetLang();
            SetColor();

        }
        //フォームがアクティブにされた時
        bool _isFirst = true;//最初の１回だげ実行する
        public void Activated() {
            if (_isFirst) {
                _isFirst = false;

                //デフォルトで表示
                if (_notifyIcon != null)
                    _notifyIcon.Visible = false;

                if (_kernel.RunMode != RunMode.Remote) {
                    var option = _kernel.ListOption.Get("Basic");
                    if (option != null) {
                        var useLastSize = (bool)option.GetValue("useLastSize");
                        if (useLastSize) {//Ver5.6.0
                            _kernel.WindowSize.Read(_listView);//カラム幅の復元
                            _kernel.WindowSize.Read(_mainForm);//終了時のウインドウサイズの復元
                        }
                        //「起動時にウインドウを開く」が指定されていない場合は、アイコン化する
                        if (!(bool)option.GetValue("isWindowOpen")) {
                            SetVisible(false);//非表示
                        }
                    }
                }
            }
        }
        public void Dispose() {
            if (_listView == null)
                return;
            //タスクトレイのアイコンを強制的に非表示にする
            _notifyIcon.Visible = false;

            _kernel.WindowSize.Save(_mainForm);//ウインドウサイズの保存
            _kernel.WindowSize.Save(_listView);//カラム幅の保存
        }
        
        public void Close() {
            if (_listView == null)
                return;
            _mainForm.Close();
        }

        //リストビューのカラー変更  
        public void SetColor() {
            if (_listView == null)
                return;
            if (_listView.InvokeRequired) {
                _listView.BeginInvoke(new MethodInvoker(SetColor));
            } else {
                var color = SystemColors.Window;
                switch (_kernel.RunMode) {
                    case RunMode.Normal:
                        //サーバプログラムが１つも起動していない場合
                        if (!_kernel.ListServer.IsRunnig)
                            color = Color.LightGray;
                        //リモート接続を受けている場合
                        if(_kernel.RemoteServer!=null)
                            color = Color.LightCyan;
                        break;
                    case RunMode.NormalRegist:
                        color = Color.LightSkyBlue;
                        break;
                    case RunMode.Remote:
                        color = (_kernel.RemoteClient!=null && _kernel.RemoteClient.IsConected) ? Color.LightGreen : Color.DarkGreen;
                        break;
                }
                _listView.BackColor = color;
            }
        }


        public void SetLang() {
            if (_listView == null)
                return;

            if (_listView.InvokeRequired) {
                _listView.Invoke(new MethodInvoker(SetLang));
            } else {
                //リストビューのカラム初期化（言語）
                _listView.Columns[0].Text = (_kernel.IsJp()) ? "日時" : "DateTime";
                _listView.Columns[1].Text = (_kernel.IsJp()) ? "種類" : "Kind";
                _listView.Columns[2].Text = (_kernel.IsJp()) ? "スレッドID" : "Thread ID";
                _listView.Columns[3].Text = (_kernel.IsJp()) ? "機能(サーバ)" : "Function(Server)";
                _listView.Columns[4].Text = (_kernel.IsJp()) ? "アドレス" : "Address";
                _listView.Columns[5].Text = (_kernel.IsJp()) ? "メッセージID" : "Message ID";
                _listView.Columns[6].Text = (_kernel.IsJp()) ? "説明" : "Explanation";
                _listView.Columns[7].Text = (_kernel.IsJp()) ? "詳細情報" : "Detailed information";
            }
        }

        public void Activate() {
            if (_listView == null)
                return;
            _mainForm.Activate();
        }

        public void SetVisible(bool enabled) {
            if (_listView == null)
                return;

            //this.enabled = enabled;


            if (_kernel.RunMode == RunMode.Remote) {
                //リモートクライアントはタスクトレイに格納しない
                _mainForm.WindowState = !enabled ? FormWindowState.Minimized : FormWindowState.Normal;
            } else {
                if (!enabled) {
                    _mainForm.Visible = false;//非表示
                    _notifyIcon.Visible = true;//タスクトレイにアイコン表示
                    _mainForm.ShowInTaskbar = false;//タスクバーにアイコン非表示

                } else {
                    _notifyIcon.Visible = false;//タスクトレイにアイコン非表示
                    _mainForm.ShowInTaskbar = true;//タスクバーにアイコン表示
                    _mainForm.Visible = true;//表示
                }
            }
        }

    }
     */
