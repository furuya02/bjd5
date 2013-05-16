using System.Drawing;
using System.Windows.Forms;

namespace Bjd.ctrl {
    public class CtrlLabel : OneCtrl {
        private Label _label;

        public CtrlLabel(string help)
            : base(help) {
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.Label;
        }

        protected override void AbstractCreate(object value, ref int tabIndex) {
            int left = Margin;
            int top = Margin;

            // ラベルの作成 top+3 は後のテキストボックスとの整合のため
            _label = (Label)Create(Panel, new Label(), left, top + 3, tabIndex);
            _label.Text = Help;
            left += LabelWidth(_label) + 10; // オフセット移動

//            //値の設定
//            AbstractWrite(value);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, DefaultHeight + Margin);

        }

        protected override void AbstractDelete() {
            Remove(Panel, _label);
            _label = null;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************
        protected override object AbstractRead() {
            return "";
        }

        protected override void AbstractWrite(object value) {
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled) {
            if (_label != null) {
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
        protected override bool AbstractIsComplete() {
            return true;
        }

        protected override string AbstractToText() {
            return Help;
        }

        protected override void AbstractFromText(string s) {
        }

        protected override void AbstractClear() {
        }
    }
}
