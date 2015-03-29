using System;
using System.Windows.Forms;
using Bjd.util;

namespace Bjd.option {
    public sealed partial class OptionDlg:Form {
        readonly OneOption _oneOption;
        public OptionDlg(Kernel kernel, OneOption oneOption) {
            InitializeComponent();


            //メニューの項目名をダイアログのタイトルにする
            var text = oneOption.MenuStr;

            var index = text.LastIndexOf(',');
            Text = index != 0 ? text.Substring(index+1) : text;
            //(&R)のようなショートカット指定を排除する
            index = Text.IndexOf('(');
            if (0 <= index) {
                Text = Text.Substring(0, index);
            }
            //&を排除する
            Text = Util.SwapChar('&','\b',Text);

            _oneOption = oneOption;
            oneOption.CreateDlg(panelMain);

            buttonCancel.Text = (kernel.IsJp()) ? "キャンセル" : "Cancel";
        }

        //public override sealed string Text{
        //    get { return base.Text; }
        //    set { base.Text = value; }
        //}

        public new static int Width(){
            return 600;
        }
        public new static int Height() {
            return 500;
        }

        //「OK」ボタンが押された場合
        private void ButtonOkClick(object sender,EventArgs e) {
            const bool isComfirm = true; // コントロールのデータが全て正常に読めるかどうかの確認(エラーの場合は、ポップアップ表示)
		    if (!_oneOption.OnOk(isComfirm)) {
			    return;
		    }
		    _oneOption.OnOk(false); //値の読み込み
        }
        //ダイアログボックスがクローズされる場合
        private void OptionDlgFormClosed(object sender,FormClosedEventArgs e) {
            _oneOption.CloseDlg();
        }

    }
}