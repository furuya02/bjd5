using System;
using System.Drawing;
using System.Windows.Forms;
using Bjd.net;
using Bjd.util;

namespace Bjd.ctrl{
    public class CtrlBindAddr : OneCtrl{
        private Label _label;
        private readonly RadioButton[] _radioButtonList = new RadioButton[3];
        private readonly Label[] _labelList = new Label[2];
        private readonly ComboBox[] _comboBoxList = new ComboBox[2];
        private readonly Ip[] _listV4;
        private readonly Ip[] _listV6;

        public CtrlBindAddr(string help, Ip[] listV4, Ip[] listV6) : base(help){
            _listV4 = listV4;
            _listV6 = listV6;

        }

        public override CtrlType GetCtrlType(){
            return CtrlType.BindAddr;
        }

        protected override void AbstractCreate(object value, ref int tabIndex){

            var left = Margin;
            var top = Margin;

            // ラベルの作成 top+3 は後のテキストボックスとの整合のため
            _label = (Label) Create(Panel, new Label(), left, top + 3, -1);
            _label.Text = Help;
            // label.setBorder(new LineBorder(Color.RED, 2, true)); //Debug 赤枠
            left += LabelWidth(_label) + 20; // オフセット移動

            //ラジオボタンの配置
            //ButtonGroup buttonGroup = new ButtonGroup(); // ボタンのグループ化
            var protoList = new[]{"IPv4", "IPv6", "Dual"};
            for (var i = 0; i < 3; i++){
                _radioButtonList[i] = (RadioButton) Create(Panel, new RadioButton(), left + i*55, top, tabIndex++);
                _radioButtonList[i].Text = protoList[i];
                _radioButtonList[i].Click += RadioButtonCheckedChanged;
                //radioButtonList[i].addActionListener(this);
                //buttonGroup.Add(_radioButtonList[i]);
               
            }
            _radioButtonList[0].Checked = true;
            top += DefaultHeight + 2; // オフセット移動

            //ComBox配置
            var labelStr = new[]{"IPv4", "IPv6"};
            for (var i = 0; i < 2; i++){
                left = Margin;
                _labelList[i] = (Label) Create(Panel, new Label(), left, top, -1);
                _labelList[i].Text = labelStr[i];
                left += _labelList[i].Width + Margin; // オフセット移動

                // コンボボックスの配置
                _comboBoxList[i] = (ComboBox) Create(Panel, new ComboBox(), left, top, tabIndex++);
                var w = 80;
                foreach (var ip in (i == 0) ? _listV4 : _listV6){
                    var ipStr = ip.ToString();
                    _comboBoxList[i].Items.Add(ipStr);
                    if ((ipStr.Length*12) > w){
                        w = ipStr.Length*12;
                    }
                }
                _comboBoxList[i].Size = new Size(w, _comboBoxList[i].Height);
                //comboBoxList[i].addActionListener(this);

                top += DefaultHeight + 2;
            }
            // オフセット移動
            left += _labelList[1].Width + _comboBoxList[1].Width;
            if (left < 330){
                left = 330; //最低でもRadioButtnで330は必要
            }

            //値の設定
            AbstractWrite(value);

            RadioButtonCheckedChanged(null,null);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, top + Margin);
        }



        protected override void AbstractDelete(){
            Remove(Panel, _label);
            _label = null;
            for (var i = 0; i < 3; i++){
                Remove(Panel, _radioButtonList[i]);
                _radioButtonList[i] = null;
            }
            for (var i = 0; i < 2; i++){
                Remove(Panel, _comboBoxList[i]);
                _comboBoxList[i] = null;
                Remove(Panel, _labelList[i]);
                _labelList[i] = null;
            }
        }

        //無効なオプションの表示
        private void SetDisable(){
            if (_comboBoxList[0].Items.Count == 1){
                //IPv4無効
                _radioButtonList[0].Enabled = false;
                _radioButtonList[2].Enabled = false;
                _comboBoxList[0].Enabled = false;
                _comboBoxList[0].SelectedIndex = 0;
            }
            if (_comboBoxList[1].Items.Count == 1){
                //IPv6無効
                _radioButtonList[1].Enabled = false;
                _radioButtonList[2].Enabled = false;
                _comboBoxList[1].Enabled = false;
                _comboBoxList[1].SelectedIndex = 0;
            }
        }

        private void RadioButtonCheckedChanged(object sender, EventArgs e) {
            _comboBoxList[0].Enabled = true;
            _comboBoxList[1].Enabled = true;

            if (_radioButtonList[0].Checked){
                //IpV4 only
                _comboBoxList[1].Enabled = false;
            }
            else if (_radioButtonList[1].Checked){
                //IpV6 only
                _comboBoxList[0].Enabled = false;
            }
            SetDisable(); //無効なオプションの表示
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************

        protected override object AbstractRead(){
            var byndStyle = BindStyle.V46Dual;
            if (_radioButtonList[0].Checked){
                byndStyle = BindStyle.V4Only;
            }else if (_radioButtonList[1].Checked){
                byndStyle = BindStyle.V6Only;
            }
            var ipV4 = _listV4[_comboBoxList[0].SelectedIndex];
            var ipV6 = _listV6[_comboBoxList[1].SelectedIndex];

            return new BindAddr(byndStyle, ipV4, ipV6);
        }

        protected override void AbstractWrite(object value){
            if (value == null){
                return;
            }
            var bindAddr = (BindAddr) value;
            switch (bindAddr.BindStyle){
                case BindStyle.V4Only:
                    _radioButtonList[0].Checked = true;
                    break;
                case BindStyle.V6Only:
                    _radioButtonList[1].Checked = true;
                    break;
                case BindStyle.V46Dual:
                    _radioButtonList[2].Checked = true;
                    break;
                default:
                    Util.RuntimeException(string.Format("bindAddr.getBindStyle()={0}", bindAddr.BindStyle));
                    break;
            }
            for (var i = 0; i < 2; i++){
                var list = (i == 0) ? _listV4 : _listV6;
                var ip = (i == 0) ? bindAddr.IpV4 : bindAddr.IpV6;
                var index = -1;
                for (var n = 0; n < list.Length; n++){
                    if (list[n] == ip){
                        index = n;
                        break;
                    }
                }
                if (index == -1){
                    index = 0;
                }
                _comboBoxList[i].SelectedIndex = index;
            }
            SetDisable(); //無効なオプションの表示
        }


        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            for (var i = 0; i < 3; i++){
                _radioButtonList[i].Enabled = enabled;
            }
            for (var i = 0; i < 2; i++){
                _comboBoxList[i].Enabled = enabled;
            }
            SetDisable(); //無効なオプションの表示
        }

        //***********************************************************************
        // OnChange関連
        //***********************************************************************
        //@Override
        //public void actionPerformed(ActionEvent e) {
        //    setOnChange();
        //    radioButtonCheckedChanged(); //ラジオボタンの変化によってコントロールの有効無効を設定する
        //}

        //***********************************************************************
        // CtrlDat関連
        //***********************************************************************
        protected override bool AbstractIsComplete(){
            //未設定状態は存在しない
            return true;
        }

        protected override string AbstractToText(){
            Util.RuntimeException("未実装");
            return "";
        }

        protected override void AbstractFromText(string s){
            Util.RuntimeException("未実装");
        }

        protected override void AbstractClear(){
            _radioButtonList[0].Checked = true;
            _comboBoxList[0].SelectedIndex = 0;
            _comboBoxList[1].SelectedIndex = 0;


        }
    }
}
