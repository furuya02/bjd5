using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace Bjd.ctrl {
    public class CtrlComboBox : OneCtrl{

        private readonly string[] _list;
        private readonly int _width;
        private Label _label;
        private ComboBox _comboBox;

        public CtrlComboBox(string help, string[] list, int width)
            : base(help){
            _list = list;
            _width = width;
        }

        public int Max{
            get { return _list.Length; }
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.ComboBox;
        }

        protected override void AbstractCreate(object value, ref int tabIndex){
            var left = Margin;
            var top = Margin;

            // ラベルの作成 top+3 は後のテキストボックスとの整合のため
            _label = (Label) Create(Panel, new Label(), left, top + 3, -1);
            _label.Text = Help;
            // label.setBorder(new LineBorder(Color.RED, 2, true)); //Debug 赤枠
            left += LabelWidth(_label) + 10; // オフセット移動

            // コンボボックスの配置
            _comboBox = (ComboBox) Create(Panel, new ComboBox(), left, top, tabIndex++);
            _comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var s in _list){
                _comboBox.Items.Add(s);
                var w = TextWidth(s) + 20;
                if (w > _comboBox.Width){
                    _comboBox.Width = w;
                }
            }
            //comboBox.addActionListener(this);
            _comboBox.SelectedIndexChanged += Change;//[C#] コントロールの変化をイベント処理する

            //comboBox.setSize(width, comboBox.getHeight());
            _comboBox.Width = _width;
            left += _comboBox.Width; // オフセット移動

            //値の設定
            AbstractWrite(value);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, DefaultHeight + Margin);
        }

        protected override void AbstractDelete(){
            _comboBox.SelectedIndexChanged -= Change;//[C#] コントロールの変化をイベント処理する
            Remove(Panel, _label);
            Remove(Panel, _comboBox);
            _label = null;
            _comboBox = null;
        }

        //Ver5.8.4 Ver5.7.xとのデータコンバート
        public int GetNewVal(string oldVal){
            for (int i = 0; i < _list.Count(); i++){
                if (oldVal == _list[i]){
                    return i;
                }
            }
            return -1;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************
        protected override object AbstractRead(){
            return _comboBox.SelectedIndex;
        }

        protected override void AbstractWrite(object value){
            _comboBox.SelectedIndex = (int) value;
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            if (_comboBox != null){
                _comboBox.Enabled = enabled;
            }
        }

        //***********************************************************************
        // OnChange関連
        //***********************************************************************
        //@Override
        //public void actionPerformed(ActionEvent e) {
        //    setOnChange();
        //}


        //***********************************************************************
        // CtrlDat関連
        //***********************************************************************

        protected override bool AbstractIsComplete(){
            return true;
        }

        protected override string AbstractToText(){
            return _comboBox.SelectedIndex.ToString();
        }

        protected override void AbstractFromText(string s){
            int n = Int32.Parse(s);
            _comboBox.SelectedIndex = n;
        }

        protected override void AbstractClear(){
            _comboBox.SelectedIndex = 0;

        }
    }

}


