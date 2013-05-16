using System.Drawing;
using System.Windows.Forms;

namespace Bjd.ctrl {
    public class CtrlCheckBox : OneCtrl{
        
        private CheckBox _checkBox;

        public CtrlCheckBox(string help)
            : base(help) {
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.CheckBox;
        }
        protected override void AbstractCreate(object value, ref int tabIndex){
            var left = Margin;
            var top = Margin;

            // チェックボックス作成
            _checkBox = (CheckBox) Create(Panel, new CheckBox(), left, top, tabIndex);
            _checkBox.Text = Help;
            _checkBox.CheckedChanged += Change;//[C#] コントロールの変化をイベント処理する

            left += _checkBox.Width + Margin; // オフセット移動

            //値の設定
            AbstractWrite(value);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, DefaultHeight + Margin*2);
        }

        protected override void AbstractDelete(){
            _checkBox.CheckedChanged -= Change;//[C#] コントロールの変化をイベント処理する
            Remove(Panel, _checkBox);
    		_checkBox = null;
        }
        
        //***********************************************************************
	    // コントロールの値の読み書き
	    //***********************************************************************
        protected override object AbstractRead(){
	    	return _checkBox.Checked;
        }

        protected override void AbstractWrite(object value){
    		_checkBox.Checked = (bool) value;
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            if (_checkBox != null) {
                _checkBox.Enabled = enabled;
            }
        }
        //***********************************************************************
	    // OnChange関連
	    //***********************************************************************
	    //@Override
	    //public void actionPerformed(ActionEvent arg0) {
		//    setOnChange();
	    //}

    	//***********************************************************************
	    // CtrlDat関連
	    //***********************************************************************
        protected override bool AbstractIsComplete(){
    		return true; // チェックの有無は、常にCompleteしている
        }

        protected override string AbstractToText(){
            return _checkBox.Checked.ToString();
        }

        protected override void AbstractFromText(string s){
            _checkBox.Checked = bool.Parse(s);
        }

        protected override void AbstractClear(){
		    _checkBox.Checked = false;
        }
    }
}
