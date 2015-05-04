using System;
using System.Drawing;
using System.Windows.Forms;
using Bjd.util;

namespace Bjd.ctrl{
    public class CtrlFont : OneCtrl{
        //private readonly bool _isJp;
        private Lang _lang;
        private Label _label;
        private Button _button;
        private Font _font;
        readonly FontDialog _dlg = new FontDialog();

        //public CtrlFont(string help, bool isJp) : base(help){
        public CtrlFont(string help, LangKind  langKind) : base(help) {
            _lang = new Lang(langKind,"CtrlFont");
            //_isJp = isJp;
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.Font;
        }

        protected override void AbstractCreate(object value, ref int tabIndex){
            int left = Margin;
            int top = Margin;

            // ラベルの作成(topの+3は、後のテキストボックスとの高さ調整)
            if (Help.Length != 0){
                _label = (Label) Create(Panel, new Label(), left, top + 3, -1);
                _label.Text = Help;
                left += _label.Width + Margin; // オフセット移動
            }

            // ボタンの配置(topの-2は、前のテキストボックスとの高さ調整)
            //string buttonText = _isJp ? "フォント" : "Font";
            var buttonText = _lang.Value("buttonText");
            _button = (Button)Create(Panel, new Button(), left, top - 3, tabIndex++);
            _button.Text = buttonText;
            _button.Click += ButtonClick;

            //button.addActionListener(this);

            //TODO CtrlFont ボタンの横にフォントの内容をテキスト表示する

            //		button.addActionListener(new ActionListener() {
            //			@Override
            //			public void actionPerformed(ActionEvent e) {
            //				JFontChooser dlg = new JFontChooser();
            //				if (font != null) {
            //					dlg.setSelectedFont(font);
            //				}
            //				if (JFontChooser.OK_OPTION == dlg.showDialog(Panel)) {
            //					font = dlg.getSelectedFont();
            //					System.out.println("Selected Font : " + font);
            //					
            //					setOnChange();//コントロールの変換
            //				}
            //			}
            //		});

            left += _button.Width + Margin; // オフセット移動

            //値の設定
            AbstractWrite(value);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, DefaultHeight + Margin);
        }

        void ButtonClick(object sender, EventArgs e) {
            //フォントボタンのダイアログ表示
            _dlg.Font = _font;
            if (DialogResult.OK == _dlg.ShowDialog()) {
                _font = _dlg.Font;
                //if (OnClose != null)
                //    OnClose(sender, e);
            }
        }


        protected override void AbstractDelete(){

            Remove(Panel, _label);
            Remove(Panel, _button);
            _label = null;
            _button = null;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************

        protected override object AbstractRead(){
            return _font;
        }

        protected override void AbstractWrite(object value){
            _font = (Font) value;
        }


        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            _button.Enabled = enabled;
        }

        //***********************************************************************
        // OnChange関連
        //***********************************************************************
        //public void actionPerformed(ActionEvent e) {
        //    JFontChooser dlg = new JFontChooser();
        //    if (font != null) {
        //	    dlg.setSelectedFont(font);
        //    }
        //    if (JFontChooser.OK_OPTION == dlg.showDialog(Panel)) {
        //	    font = dlg.getSelectedFont();
        //	    System.out.println("Selected Font : " + font);
        //		setOnChange(); //コントロールの変換
        //	}
        //}

        //***********************************************************************
        // CtrlDat関連
        //***********************************************************************
        protected override bool AbstractIsComplete(){
            return (_font != null);
        }

        protected override string AbstractToText(){
            Util.RuntimeException("未実装");
            return "";
        }

        protected override void AbstractFromText(string s){
            Util.RuntimeException("未実装");
        }

        protected override void AbstractClear(){
            Util.RuntimeException("未実装");
        }
    }
}
