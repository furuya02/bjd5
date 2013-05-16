using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Bjd.option;
using Bjd.util;

namespace Bjd.ctrl{
    public class CtrlTabPage : OneCtrl{
        public List<OnePage> PageList { get; private set; }
        //private TabPage _tabPage;
        private TabControl _tabControl;


        private List<TabPage> _pagePanelList;

        public CtrlTabPage(string help, List<OnePage> pageList)
            : base(help){
            PageList = pageList;
        }


        public override CtrlType GetCtrlType(){
            return CtrlType.TabPage;
        }




        protected override void AbstractCreate(object value, ref int tabIndex){

            var left = Margin;
            var top = Margin;

            _tabControl = (TabControl) Create(Panel, new TabControl(), left, top, tabIndex++);
            _tabControl.Height = OptionDlg.Height() - 60 - top;
            _tabControl.Width = OptionDlg.Width();
            _tabControl.SelectedIndex = 0;
            //    Dock = DockStyle.Bottom,

            //_tabPage.setSize(getDlgWidth() - 22, getDlgHeight() - 80 - top);
            //_tabPage.Width = DlgWidth() - 22;
            //_tabPage.Height = DlgHeight() - 80 - top;

            //ページ変更のイベントをトラップする
            //tabbedPane.addChangeListener(this);


            Panel.Controls.Add(_tabControl);

            //グループに含まれるコントロールを描画する(listValはCtrlPageなので座標やサイズはもう関係ない)


            _pagePanelList = new List<TabPage>();

            foreach (var onePage in PageList){
                var tabPage = new TabPage();
                tabPage.Text = onePage.Title;
                //p.setLayout(null); // 絶対位置表示
                //p.setName(onePage.getName());

                onePage.ListVal.CreateCtrl(tabPage, 0, 0, ref tabIndex); //ページの中を作成

                onePage.ListVal.OnChange += new option.ListVal.OnChangeHandler(ListVal_OnChange);

                _tabControl.Controls.Add(tabPage);    
                //_tabPage.addTab(onePage.Title, p);
                _pagePanelList.Add(tabPage);
            }

            // オフセット移動
            left += _tabControl.Width;
            top += _tabControl.Height;

            //値の設定
            //abstractWrite(value);

            // パネルのサイズ設定
            Panel.Size = new Size(left + Margin, top + Margin);
        }

        void ListVal_OnChange() {
            Change(null,null);//[C#] コントロールの変化をイベント処理する
        }


        protected override void AbstractDelete(){
            foreach (var onePage in PageList){
                onePage.ListVal.DeleteCtrl(); //これが無いと、グループの中のコントロールが２回目以降表示されなくなる
            }

            while (_pagePanelList.Count != 0){
                _tabControl.Controls.Remove(_pagePanelList[0]); //タブから削除
                _pagePanelList.RemoveAt(0); // リストから削除
            }
            _pagePanelList = null;

            Remove(Panel, _tabControl);
            _tabControl = null;
        }

        //***********************************************************************
        // コントロールの値の読み書き
        //***********************************************************************
        protected override object AbstractRead(){
            foreach (var onePage in PageList){
                onePage.ListVal.ReadCtrl(false);
            }
            return 0;
        }

        protected override void AbstractWrite(object value){
            //処理なし
        }

        //***********************************************************************
        // コントロールへの有効・無効
        //***********************************************************************
        protected override void AbstractSetEnable(bool enabled){
            //タブページの場合は、disableで非表示とする
            _tabControl.Visible = enabled;
        }

        //***********************************************************************
        // OnChange関連
        //***********************************************************************
        //@Override
        //public void stateChanged(ChangeEvent arg0) {
        //	setOnChange();
        //}

        //***********************************************************************
        // CtrlDat関連
        //***********************************************************************
        protected override bool AbstractIsComplete(){
            return true; //未入力状態はない
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

//JTabbedPane tabbedpane = new JTabbedPane();
//
//JPanel tabPanel1 = new JPanel();
//tabPanel1.add(new JButton("button1"));
//
//JPanel tabPanel2 = new JPanel();
//tabPanel2.add(new JLabel("Name:"));
//tabPanel2.add(new JTextField("", 10));
//
//JPanel tabPanel3 = new JPanel();
//tabPanel3.add(new JButton("button2"));
//
//tabbedpane.addTab("tab1", tabPanel1);
//tabbedpane.addTab("tab2", tabPanel2);
//tabbedpane.addTab("tab3", tabPanel3);