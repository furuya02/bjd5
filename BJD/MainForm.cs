using System;
using System.Windows.Forms;
using Bjd.util;


namespace Bjd {


    public partial class MainForm : Form {
        readonly Kernel _kernel;
        public MainForm() {
            InitializeComponent();
            //kernel = new Kernel(this, listViewMainLog, menuStrip, contextMenuStrip,notifyIcon);
            _kernel = new Kernel(this, listViewMainLog, menuStrip, notifyIcon);

        }


        private void MainFormActivated(object sender, EventArgs e) {
            _kernel.View.Activated();
        }
        //フォームが閉じられるとき
        private void MainFormFormClosing(object sender, FormClosingEventArgs e) {

            //プログラムの終了確認
            if (_kernel.RunMode == RunMode.Normal || _kernel.RunMode == RunMode.NormalRegist) {
                if ((bool)_kernel.ListOption.Get("Basic").GetValue("useExitDlg")) {
                    if (DialogResult.OK != Msg.Show(MsgKind.Question, _kernel.IsJp() ? "プログラムを終了してよろしいですか" : "May I finish a program?")) {
                        e.Cancel = true;//終了処理で中止された場合は、プログラムを終了しない
                        return;
                    }
                }
            }
            _kernel.Dispose();
        }
        protected override void WndProc(ref Message m) {
            //最小化メッセージをフックする
            //　WM_SYSCOMMAND(0x112) 
            if (m.Msg == 0x112){
                //SC_MINIMIZE(0xF020) 最小化
                if(m.WParam == (IntPtr)0xF020){
                    _kernel.View.SetVisible(false);
                    return;
                //Ver5.0.0-a5
                //SC_CLOSE(0xF060)クローズウインドウ
                }
                if(m.WParam == (IntPtr)0xF060){
                    _kernel.View.SetVisible(false);
                    return;                
                }
            }
            base.WndProc(ref m);
        } 

        private void PopupMenuClick(object sender, EventArgs e) {
            if (sender is NotifyIcon) {//タスクトレイアイコンのダブルクリック
                if (CheckPassword()) {//管理者パスワードの確認
                    _kernel.View.SetVisible(true);
                    LogEnsure();//Ver5.0.0-b23 最終行を表示する
                }
            } else {
                var menu = (ToolStripMenuItem)sender;
                if (menu.Name.IndexOf("PopupMenuOpen") == 0) {//「開く」
                    if (CheckPassword()) {//管理者パスワードの確認
                        _kernel.View.SetVisible(true);
                        LogEnsure();//Ver5.0.0-b23 最終行を表示する
                    }
                } else if (menu.Name.IndexOf("PopupMenuExit") == 0) {//「終了」
                    if (CheckPassword()) //管理者パスワードの確認
                        Close();
                }
            }
        }

        //Ver5.0.0-b23 最終行を表示する
        private void LogEnsure() {
            //Ver5.0.1
            if(listViewMainLog.Items.Count>0)
                listViewMainLog.EnsureVisible(listViewMainLog.Items[listViewMainLog.Items.Count - 1].Index);
        }

        //管理者パスワードの確認
        //Ver5.4.2
        //bool PasswordCheck() {
        bool CheckPassword() {
            var op = _kernel.ListOption.Get("Basic");
            if (!(bool)op.GetValue("useAdminPassword"))
                return true;
            var password = (string)op.GetValue("password");
            if (password == "") 
                return true;
            var dlg = new PasswordDlg(_kernel);
            while (true) {
                if (DialogResult.OK != dlg.ShowDialog())
                    return false;
                if (dlg.PasswordStr == password)
                    return true;
                Msg.Show(MsgKind.Error,(_kernel.IsJp()) ? "パスワードが違います" : "password incorrect");
            }
        }
    }

}
