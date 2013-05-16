using System;
using System.Drawing;
using System.Windows.Forms;
using Bjd.util;

namespace Bjd.ctrl {
    public class CtrlRadio : OneCtrl{
        private readonly int _width;
        private readonly int _colMax;
        private readonly String[] _list;

        private GroupBox _groupBox;
        private RadioButton[] _radioButtonList;

        public CtrlRadio(string help, string[] list, int width, int colMax)
            : base(help){
            _list = list;
            _width = width;
            _colMax = colMax;
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.Radio;
        }


        protected override void AbstractCreate(object value, ref int tabIndex){
            // 行数及び１項目の横幅の計算
            // 行数
            var rowMax = _list.Length/_colMax;
            if (_list.Length%_colMax != 0){
                rowMax++;
            }
            // 1項目ごとの横幅
            var spanWidth = _width/_colMax;

            var left = Margin;
            var top = Margin;
            var height = DefaultHeight*rowMax + 20;

            // ラジオボタンを囲むボーダライン（groupPanel）の生成
            _groupBox = (GroupBox)Create(Panel, new GroupBox(),left, top + 3,tabIndex++);
            _groupBox.AutoSize = false;
            _groupBox.Text = Help;
            _groupBox.Width = _width;
            _groupBox.Height = height;

            //_groupPanel = (Panel) Create(Panel, new Panel(new GridLayout(0, 1)), left, top);
            //_groupPanel.setBorder(BorderFactory.createTitledBorder(getHelp()));
            //groupPanel.setSize(width, height);
            //_groupPanel.Width = _width;
            //_groupPanel.Height = height;

            // ラジオボタンの生成
            _radioButtonList = new RadioButton[_list.Length];
            // groupPanelの中のオフセット
            var l = 10;
            var t = 18;
            //ButtonGroup buttonGroup = new ButtonGroup(); // ボタンのグループ化
            for (var i = 0; i < _list.Length; i++){
                if (i%_colMax == 0){
                    l = 10;
                    if (i != 0){
                        t += DefaultHeight;
                    }
                }
                _radioButtonList[i] = (RadioButton) Create(_groupBox, new RadioButton(), l, t,tabIndex++);
                _radioButtonList[i].Text = _list[i];
                //radioButtonList[i].addActionListener(this);
                _radioButtonList[i].CheckedChanged += Change;//[C#] コントロールの変化をイベント処理する
                

                l += spanWidth;
                //_groupBox.Add(_radioButtonList[i]);
            }
            left += _groupBox.Width; // オフセット移動

            //値の設定
            AbstractWrite(value);
            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, height + Margin*2);
        }

        protected override void AbstractDelete(){
            for (var i = 0; i < _radioButtonList.Length; i++){
                _radioButtonList[i].CheckedChanged -= Change;//[C#] コントロールの変化をイベント処理する
                Remove(Panel, _radioButtonList[i]);
                _radioButtonList[i] = null;

            }
            _radioButtonList = null;
            Remove(Panel, _groupBox);
            _groupBox = null;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************
        protected override object AbstractRead(){
            for (var i = 0; i < _list.Length; i++){
                if (_radioButtonList[i].Checked){
                    return i;
                }
            }
            Msg.Show(MsgKind.Error, "選択されているラジオボタンがありません");
            return 0;
        }

        protected override void AbstractWrite(object value){
            _radioButtonList[(int) value].Checked = true;
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************

        protected override void AbstractSetEnable(bool enabled){
            foreach (var t in _radioButtonList){
                if (t != null){
                    t.Enabled = enabled;
                }
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
            for (var i = 0; i < _radioButtonList.Length; i++){
                if (_radioButtonList[i].Checked){
                    return i.ToString();
                }

            }
            Util.RuntimeException("radioButtunに選択がない");
            return "";
        }

        protected override void AbstractFromText(string s){
            var n = Int32.Parse(s);
            if (0 < n && n <= _radioButtonList.Length){
                _radioButtonList[n].Checked = true;
            }else{
                Util.RuntimeException(string.Format("n={0} radioButtonList.length={1} ", n, _radioButtonList.Length));
            }
        }

        protected override void AbstractClear(){
            _radioButtonList[0].Checked = true;
        }
    }
}

/*
	

	


	
}*/
