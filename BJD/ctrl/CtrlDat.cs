using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Bjd.option;
using Bjd.util;

namespace Bjd.ctrl{
    public class CtrlDat : OneCtrl{
        private GroupBox _border;
        private List<Button> _buttonList;
        
//        private CheckedListBox _checkedListBox;
        private ListView _listView;
        
        //Ver6.0.0
        private readonly Sorter _sorter = new Sorter();

        private readonly string[] _tagList = new[]{"Add", "Edit", "Del", "Import", "Export", "Clear"};
        //private readonly string[] _strList = new[]{"追加", "変更", "削除", "インポート", "エクスポート", "クリア"};

        private readonly ListVal _listVal;

        private readonly int _height;
        //private Kernel kernel;
        //private readonly bool _isJp;
        private Lang _lang;
        private const int Add = 0;
        private const int Edit = 1;
        private const int Del = 2;
        private const int Import = 3;
        public const int Export = 4;
        private const int CLEAR = 5;

        //public CtrlDat(string help, ListVal listVal, int height, bool isJp) : base(help){
        public CtrlDat(string help, ListVal listVal, int height, LangKind langKind) : base(help){
            _listVal = listVal;
            _height = height;
            //_isJp = isJp;
            _lang = new Lang(langKind,"CtrlDat");
        }

        public CtrlType[] CtrlTypeList{
            get{
                var ctrlTypeList = new CtrlType[_listVal.Count];
                int i = 0;
                foreach (var o in _listVal){
                    ctrlTypeList[i++] = o.OneCtrl.GetCtrlType();
                }
                return ctrlTypeList;
            }
        }

        //OnePage(CtrlTabPage.pageList) CtrlGroup CtrlDatにのみ存在する
        public ListVal ListVal{
            get { return _listVal; }
        }

        public override CtrlType GetCtrlType(){
            return CtrlType.Dat;
        }


        protected override void AbstractCreate(object value, ref int tabIndex){
            var left = Margin;
            var top = Margin;

            // ボーダライン（groupPanel）の生成
            _border = (GroupBox) Create(Panel, new GroupBox(), left, top, -1);
            _border.Width = OptionDlg.Width() - 15;
            _border.AutoSize = false;
            _border.Height = _height;
            _border.Text = Help;

            //border = (JPanel) create(Panel, new JPanel(new GridLayout()), left, top);
            //border.setBorder(BorderFactory.createTitledBorder(getHelp()));
            //border.setSize(getDlgWidth() - 32, height); // サイズは、コンストラクタで指定されている

            //Datに含まれるコントロールを配置

            //ボーダーの中でのオフセット移動
            left += 8;
            top += 12;
            _listVal.CreateCtrl(_border, left, top, ref tabIndex);
            _listVal.OnChange += ListValOnChange;
            //listVal.SetListener(this); //コントロール変化のイベントをこのクラスで受信してボタンの初期化に利用する

            //オフセット移動
            var dimension = _listVal.Size;
            top += dimension.Height;

            //ボタンの生成s
            //private readonly string[] _tagList = new[]{"Add", "Edit", "Del", "Import", "Export", "Clear"};
            //private readonly string[] _strList = new[]{"追加", "変更", "削除", "インポート", "エクスポート", "クリア"};
            _buttonList = new List<Button>();
            for (int i = 0; i < _tagList.Count(); i++){
                var b = (Button) Create(_border, new Button(), left + 85*i, top, tabIndex++);
                b.Width = 80;
                b.Height = 24;
                b.Tag = i; //インデックス
                //b.Text = (_isJp) ? _strList[i] : _tagList[i];
                var key = string.Format("button{0}", i);
                b.Text = _lang.Value(key);
                b.Click += ButtonClick;
                b.Tag = _tagList[i]; //[C#]
                _buttonList.Add(b);
            }


            //オフセット移動
            top += _buttonList[0].Height + Margin;

            //チェックリストボックス配置
//            _checkedListBox = (CheckedListBox) Create(_border, new CheckedListBox(), left, top, tabIndex++);
//            _checkedListBox.AutoSize = false;
//            _checkedListBox.Width = OptionDlg.Width() - 35; // 52;
//            _checkedListBox.Height = _height - top -3; // 15;
//            _checkedListBox.SelectedIndexChanged += CheckedListBoxSelectedIndexChanged;

            _listView = (ListView)Create(_border, new ListView(), left, top, tabIndex++);
            _listView.AutoSize = false;
            _listView.Width = OptionDlg.Width() - 35; // 52;
            _listView.Height = _height - top - 3; // 15;
            _listView.SelectedIndexChanged += CheckedListBoxSelectedIndexChanged;
            _listView.CheckBoxes = true;
            _listView.View = System.Windows.Forms.View.Details;
            _listView.FullRowSelect = true;
            _listView.HideSelection = false;

            //Ver6.0.0
            _listView.ListViewItemSorter = _sorter;
            _listView.ColumnClick += _listView_ColumnClick;

            foreach (var a in ListVal){
                var title = a.OneCtrl.Help;
                var colHeader = _listView.Columns.Add(title);
                var width = GetTextWidth(title);
                if (colHeader.Width < width){
                    colHeader.Width = width;
                }
            }

            //値の設定
            AbstractWrite(value);

            // パネルのサイズ設定
            //Panel.Size = new Size(_border.Width + Margin*2, _border.Height + Margin*2);
            Panel.Size = new Size(_border.Width + Margin*2, _border.Height);

            ListValOnChange(); //ボタン状態の初期化
        }


        //コントロールの入力内容に変化があった場合
        public virtual void ListValOnChange(){

            ButtonsInitialise(); //ボタン状態の初期化

        }

        //ボタン状態の初期化
        private void ButtonsInitialise(){
            //コントロールの入力が完了しているか
            var isComplete = IsComplete();
            //チェックリストボックスのデータ件数
            var count = _listView.Items.Count;
            //チェックリストボックスの選択行
            //int index = _listView.SelectedIndex;
            var index = -1;
            if (_listView.SelectedItems.Count > 0) {
                index = _listView.SelectedItems[0].Index;
            }

            _buttonList[Add].Enabled = isComplete;
            _buttonList[Export].Enabled = (count > 0);
            _buttonList[CLEAR].Enabled = (count > 0);
            _buttonList[Del].Enabled = (index >= 0);
            _buttonList[Edit].Enabled = (index >= 0 && isComplete);
        }


        //ボタンのイベント
        private void ButtonClick(object sender, EventArgs e){
            var cmd = (string) ((Button) sender).Tag;

            //var selectedIndex = _checkedListBox.SelectedIndex; // 選択行
            var selectedIndex = -1;
            if (_listView.SelectedItems.Count > 0) {
                selectedIndex = _listView.SelectedItems[0].Index;
            }

            if (cmd == _tagList[Add]){
                //コントロールの内容をテキストに変更したもの
                var s = ControlToText();
                if (s == ""){
                    return;
                }
                //同一のデータがあるかどうかを確認する
                //if (_checkedListBox.Items.IndexOf(s) != -1){
                if (ListViewItemIndexOf(s) != -1){
                    //Msg.Show(MsgKind.Error, _isJp ? "既に同一内容のデータが存在します。" : "There is already the same data");
                    Msg.Show(MsgKind.Error, _lang.Value("Message001"));
                    return;
                }
                //チェックリストボックスへの追加
                //int index = _checkedListBox.Items.Add(s);
                int index = ListViewItemAdd(s);
                _listView.Items[index].Checked = true;//最初にチェック（有効）状態にする
                _listView.Items[index].Selected = true;//選択状態にする
            }
            else if (cmd == _tagList[Edit]){
                //コントロールの内容をテキストに変更したもの
                string str = ControlToText();
                if (str == ""){
                    return;
                }
                //if (str == (string) _checkedListBox.Items[selectedIndex]){
                if (str == ListViewItemToString(selectedIndex)){
                    //Msg.Show(MsgKind.Error, _isJp ? "変更内容はありません" : "There is not a change");
                    Msg.Show(MsgKind.Error, _lang.Value("Message002"));
                    return;
                }
                //同一のデータがあるかどうかを確認する
                //if (_checkedListBox.Items.IndexOf(str) != -1){
                if (ListViewItemIndexOf(str) != -1){
                    //Msg.Show(MsgKind.Error, _isJp ? "既に同一内容のデータが存在します" : "There is already the same data");
                    Msg.Show(MsgKind.Error, _lang.Value("Message001"));
                    return;
                }
                //_checkedListBox.Items[selectedIndex] = str;
                ListViewItemEdit(selectedIndex, str);

            }
            else if (cmd == _tagList[Del]){
                foreach (var v in _listVal){
                    //コントロールの内容をクリア
                    v.OneCtrl.Clear();
                }
                if (selectedIndex >= 0){
                    _listView.Items.RemoveAt(selectedIndex);
                }
            }
            else if (cmd == _tagList[Import]){
                var d = new OpenFileDialog();
                if (DialogResult.OK == d.ShowDialog()){
                    var lines = File.ReadAllLines(d.FileName);
                    ImportDat(lines.ToList());
                }
//                    catch (IOException e){
//                        Msg.Show(MsgKind.Error, string.format("ファイルの読み込みに失敗しました[%s]", file.getPath()));
//                    }
            }
            else if (cmd == _tagList[Export]){
                var dlg = new SaveFileDialog();
                if (DialogResult.OK == dlg.ShowDialog()){
                    var isExecute = true;
                    if (File.Exists(dlg.FileName)){
                        //if (DialogResult.OK != Msg.Show(MsgKind.Question, _isJp ? "上書きして宜しいですか?" : "May I overwrite?")){
                        if (DialogResult.OK != Msg.Show(MsgKind.Question,_lang.Value("Message006"))){
                            isExecute = false; //キャンセル
                        }
                    }
                    if (isExecute){
                        var lines = ExportDat();
                        File.WriteAllLines(dlg.FileName,lines.ToArray());
                    }
                }
            }
            else if (cmd == _tagList[CLEAR]){
                if (DialogResult.OK ==
                    //Msg.Show(MsgKind.Question, _isJp ? "すべてのデータを削除してよろしいですか" : "May I eliminate all data?")){
                    Msg.Show(MsgKind.Question, _lang.Value("Message007"))){
                    _listView.Items.Clear();
                }
                foreach (OneVal v in _listVal){
                    //コントロールの内容をクリア
                    v.OneCtrl.Clear();
                }
            }
        }

        //チェックボックス用のテキストを入力コントロールに戻す
        private void TextToControl(string str){
            var tmp = str.Split('\t');
            if (_listVal.Count != tmp.Length){
                //Msg.Show(MsgKind.Error, (_isJp) ? "項目数が一致しません" : "The number of column does not agree");
                Msg.Show(MsgKind.Error,_lang.Value("Message004"));
                return;
            }
            var i = 0;
            foreach (var v in _listVal){
                v.OneCtrl.FromText(tmp[i++]);
            }
        }

        //入力コントロールの内容をチェックボックス用のテキストに変換する
        private string ControlToText(){

            var sb = new StringBuilder();
            foreach (var v in _listVal){
                if (sb.Length != 0){
                    sb.Append("\t");
                }
                sb.Append(v.OneCtrl.ToText());
            }
            return sb.ToString();

        }

        //インポート
        private void ImportDat(List<string> lines){
            foreach (var s in lines){
                var str = s;
                var isChecked = str[0] != '#';
                str = str.Substring(2);

                //カラム数の確認
                string[] tmp = str.Split('\t');
                if (_listVal.Count != tmp.Length){
                    Msg.Show(MsgKind.Error,
                             string.Format("{0} [ {1} ] ",
                                           //_isJp
                                           //    ? "カラム数が一致しません。この行はインポートできません。"
                                           //    : "The number of column does not agree and cannot import this line.", str));
                                           //      string.Format("{0} [ {1} ] ",
                                           _lang.Value("Message003"), str));

                    continue;
                }
                //Ver5.0.0-a9 パスワード等で暗号化されていない（平文の）場合は、ここで
                bool isChange = false;
                if (isChange){
                    var sb = new StringBuilder();
                    foreach (string l in tmp){
                        if (sb.Length != 0){
                            sb.Append('\t');
                        }
                        sb.Append(l);
                    }
                    str = sb.ToString();
                }
                //同一のデータがあるかどうかを確認する
                //if (_checkedListBox.Items.IndexOf(str) != -1){
                if (ListViewItemIndexOf(str) != -1) {
                    Msg.Show(MsgKind.Error,
                        string.Format("{0} [ {1} ] ",
                            //_isJp
                            //    ? "データ重複があります。この行はインポートできません。"
                            //    : "There is data repetition and cannot import this line.", str));
                            _lang.Value("Message005"), str));
                    continue;
                }

                //int index = _checkedListBox.Items.Add(str);
                int index = ListViewItemAdd(str);

                //最初にチェック（有効）状態にする
                _listView.Items[index].Checked = isChecked;
                _listView.Items[index].Selected = true;
            }
        }


        //エクスポート
        private List<string> ExportDat(){
            //チェックリストボックスの内容からDatオブジェクトを生成する
            var lines = new List<String>();
            for (var i = 0; i < _listView.Items.Count; i++){
                //var s = (string) _checkedListBox.Items[i];
                var s = ListViewItemToString(i);

                //lines.Add(_checkedListBox.GetItemCheckState(i) == CheckState.Checked ? string.Format(" \t{0}", s) : string.Format("#\t{0}", s));
                lines.Add(_listView.Items[i].Checked ? string.Format(" \t{0}", s) : string.Format("#\t{0}", s));
            }
            return lines;
        }

        void CheckedListBoxSelectedIndexChanged(object sender, EventArgs e){
            //int index = _checkedListBox.SelectedIndex;
            var index = -1;
            if (_listView.SelectedItems.Count > 0){
                index = _listView.SelectedItems[0].Index;
            }


             ButtonsInitialise(); //ボタン状態の初期化
             //チェックリストの内容をコントロールに転送する
            if (index >= 0) {
                //TextToControl((String)_checkedListBox.Items[index]);
                TextToControl(ListViewItemToString(index));
            }
        }

        protected override void AbstractDelete(){
	        _listVal.DeleteCtrl(); //これが無いと、グループの中のコントロールが２回目以降表示されなくなる

	        if (_buttonList != null){
	            for (var i = 0; i < _buttonList.Count; i++){
	                Remove(_border, _buttonList[i]);
	                _buttonList[i] = null;
	            }
	        }
	        Remove(Panel, _border);
	        Remove(Panel, _listView);
	        _border = null;
	    }

	    //コントロールの入力が完了しているか
        protected new virtual bool IsComplete() {
	    	return _listVal.IsComplete();
    	}

        //***********************************************************************
	    // コントロールの値の読み書き
	    //***********************************************************************
        protected override object AbstractRead(){
            var dat = new Dat(CtrlTypeList);
            //チェックリストボックスの内容からDatオブジェクトを生成する
            for (int i = 0; i < _listView.Items.Count; i++){
                //bool enable = _checkedListBox.GetItemChecked(i);
                bool enable = _listView.Items[i].Checked;
                //if (!dat.Add(enable, _checkedListBox.Items[i].ToString())) {
                if (!dat.Add(enable, ListViewItemToString(i))) {
                    Util.RuntimeException("CtrlDat abstractRead() 外部入力からの初期化ではないので、このエラーは発生しないはず");
                }
            }
            return dat;
        }

        protected override void AbstractWrite(object value){
            if (value == null){
                return;
            }
            var dat = (Dat) value;
            foreach (var d in dat){
                var sb = new StringBuilder();
                //List<string> strList = d.StrList;
                foreach (var s in d.StrList) {
                    if (sb.Length != 0){
                        sb.Append("\t");
                    }
                    sb.Append(s);
                }
                //int i = _checkedListBox.Items.Add(sb.ToString());
                int i = ListViewItemAdd(sb.ToString());
                //_checkedListBox.SetItemChecked(i, d.Enable);
                _listView.Items[i].Checked = d.Enable;

            }
            //データがある場合は、１行目を選択する
            if (_listView.Items.Count > 0) {
                _listView.Items[0].Selected = true;
            }
        }

    	//***********************************************************************
    	// コントロールへの有効・無効
	    //***********************************************************************

        protected override void AbstractSetEnable(bool enabled){
            if (_border != null){
                //CtrlDatの場合は、disableで非表示にする
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
    		Util.RuntimeException("使用禁止");
            return false;
        }

        protected override string AbstractToText(){
    		Util.RuntimeException("使用禁止");
            return null;
        }

        protected override void AbstractFromText(string s){
    		Util.RuntimeException("使用禁止");
        }

        protected override void AbstractClear(){
    		Util.RuntimeException("使用禁止");
        }

        //Ver6.0.0　カラムヘッダクリック（ソート開始）
        void _listView_ColumnClick(object sender, ColumnClickEventArgs e) {
            if (e.Column == _sorter.Column) {
                _sorter.Order = _sorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            } else {
                _sorter.Column = e.Column;
                _sorter.Order = SortOrder.Ascending;
            }
            _listView.Sort();
        }

        //Ver6.0.0 1行を文字列化する
        String ListViewItemToString(int index) {
            var sb = new StringBuilder();
            foreach (ListViewItem.ListViewSubItem m in _listView.Items[index].SubItems) {
                if (sb.Length != 0) {
                    sb.Append("\t");
                }
                sb.Append(m.Text);
            }
            return sb.ToString();
        }
        //Ver6.0.0 文字列から指定行を変更する
        void ListViewItemEdit(int index, String str) {
            var item = StrToItem(str);
            item.Checked = _listView.Items[index].Checked;
            _listView.Items[index] = item;
        }

        //Ver6.0.0
        ListViewItem StrToItem(String str) {
            var tmp = str.Split('\t');
            var item = new ListViewItem();

            if (tmp.Count() == _listView.Columns.Count) {
                item.Text = tmp[0];
                for (var i = 1; i < tmp.Count(); i++) {
                    item.SubItems.Add(tmp[i]);
                }
            }
            return item;
        }

        //Ver6.0.0 文字列から１行追加する
        int ListViewItemAdd(String str) {
            var item = StrToItem(str);
            _listView.Items.Add(item);

            //カラム幅の調整
            for (var i = 0; i < item.SubItems.Count; i++) {
                var width = GetTextWidth(item.SubItems[i].Text);
                if (_listView.Columns[i].Width < width) {
                    _listView.Columns[i].Width = width;
                }
            }
            return _listView.Items.Count - 1;
        }
        //Ver6.0.0 文字列の描画領域の計算
        int GetTextWidth(string str) {
            var pictureBox = new PictureBox();
            //描画先とするImageオブジェクトを作成する
            var canvas = new Bitmap(pictureBox.Width, pictureBox.Height);
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            var g = Graphics.FromImage(canvas);
            //フォントオブジェクトの作成
            var fnt = new Font("Arial", 13);
            //文字列を描画する
            TextRenderer.DrawText(g, str, fnt, new Point(0, 0), Color.Black);
            //文字列を描画するときの大きさを計測する
            var size = TextRenderer.MeasureText(g, str, fnt);
            return size.Width;
        }

        //Ver6.0.0 同一データの検索
        int ListViewItemIndexOf(String str) {
            for (var i = 0; i < _listView.Items.Count; i++) {
                if (ListViewItemToString(i) == str) {
                    return i;
                }
            }
            return -1;
        }

        //Ver6.0.0
        public class Sorter : IComparer {
            private int _column;
            private SortOrder _order;
            private readonly CaseInsensitiveComparer _objectCompare;
            public int Column {
                get { return _column; }
                set { _column = value; }
            }
            public SortOrder Order {
                get { return _order; }
                set { _order = value; }
            }

            public Sorter() {
                _order = SortOrder.None;
                _objectCompare = new CaseInsensitiveComparer(CultureInfo.CurrentUICulture);
            }

            public int Compare(object x, object y) {
                var a = x as ListViewItem;
                var b = y as ListViewItem;
                var result = _objectCompare.Compare(a.SubItems[_column].Text, b.SubItems[_column].Text);
                if (_order == SortOrder.Ascending) {
                    return result;
                }
                if (_order == SortOrder.Descending) {
                    return (-result);
                }
                return 0;
            }
        }


    }
    
    


}
/*
	@Override
	public  void actionPerformed(ActionEvent e) {

		string cmd = e.getActionCommand();
		string source = e.getSource().getClass().getName();

		if (source.indexOf("JButton") != -1) {
			actionButton(cmd); //ボタンのイベント
		} else if (source.indexOf("CheckListBox") != -1) {
			actionCheckListBox(cmd); //チェックリストボックスのイベント
		}

	}

	//チェックリストボックスのイベント
	 void actionCheckListBox(string cmd) {
	}

}
*/