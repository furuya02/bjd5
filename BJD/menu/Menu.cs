using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Bjd.menu{

    //メニューを管理するクラス
    public class Menu : IDisposable{
        private readonly Kernel _kernel;
        private readonly MenuStrip _menuStrip;
        private readonly System.Timers.Timer _timer; //[C#]
        private readonly Dictionary<OneMenu, ToolStripMenuItem> _ar = new Dictionary<OneMenu, ToolStripMenuItem>();

        //Java fix
        private bool _isJp;


        //[C#]【メニュー選択時のイベント】
        //public delegate void MenuClickHandler(ToolStripMenuItem menu);//デリゲート
        //public event MenuClickHandler OnClick;//イベント
        readonly Queue<ToolStripMenuItem> _queue = new Queue<ToolStripMenuItem>();

        public Menu(Kernel kernel, MenuStrip menuStrip){
            _kernel = kernel;
            _menuStrip = menuStrip;
            
            //[C#]
            _timer = new System.Timers.Timer{Interval = 100};
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = true;
        }

        //[C#]メニュー選択のイベントを発生させる
        //synchro=false 非同期で動作する
        public void EnqueueMenu(string name, bool synchro) {
            var item = new ToolStripMenuItem { Name = name };
            if (synchro) {
                _kernel.MenuOnClick(name);
            } else {
                _queue.Enqueue(item);//キューに格納する
            }
        }

        //[C#]タイマー起動でキューに入っているメニューイベントを実行する
        void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e) {
            if (_queue.Count > 0) {
                var q = _queue.Dequeue();
                _kernel.MenuOnClick(q.Name);
            }
        }

        //終了処理
        public void Dispose(){
        }

        //Java fix パラメータisJpを追加
        //メニュー構築（内部テーブルの初期化）
        public void InitializeRemote(bool isJp) {
            if (_menuStrip == null)
                return;
            
            if (_menuStrip.InvokeRequired) {
                _menuStrip.BeginInvoke(new MethodInvoker(()=>InitializeRemote(isJp)));
            } else {
                //Java fix
                _isJp = isJp;
                //全削除
                _menuStrip.Items.Clear();
                _ar.Clear();

                var subMenu = new ListMenu { new OneMenu("File_Exit", "終了", "Exit",'X',Keys.None) };
                //「ファイル」メニュー
                var m = AddSubMenu(_menuStrip.Items, new OneMenu("File", "ファイル", "File", 'F', Keys.None));
                AddListMenu(m, subMenu);

            }
        }

        //メニュー構築（内部テーブルの初期化） リモート用

        //Java fix パラメータisJpを追加
        //メニュー構築（内部テーブルの初期化） 通常用
        public void Initialize(bool isJp){
            _isJp = isJp;

            if (_menuStrip == null){
                return;
            }

            if (_menuStrip.InvokeRequired){
                _menuStrip.Invoke(new MethodInvoker(()=>Initialize(isJp)));
            }else{
                //全削除
                _menuStrip.Items.Clear();
                _ar.Clear();

                //「ファイル」メニュー
                var m = AddSubMenu(_menuStrip.Items, new OneMenu("File", "ファイル","File", 'F', Keys.None));
                AddListMenu(m, FileMenu());


                //「オプション」メニュー
                m = AddSubMenu(_menuStrip.Items, new OneMenu("Option", "オプション", "Option", 'O', Keys.None));
                AddListMenu(m, _kernel.ListOption.GetListMenu());

                //「ツール」メニュー
                m = AddSubMenu(_menuStrip.Items, new OneMenu("Tool", "ツール", "Tool", 'T', Keys.None));
                AddListMenu(m, _kernel.ListTool.GetListMenu());
                //
                //「起動/停止」メニュー
                m = AddSubMenu(_menuStrip.Items, new OneMenu("StartStop", "起動/停止", "Start/Stop", 'S', Keys.None));
                AddListMenu(m, StartStopMenu());
                
                //「ヘルプ」メニュー
                m = AddSubMenu(_menuStrip.Items, new OneMenu("Help", "ヘルプ", "Help", 'H', Keys.None));
                AddListMenu(m, HelpMenu());

                //Java fix
                SetEnable();//状況に応じた有効無効
                // menuBar.updateUI(); //メニューバーの再描画
            }
        }


        //ListMenuの追加 (再帰)
        void AddListMenu(ToolStripMenuItem owner, ListMenu subMenu) {
            foreach (var o in subMenu) {
                AddSubMenu(owner.DropDownItems, o);
            }
        }
        //OneMenuの追加
        ToolStripMenuItem AddSubMenu(ToolStripItemCollection items, OneMenu o) {
            if (o.Name == "-") {
                items.Add("-");//ToolStripSeparatorが生成される
                return null;
            }

            //Java fix _isJp対応
            var title = string.Format("{0}", o.EnTitle);
            if (_isJp){
                title = string.Format("{0}(&{1})", o.JpTitle, o.Mnemonic);
                if (o.Mnemonic == '0') { //0が指定された場合、ショートカットは無効
                    title = o.JpTitle;
                }
            }
            var item = (ToolStripMenuItem)items.Add(title);
            

            item.Name = o.Name;//名前
            item.ShortcutKeys = o.Accelerator;//ショッートカット
            item.Click += MenuItemClick;//クリックイベンント
            AddListMenu(item, o.SubMenu);//再帰処理(o.SubMenu.Count==0の時、処理なしで戻ってくる)

            _ar.Add(o,item);//内部テーブルへの追加
            return item;
        }


        //メニュー選択時のイベント処理
        void MenuItemClick(object sender, EventArgs e){

            var cmd = ((ToolStripMenuItem) sender).Name;
            _kernel.MenuOnClick(cmd);
        }



        //状況に応じた有効/無効のセット
        public void SetEnable(){
            if (_kernel.RunMode == RunMode.NormalRegist){
                //サービス登録されている場合
                //サーバの起動停止はできない
                SetEnabled("StartStop_Start", false);
                SetEnabled("StartStop_Stop", false);
                SetEnabled("StartStop_Restart", false);
                SetEnabled("StartStop_Service", true);
                SetEnabled("File_LogClear", false);
                SetEnabled("File_LogCopy", false);
                SetEnabled("File_Trace", false);
                SetEnabled("Tool", false);
            }
            else if (_kernel.RunMode == RunMode.Remote){
                //リモートクライアント
                //サーバの再起動のみ
                SetEnabled("StartStop_Start", false);
                SetEnabled("StartStop_Stop", false);
                SetEnabled("StartStop_Restart", true);
                SetEnabled("StartStop_Service", false);
                SetEnabled("File_LogClear", true);
                SetEnabled("File_LogCopy", true);
                SetEnabled("File_Trace", true);
                SetEnabled("Tool", true);
            }
            else{
                //通常起動
                //Util.sleep(0); //起動・停止が全部完了してから状態を取得するため
                var isRunning = _kernel.ListServer.IsRunnig();
                SetEnabled("StartStop_Start", !isRunning);
                SetEnabled("StartStop_Stop", isRunning);
                SetEnabled("StartStop_Restart", isRunning);
                SetEnabled("StartStop_Service", !isRunning);
                SetEnabled("File_LogClear", true);
                SetEnabled("File_LogCopy", true);
                SetEnabled("File_Trace", true);
                SetEnabled("Tool", true);
            }
        }

        //有効/無効
        private void SetEnabled(String name, bool enabled){
            foreach (var o in _ar){
                if (o.Key.Name == name){
                    o.Value.Enabled = enabled;
                    return;
                }
            }
        }

        //「ファイル」のサブメニュー
        private ListMenu FileMenu(){
            ListMenu subMenu = new ListMenu();
            subMenu.Add(new OneMenu("File_LogClear", "ログクリア", "Loglear", 'C', Keys.F1));
            subMenu.Add(new OneMenu("File_LogCopy", "ログコピー", "LogCopy", 'L', Keys.F2));
            subMenu.Add(new OneMenu("File_Trace", "トレース表示", "Trace", 'T', Keys.None));
            subMenu.Add(new OneMenu()); // セパレータ
            subMenu.Add(new OneMenu("File_Exit", "終了", "Exit", 'X', Keys.None));
            return subMenu;
        }

        //「起動/停止」のサブメニュー
        private ListMenu StartStopMenu(){
            ListMenu subMenu = new ListMenu();
            subMenu.Add(new OneMenu("StartStop_Start", "サーバ起動", "Start", 'S', Keys.None));
            subMenu.Add(new OneMenu("StartStop_Stop", "サーバ停止", "Stop", 'P', Keys.None));
            subMenu.Add(new OneMenu("StartStop_Restart", "サーバ再起動", "Restart", 'R', Keys.None));
            subMenu.Add(new OneMenu("StartStop_Service", "サービス設定", "Service", 'S', Keys.None));
            return subMenu;
        }

        //「ヘルプ」のサブメニュー
        private ListMenu HelpMenu(){
            ListMenu subMenu = new ListMenu();
            subMenu.Add(new OneMenu("Help_Homepage", "ホームページ", "HomePage", 'H', Keys.None));
            subMenu.Add(new OneMenu("Help_Document", "ドキュメント", "Document", 'D', Keys.None));
            subMenu.Add(new OneMenu("Help_Support", "サポート掲示板", "Support", 'S', Keys.None));
            subMenu.Add(new OneMenu("Help_Version", "バージョン情報", "Version", 'V', Keys.None));
            return subMenu;
        }
    }
}

