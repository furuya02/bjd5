using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bjd.ctrl{
    public class CtrlTextBox : OneCtrl{
        private readonly int _digits;
        private Label _label;
        private TextBox _textBox;

        public CtrlTextBox(string help, int digits)
            : base(help){
            _digits = digits;
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.TextBox;
        }

        protected override void AbstractCreate(object value, ref int tabIndex){
            int left = Margin;
            int top = Margin;

            // ラベルの作成 top+3 は後のテキストボックスとの整合のため
            _label = (Label) Create(Panel, new Label(), left, top + 3, tabIndex);
            _label.Text = Help;
            left += LabelWidth(_label) + 10; // オフセット移動

            // テキストボックスの配置
            _textBox = (TextBox) Create(Panel, new TextBox(), left, top, tabIndex++);
            _textBox.Width = _digits*6;
            _textBox.TextChanged += Change;//[C#] コントロールの変化をイベント処理する

            //_textBox.getDocument().addDocumentListener(this);
            left += _textBox.Width; // オフセット移動


            //値の設定
            AbstractWrite(value);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, DefaultHeight + Margin);

        }

        protected override void AbstractDelete(){
            _textBox.TextChanged -= Change;//[C#] コントロールの変化をイベント処理する
            Remove(Panel, _label);
            Remove(Panel, _textBox);
            _label = null;
            _textBox = null;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************
        protected override object AbstractRead(){
            return _textBox.Text;
        }

        protected override void AbstractWrite(object value){
            if (value != null){
                _textBox.Text = (String) value;
            }
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            if (_textBox != null){
                _textBox.Enabled = enabled;
                _label.Enabled = enabled;
            }
        }

        //***********************************************************************
        // OnChange関連
        //***********************************************************************
        //@Override
        //public void changedUpdate(DocumentEvent e) {
        //}
        //@Override
        //public void insertUpdate(DocumentEvent e) {
        //    setOnChange();
        //}
        //@Override
        //public void removeUpdate(DocumentEvent e) {
        //    insertUpdate(e);
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
