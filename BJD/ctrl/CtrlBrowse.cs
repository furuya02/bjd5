using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bjd.ctrl {
    public abstract class CtrlBrowse : OneCtrl{
        private Label _label;
        private TextBox _textBox;
        private Button _button;
        private readonly int _digits;
        private readonly bool _isJp;

        private readonly Kernel _kernel;
        private readonly bool _editBrowse;
        private readonly RunMode _runMode;

        protected CtrlBrowse(string help, int digits, Kernel kernel) : base(help){
            _digits = digits;
            _kernel = kernel;
            _isJp = _kernel.IsJp();
            _runMode = _kernel.RunMode;
            _editBrowse = _kernel.EditBrowse;
        }


        protected override void AbstractCreate(object value, ref int tabIndex){
            int left = Margin;
            int top = Margin;

            // ラベルの作成(topの+3は、後のテキストボックスとの高さ調整)
            _label = (Label) Create(Panel, new Label(), left, top + 3, -1);
            _label.Text = Help;
            left += LabelWidth(_label)+ 10; // オフセット移動

            // テキストボックスの配置
            _textBox = (TextBox) Create(Panel, new TextBox(), left, top, tabIndex++);
            _textBox.Width = _digits*6;
            _textBox.TextChanged += Change;//[C#] コントロールの変化をイベント処理する

            //		textBox.getDocument().addDocumentListener(this);
            _textBox.ReadOnly = !(_editBrowse);// 読み取り専用

            left += _textBox.Width + Margin; // オフセット移動

            // ボタンの配置(topの-2は、前のテキストボックスとの高さ調整)
            string buttonText = _isJp ? "参照" : "Browse";
            _button = (Button) Create(Panel, new Button(), left, top - 3, tabIndex++);
            _button.Text = buttonText;
            _button.Click += _button_Click;

            left += _button.Width + Margin; // オフセット移動

            //値の設定
            AbstractWrite(value);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, DefaultHeight + Margin);
        }

        void _button_Click(object sender, EventArgs e) {
            if (_runMode == RunMode.Remote) {
                //Java fix
                string resultStr = _kernel.RemoteClient.ShowBrowseDlg(GetCtrlType());
                if (resultStr != null) {
                    _textBox.Text = resultStr;
                }
                return;
            }
            //フォルダのダイアログ表示
            if (GetCtrlType() == CtrlType.Folder) {
                var dlg = new FolderBrowserDialog{SelectedPath = _textBox.Text};
                if (DialogResult.OK == dlg.ShowDialog()) {
                    _textBox.Text = dlg.SelectedPath;
                }
            } else {
                var dlg = new OpenFileDialog{FileName = _textBox.Text};
                again:
                try {
                    if (DialogResult.OK == dlg.ShowDialog()) {
                        _textBox.Text = dlg.FileName;
                    }
                } catch {
                    if (dlg.FileName != "") {
                        dlg.FileName = "";
                        goto again;
                    }
                }
            }
        }


        protected override void AbstractDelete(){
            _textBox.TextChanged -= Change;//[C#] コントロールの変化をイベント処理する
            Remove(Panel, _label);
            Remove(Panel, _textBox);
            Remove(Panel, _button);
            _label = null;
            _textBox = null;
            _button = null;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************
        protected override object AbstractRead(){
            return _textBox.Text;
        }

        protected override void AbstractWrite(object value){
            _textBox.Text = (string) value;
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            if (_textBox != null){
                _textBox.Enabled = enabled;
            }
            if (_button != null){
                _button.Enabled = enabled;
            }
        }

        //***********************************************************************
        // OnChange関連
        //***********************************************************************
        //@Override
        //public final void changedUpdate(DocumentEvent e) {
        //}
        //@Override
        //public final void insertUpdate(DocumentEvent e) {
        //	setOnChange();
        //}
        //@Override
        //public final void removeUpdate(DocumentEvent e) {
        //    setOnChange();
        //}

        //***********************************************************************
        // CtrlDat関連
        //***********************************************************************
        protected override bool AbstractIsComplete(){
            if (_textBox.Text == ""){
                return false;
            }
            return true;
        }

        protected override string AbstractToText(){
            return _textBox.Text;
        }

        protected override void AbstractFromText(string s){
            _textBox.Text = s;
        }

        protected override void AbstractClear(){
            _textBox.Text = "";
        }
    }
}