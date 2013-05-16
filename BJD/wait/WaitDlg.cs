using System;
using System.Windows.Forms;

namespace Bjd.wait {
    internal partial class WaitDlg : Form {
        readonly Wait _owner;//このダイアログを使用している親クラス

        public WaitDlg(Wait owner) {
            InitializeComponent();

            _owner = owner;
        }


        //プログレスバーの値を更新する
        public void Renew() {
            if (progressBar.InvokeRequired) {// 別スレッドから呼び出された場合
                progressBar.Invoke(new MethodInvoker(Renew));
            } else {
                //メッセージを更新
                labelMeg.Text = _owner.Msg;
                //プログレスバーの値を更新
                progressBar.Maximum = _owner.Max;
                progressBar.Value = _owner.Val;
            }
        }

        new public void Close() {
            if (progressBar.InvokeRequired) {
                progressBar.Invoke(new MethodInvoker(Close));
            } else {
                base.Close();
            }
        }
        public void Open() {
            if (progressBar.InvokeRequired) {
                progressBar.Invoke(new MethodInvoker(Open));
            }else{
                ShowDialog();
            }
        }

        //キャンセル
        private void ButtonCancelClick(object sender, EventArgs e) {
            _owner.Life = false;
        }

        private void WaitMsgDlgFormClosing(object sender, FormClosingEventArgs e) {
            _owner.Life = false;
        }
    }
}
//【使用例】
//    int max = 100;
//    kernel.Wait.Start("しばらくお待ちください。");//ダイアログの表示
//    kernel.Wait.Max = max;//プログレスバーの最大値
//    for (int i = 0;i < max && kernel.Wait.Life;i++) {//ダイアログが無効化された場合 Life=falseになる
//         kernel.Wait.Val = i;//プログレスバーの値
//        Thread.Sleep(10);
//    }
//    kernel.Wait.Stop();//ダイアログのクローズ