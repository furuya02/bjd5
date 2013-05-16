using System;
using System.Drawing;
using System.Windows.Forms;
using Bjd.net;
using Bjd.util;

namespace Bjd.ctrl{
    public class CtrlAddress : OneCtrl{
        private Label _label;
        private MaskedTextBox[] _textBoxList;
        public CtrlAddress(string help) : base(help){
            
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.AddressV4;
        }

        protected override void AbstractCreate(object value, ref int tabIndex){
            var left = Margin;
            var top = Margin;

            // ラベルの作成 top+3 は後のテキストボックスとの整合のため
            _label = (Label) Create(Panel, new Label(), left, top + 3, -1);
            _label.Text = Help;
            // label.setBorder(new LineBorder(Color.RED, 2, true)); //Debug 赤枠
            left += LabelWidth(_label)+10; // オフセット移動

            // テキストボックスの配置
            _textBoxList = new MaskedTextBox[4];
            for (var i = 0; i < 4; i++){
                _textBoxList[i] = (MaskedTextBox)Create(Panel, new MaskedTextBox(), left, top, tabIndex++);
                _textBoxList[i].Mask = "999";//桁指定
                _textBoxList[i].Width = 40;
                //textBoxList[i].getDocument().addDocumentListener(this);
                left += 5;

                _textBoxList[i].TextChanged += Change;//[C#] コントロールの変化をイベント処理する

                //((AbstractDocument) textBoxList[i].getDocument()).setDocumentFilter(new IntegerDocumentFilter(digits));
                left += _textBoxList[i].Width; // オフセット移動
            }

            //値の設定
            AbstractWrite(value);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, DefaultHeight + Margin);
        }


        protected override void AbstractDelete(){
            Remove(Panel, _label);
            for (var i = 0; i < 4; i++){
                _textBoxList[i].TextChanged -= Change;//[C#] コントロールの変化をイベント処理する
                Remove(Panel, _textBoxList[i]);
                _textBoxList[i] = null;
            }
            _label = null;
            _textBoxList = null;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************
        protected override object AbstractRead(){
            try{
                var ipStr = string.Format("{0}.{1}.{2}.{3}",
                                             Convert.ToInt32(_textBoxList[0].Text),
                                             Convert.ToInt32(_textBoxList[1].Text),
                                             Convert.ToInt32(_textBoxList[2].Text),
                                             Convert.ToInt32(_textBoxList[3].Text));
                return new Ip(ipStr);
            }
            catch (Exception e){
                //ここでの例外は、設計の問題
                Util.RuntimeException(e.Message);
                return null;
            }
        }

        protected override void AbstractWrite(object value){
            var ip = (Ip) value;
            for (var i = 0; i < 4; i++){
                _textBoxList[i].Text = ip.IpV4[i].ToString();
            }
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            for (var i = 0; i < 4; i++){
                if (_textBoxList[i] != null){
                    _textBoxList[i].Enabled = enabled;
                }
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
        //    setOnChange();
        //}

        //***********************************************************************
        // CtrlDat関連
        //***********************************************************************

        protected override bool AbstractIsComplete(){
            for (var i = 0; i < 4; i++){
                if (_textBoxList[i].Text == ""){
                    return false;
                }
            }
            return true;
        }

        protected override string AbstractToText(){
            var ip = (Ip) AbstractRead();
            return ip.ToString();
        }

        protected override void AbstractFromText(string s){
            Ip ip;
            try{
                ip = new Ip(s);
            }catch (ValidObjException){
                ip = new Ip(IpKind.V4_0);
            }
            AbstractWrite(ip);
        }

        protected override void AbstractClear(){
            for (var i = 0; i < 4; i++){
                _textBoxList[i].Text = "0";
            }
        }
    }
}
