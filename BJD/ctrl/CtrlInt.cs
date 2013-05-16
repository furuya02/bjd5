using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Bjd.ctrl {
    public class CtrlInt : OneCtrl{
        private readonly int _digits;
        private Label _label;
        MaskedTextBox _maskedTextBox;
        
        public CtrlInt(String help, int digits):base(help){
            _digits = digits;
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.Int;
        }

    	protected override void AbstractCreate(Object value,ref int tabIndex){

    	    var left = Margin;
    	    var top = Margin;

    	    // ラベルの作成 top+3 は後のテキストボックスとの整合のため
    	    _label = (Label) Create(Panel, new Label(), left, top + 3 , tabIndex++);
    	    _label.Text = Help;

            // label.setBorder(new LineBorder(Color.RED, 2, true)); //Debug 赤枠
            left += LabelWidth(_label) + 10; // オフセット移動

    	    // テキストボックスの配置
            //_maskedTextBox = (MaskedTextBox) Create(Panel, new MaskedTextBox(_digits), left, top);
            //((AbstractDocument) __maskedTextBox.getDocument()).setDocumentFilter(new IntegerDocumentFilter(_digits));
            //__maskedTextBox.getDocument().addDocumentListener(this);

    	    _maskedTextBox = (MaskedTextBox)Create(Panel, new MaskedTextBox(), left, top, tabIndex++);
            _maskedTextBox.TextChanged += Change;//[C#] コントロールの変化をイベント処理する

            //桁指定
    	    _maskedTextBox.Width = _digits*10;
            var sb = new StringBuilder();
            for (var i = 0; i < _digits; i++)
                sb.Append("9");
            _maskedTextBox.Mask = sb.ToString();
            
            left += _maskedTextBox.Width; // オフセット移動

    	    //値の設定
    	    AbstractWrite(value);

    	    // パネルのサイズ設定
    	    //Panel.setSize(left + margin, defaultHeight + margin);
    	    Panel.Width = left + Margin;
            Panel.Height = DefaultHeight + Margin;
    	}


        protected override void AbstractDelete(){
            _maskedTextBox.TextChanged -= Change;//[C#] コントロールの変化をイベント処理する
            Remove(Panel, _label);
    	    Remove(Panel, _maskedTextBox);
    	    _label = null;
    	    _maskedTextBox = null;
    	}
    	//***********************************************************************
	    // コントロールの値の読み書き
	    //***********************************************************************
    	protected override Object AbstractRead(){
    	    String s = _maskedTextBox.Text;
            s = s.Replace(" ", "");
    	    if (s == ""){
    	        s = "0";
    	    }
    	    try{
    	        return Int32.Parse(s);
    	    }catch (Exception){
    	        return 0;
    	    }
    	}

        protected override void AbstractWrite(Object value){
            try {
                _maskedTextBox.Text = ((int)value).ToString();
            } catch {
                _maskedTextBox.Text = "";
            }
        }
    	//***********************************************************************
	    // コントロールへの有効・無効
	    //***********************************************************************
    	protected override void AbstractSetEnable(bool enabled){
    	    if (_maskedTextBox != null){
                _label.Enabled = enabled;
                _maskedTextBox.Enabled = enabled;
    	    }
    	}
    	//***********************************************************************
	    // OnChange関連
	    //***********************************************************************
        //public void changedUpdate(DocumentEvent e) {
        //}

        //public void insertUpdate(DocumentEvent e) {
        //    setOnChange();
        //}

        //public void removeUpdate(DocumentEvent e) {
        //    setOnChange();
        //}
    	//***********************************************************************
	    // CtrlDat関連
	    //***********************************************************************
        protected override bool AbstractIsComplete() {
            if (_maskedTextBox.Text=="") {
                return false;
            }
            return true;
        }

        protected override String AbstractToText() {
            var i = (int)AbstractRead();
            return i.ToString();
        }

        protected override void AbstractFromText(String s) {
            var i = Convert.ToInt32(s);
            _maskedTextBox.Text = i.ToString();

        }

        protected override void AbstractClear() {
            _maskedTextBox.Text = "";
        }
    }
}





