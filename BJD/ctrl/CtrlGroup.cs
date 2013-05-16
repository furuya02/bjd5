using System.Drawing;
using System.Windows.Forms;
using Bjd.option;
using Bjd.util;

namespace Bjd.ctrl {
    public class CtrlGroup : OneCtrl{

        private GroupBox _border;
        public ListVal ListVal { get; private set; }


        public CtrlGroup(string help, ListVal listVal)
            : base(help){
            ListVal = listVal;
        }



        public override CtrlType GetCtrlType(){
            return CtrlType.Group;
        }

        protected override void AbstractCreate(object value, ref int tabIndex){
            int left = Margin;
            int top = Margin;

            // ボーダライン（groupPanel）の生成
            _border = (GroupBox) Create(Panel, new GroupBox(), left, top, -1);
            //border.setBorder(BorderFactory.createTitledBorder(getHelp()));
            _border.Text = Help;
            _border.AutoSize = false;

            //グループに含まれるコントロールを描画する
            var x = left + 8;
            var y = top + 12;
            ListVal.CreateCtrl(_border, x, y, ref tabIndex);
            var dimension = ListVal.Size;

            // borderのサイズ指定
            _border.Size = new Size(OptionDlg.Width() - 15, dimension.Height +18); // 横はダイアログ幅、縦は、含まれるコントロールで決まる

            // オフセット移動
            left += _border.Width;
            top += _border.Height;

            //値の設定
            AbstractWrite(value);

            // パネルのサイズ設定
            //Panel.setSize(left + width +Margin, top + height +Margin * 2);
            Panel.Size = new Size(left + Margin, top + Margin);
        }

        protected override void AbstractDelete(){
            ListVal.DeleteCtrl(); //これが無いと、グループの中のコントロールが２回目以降表示されなくなる

            Remove(Panel, _border);
            _border = null;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************
        protected override object AbstractRead(){
            ListVal.ReadCtrl(false);
            return 0; //nullを返すと無効値になってしまうのでダミー値(0)を返す
        }

        protected override void AbstractWrite(object value){
        }


        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            if (_border != null){
                //CtrlGroupの場合は、disableで非表示にする
                Panel.Enabled = enabled;
                //border.setEnabled(enabled);
            }
        }

        //***********************************************************************
        // OnChange関連
        //***********************************************************************
        // 必要なし

        //***********************************************************************
        // CtrlDat関連
        //***********************************************************************
        protected override bool AbstractIsComplete(){
            return true;
        }

        protected override string AbstractToText(){
            Util.RuntimeException("使用禁止");
            return "";
        }

        protected override void AbstractFromText(string s){
            Util.RuntimeException("使用禁止");
        }

        protected override void AbstractClear(){
        }
    }
}
