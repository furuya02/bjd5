using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bjd.ctrl{
    public class CtrlMemo : OneCtrl{
        private readonly int _height;
        private readonly int _width;
        private Label _label;
        private TextBox _textBox;

        public CtrlMemo(string help, int width, int height) : base(help){
            _width = width;
            _height = height;
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.Memo;
        }

        protected override void AbstractCreate(object value, ref int tabIndex){
            int left = Margin;
            int top = Margin;

            // ラベルの作成 top+3 は後のテキストボックスとの整合のため
            _label = (Label) Create(Panel, new Label(), left, top + 3, -1);
            _label.Text = Help;
            // label.setBorder(new LineBorder(Color.RED, 2, true)); //Debug 赤枠
            //left += _label.Width + Margin; // オフセット移動

            // テキストエリアの配置
            //textBox = new JTextArea();
            //textBox.getDocument().addDocumentListener(this);
            //scrollPane = (JScrollPane) create(Panel, new JScrollPane(textBox), left, top);
            //scrollPane.setSize(width, height);
            //Panel.add(scrollPane);

            //テキストボックスの配置
            _textBox = (TextBox) Create(Panel, new TextBox(), left, top + 7 + _label.Height, tabIndex++);
            _textBox.Height = _height;
            _textBox.Width = _width;
            _textBox.Multiline = true;
            _textBox.ScrollBars = ScrollBars.Vertical;

            _textBox.TextChanged += Change;//[C#] コントロールの変化をイベント処理する



            // オフセット移動
            left += _textBox.Width;
            top += _height;

            //値の設定
            AbstractWrite(value);
            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, top + Margin);
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
            _textBox.Text = (String) value;
            //textBox.setCaretPosition(0);
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            if (_textBox != null){
                _textBox.Enabled = enabled;
            }
        }

        //***********************************************************************
        // OnChange関連
        //***********************************************************************
        //@Override
        //public void changedUpdate(DocumentEvent arg0) {
        //}
        //@Override
        //public void insertUpdate(DocumentEvent arg0) {
        //    setOnChange();
        //}
        //@Override
        //public void removeUpdate(DocumentEvent arg0) {
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

