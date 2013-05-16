using System;
using System.Windows.Forms;
using Bjd.service;

namespace Bjd {
    static class Program {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main() {

            //起動ユーザがSYSTEMの場合、サービス起動であると判断する
            if (Environment.UserName == "SYSTEM") {
                Service.ServiceMain();
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                Application.Run(new MainForm());
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + ex.StackTrace,"BlackJumboDog",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
    }
}