﻿using System;
using System.Windows.Forms;

namespace Bjd {
    public partial class PasswordDlg : Form {
        Kernel _kernel;
        public string PasswordStr = ""; 
        public PasswordDlg(Kernel kernel) {
            InitializeComponent();
            _kernel = kernel;

            label1.Text = (kernel.IsJp()) ? "パスワードを入力してください" : "Login password";
            buttonCancel.Text = (kernel.IsJp()) ? "キャンセル" : "Cancel";
        }

        private void ButtonOkClick(object sender, EventArgs e) {
            PasswordStr = textBoxPassword.Text;
        }

        private void TextBoxPasswordKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.Enter) {
                PasswordStr = textBoxPassword.Text;
                DialogResult = DialogResult.OK;
            }
        }

    }
}