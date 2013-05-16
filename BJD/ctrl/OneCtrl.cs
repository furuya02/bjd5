using System;
using System.Drawing;
using System.Windows.Forms;
using Bjd.util;

namespace Bjd.ctrl{
    public abstract class OneCtrl{
        private Control _owner;
        private int _controlCounter; // 生成したコントロールを全部はきしたかどうかを確認するためのカウンタ

        //OneValのコンストラクタでnameの初期化に使用される
        //OneValのコンストラクタ内以外で利用してはならない
        public string Name { get; set; }
        protected Control Panel = null;
        protected int Margin = 3;
        protected int DefaultHeight = 20;

        public string Help { get; private set; }

        //[C#]コントロール変化時のイベント
        public delegate void OnChangeHandler();//デリゲート
        public event OnChangeHandler OnChange;//イベント


        //コントロールのサイズの取得
        public Size CtrlSize{
            get{
                if (Panel == null){
                    return new Size(0, 0);
                }
                return new Size(Panel.Width, Panel.Height);
            }
        }

        //コンストラクタ
        public OneCtrl(string help){
            if(help==null){
                Help = "";
            }else{
                Help = help;
            }
        }
        
        //[C#] コントロールの変化
        protected void Change(object sender, EventArgs e){
            if(OnChange!=null){
                OnChange();
            }
        }


        //コントロールの種類の取得（継承クラスで実装）
        public abstract CtrlType GetCtrlType();

        //コントロールの生成（継承クラスで実装）
        protected abstract void AbstractCreate(Object value,ref int tabIndex);

        //コントロールの生成
        public void Create(Control owner, int x, int y, Object value,ref int tabIndex) {
            _owner = owner;

            if (Panel == null){

                Panel = (Panel) Create(owner, new Panel(), x, y , -1); //tabIndex=-1

                // Debug 色付ける
                // Random r = new Random();
                // Color bc = new Color(r.nextInt(205), r.nextInt(205),
                // r.nextInt(205));
                // panel.setBackground(bc);

                // 全部の子コントロールをベースとなるpanelのサイズは、abstractCreate()で変更される
                AbstractCreate(value,ref tabIndex); // panelの上に独自コントロールを配置する
            }
        }
        
        //テキスト表示用の幅を取得する
        protected int TextWidth(string str) {
            var label = new Label { Text = str };
            var width = label.PreferredWidth;
            label.Dispose();
            return width;
        }

        //[C#]ラベルの幅を取得する
        protected int LabelWidth(Label label){
            double w = label.Width * 1.1;
            return  (int)w;
        }


        //コントロールの破棄（継承クラスで実装）
        protected abstract void AbstractDelete();

        //コントロールの破棄
        public void Delete(){
            AbstractDelete();

            if (_owner != null){
                Remove(_owner, Panel);
            }
            Panel = null;
            if (_controlCounter != 0){
                Msg.Show(MsgKind.Error,String.Format("生成したコントロールと破棄したコントロールの数が一致しません。 remove()に漏れが無いかを確認する必要があります。 {0}", GetCtrlType()));
            }
        }

        //フィールドテキストに合わせてサイズを自動調整する
        protected void SetAutoSize(Control control){
            if (control != null){
                control.AutoSize = true;
            }

            //Dimension dimension = component.getPreferredSize(); // 適切サイズを取得
            //dimension.width += 8; // 微調整
            //component.setSize(dimension);
        }


        // ***********************************************************************
        // コントロールの値の読み書き
        // データが無効なときnullが返る
        // ***********************************************************************
        //コントロールの値の取得(継承クラスで実装)<br>
        //TODO abstractRead() nullを返す際に、コントロールを赤色表示にする
        protected abstract Object AbstractRead();

        //コントロールの値の取得
        public Object Read(){
            return AbstractRead();
        }

        //コントロールの値の設定(継承クラスで実装)
        protected abstract void AbstractWrite(Object value);

        //コントロールの値の設定
        public void Write(Object value){
            //コントロールが生成されていないとき、コントロールへの値の設定は必要ない
            if(Panel==null){
                return;
            }
            AbstractWrite(value);
        }

        // ***********************************************************************
        // コントロールへの有効・無効
        // ***********************************************************************
        //有効・無効の設定(継承クラスで実装)
        protected abstract void AbstractSetEnable(bool enabled);

        //有効・無効の設定
        public void SetEnable(bool enabled){
            if (Panel != null){
                AbstractSetEnable(enabled);
            }
        }

        // ***********************************************************************
        // コントロールの生成・破棄（共通関数）
        // ***********************************************************************
        protected Control Create(Control parent, Control self, int x, int y, int tabIndex) {
            _controlCounter++;
            Control control = self;
            //control.SetLocation(x, y);
            control.Left = x;
            control.Top = y;
            if (self is Button){
                // JButtonは、AutoSizeだと小さくなってしまう
                //control.setSize(75, 22);
                control.Width = 75;
                control.Height = 22;
            }else{
                SetAutoSize(control); // サイズ自動調整(この時点でテキストが適切に設定されているばあ、これでサイズの調整は終わる)
            }

            if (tabIndex == -1) {
                control.TabStop = false;
            } else {
                control.TabIndex = tabIndex;
            }

            // JScrollPaneは、textAreaを配置する関係で、setLayout(null)だと入力できなくなる
            // JTabbedPaneは、setLayout(null)すると例外が発生する
            //if (!(self is ScrollPane) && !(self is TabbedPane)){
            //    control.setLayout(null); // 子コントロールを絶対位置で表示する
            //}
            if (parent != null){
                // ownerがnullの場合は、非表示（デバッグモード）
                parent.Controls.Add(control);
                //owner.Add(control);
                //control.setFont(owner.getFont()); // フォントの継承
            }
            return control;
        }

        protected void Remove(Control palent, Control self){
            if (self != null){
                _controlCounter--;
                if (palent != null){
                    // ownerがnullの場合は、非表示（デバッグモード）
                    palent.Controls.Remove(self);
                    self.Dispose();
                }
            }
            //RemoveListener(); // リスナーも削除する
        }

        //// ***********************************************************************
        //// イベントリスナー関連
        //// ***********************************************************************
        //public final void setListener(ICtrlEventListener listener) {
        //    listenerList.add(listener);
        //}

        //public final void removeListener() {
        //    while (listenerList.size() != 0) {
        //        listenerList.remove(0);
        //    }
        //}
        //protected final void setOnChange() {
        //    for (ICtrlEventListener listener : listenerList) {
        //        listener.onChange(this);
        //    }
        //}

        // ***********************************************************************
        // CtrlDat関連　(Add/Del/Edit)の状態の変更、チェックリストボックスとのテキストの読み書き
        // ***********************************************************************
        protected abstract bool AbstractIsComplete();
        //CtrlDatで入力が入っているかどうかでボタン
        public bool IsComplete(){
            if (Panel != null){
                return AbstractIsComplete();
            }
            return false;
        }


        protected abstract String AbstractToText();

        //CtrlDatでリストボックスに追加するため使用される
        public string ToText(){
            if (Panel != null){
                return AbstractToText();
            }
            return "";
        }
        protected abstract void AbstractFromText(String s);
	    
        //CtrlDatでリストボックスから値を戻す時、使用される
    	public void FromText(String s) {
		    if (Panel != null) {
			    AbstractFromText(s);
		    }
	    }

	    protected abstract void AbstractClear();
	    //CtrlDatでDelDelボタンを押したときに使用される
    	public void Clear() {
		    if (Panel != null) {
			    AbstractClear();
		    }
	    }
    }

}






