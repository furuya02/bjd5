using System;
using System.Windows.Forms;
using Bjd.ctrl;
using Bjd.net;
using Bjd.util;

namespace Bjd.option {
    abstract public class OneOption : IDisposable {

        public ListVal ListVal { get; private set; }
	    private readonly bool _isJp;
        public string NameTag { get; private set; }
        public string Path {get; private set;  }//実態が格納されているモジュール(DLL)のフルパス
        
        abstract public string JpMenu { get; }
        abstract public string EnMenu { get; }
        abstract public char Mnemonic { get; }

        //Ver6.1.6
        protected readonly Lang Lang;

        public OneOption(bool isJp,string path, string nameTag) {
            ListVal = new ListVal();
            _isJp = isJp;
            Path = path;
            NameTag = nameTag;

            //Ver6.1.6
            Lang = new Lang(IsJp() ? LangKind.Jp : LangKind.En, "Option" + nameTag);

            ListVal.OnChange += ArOnChange;
        }

        protected bool IsJp(){
            return _isJp;
        }


	    //レジストリへ保存
        public void Save(IniDb iniDb) {
            iniDb.Save(NameTag, ListVal);//レジストリへ保存
        }

        	

        //レジストリからの読み込み
        public void Read(IniDb iniDb) {
	    	iniDb.Read(NameTag, ListVal);
        }

        //listValの初期化
//        protected void Init() {
//
//
//            //「ACL」タブの追加
//            if (_useAcl) {
//                var list = new ListVal();
        //                list.Add(new OneVal("enableAcl", 0, Crlf.Nextline, new CtrlRadio((Kernel.IsJp()) ? "指定したアドレスからのアクセスのみを" : "Access of ths user who appoint it", new List<string> { (Kernel.IsJp()) ? "許可する" : "Allow", (Kernel.IsJp()) ? "禁止する" : "Deny" }, OptionDlg.Width() - 15, 2)));
//                {//DAT
//                    var l = new ListVal();
//                    l.Add(new OneVal("aclName", "", Crlf.Nextline, new CtrlTextBox((Kernel.IsJp()) ? "名前（表示名）" : "Name(Display)", 200)));
//                    l.Add(new OneVal("aclAddress", "", Crlf.Nextline, new CtrlTextBox((Kernel.IsJp()) ? "アドレス" : "Address", 300)));
//                    list.Add(new OneVal("acl", null, Crlf.Nextline, new CtrlDat((Kernel.IsJp()) ? "利用者（アドレス）の指定" : "Access Control List", l, 600, 340, Kernel.IsJp())));
//                }//DAT
//                Add(new OneVal("ACL", list, Crlf.Nextline, new CtrlTabPage("ACL")));
//            }
//
//            //名前重複の確認 + ar.Valsの初期化
//            foreach (var a in ListVal.Vals) {
//                if (1 != ListVal.Vals.Count(o => o.Name == a.Name)) {
//                    throw new Exception(string.Format("Name repetition {0}-{1}\r\n", this, a.Name));
//                }
//            }
//
//            //レジストリからの読み込み
//            _iniDb.Read(NameTag, ListVal);
//        }
//
        protected OnePage PageAcl() {
		    var onePage = new OnePage("ACL", "ACL");
		    onePage.Add(new OneVal("enableAcl", 0, Crlf.Nextline, new CtrlRadio(_isJp ? "指定したアドレスからのアクセスのみを": "Access of ths user who appoint it",
                new[] { _isJp ? "許可する" : "Allow", _isJp ? "禁止する" : "Deny" }, OptionDlg.Width() - 15, 2)));

		    var list = new ListVal();
		    list.Add(new OneVal("aclName", "", Crlf.Nextline, new CtrlTextBox(_isJp ? "名前（表示名）" : "Name(Display)", 20)));
		    list.Add(new OneVal("aclAddress", "", Crlf.Nextline, new CtrlTextBox(_isJp ? "アドレス" : "Address", 20)));
		    onePage.Add(new OneVal("acl", null, Crlf.Nextline, new CtrlDat(_isJp ? "利用者（アドレス）の指定" : "Access Control List",list, 310, _isJp)));

		    return onePage;
	    }


        //OneValとしてサーバ基本設定を作成する
    	protected OneVal CreateServerOption(ProtocolKind protocolKind, int port, int timeout, int multiple) {
		    var list = new ListVal();
		    list.Add(new OneVal("protocolKind", protocolKind, Crlf.Contonie, new CtrlComboBox(_isJp ? "プロトコル"
				: "Protocol", new [] { "TCP", "UDP" }, 60)));
		    list.Add(new OneVal("port", port, Crlf.Nextline, new CtrlInt(_isJp ? "クライアントから見たポート" : "Port (from client side)", 5)));
		    var localAddress = LocalAddress.GetInstance();
		    var v4 = localAddress.V4;
		    var v6 = localAddress.V6;
	    	list.Add(new OneVal("bindAddress2", new BindAddr(), Crlf.Nextline, new CtrlBindAddr(_isJp ? "待ち受けるネットワーク": "Bind Address", v4, v6)));
		    list.Add(new OneVal("useResolve", false, Crlf.Nextline, new CtrlCheckBox((_isJp ? "クライアントのホスト名を逆引きする": "Reverse pull of host name from IP address"))));
		    list.Add(new OneVal("useDetailsLog", true, Crlf.Contonie, new CtrlCheckBox(_isJp ? "詳細ログを出力する": "Use Details Log")));
		    list.Add(new OneVal("multiple", multiple, Crlf.Contonie, new CtrlInt(_isJp ? "同時接続数" : "A repetition thread", 5)));
		    list.Add(new OneVal("timeOut", timeout, Crlf.Nextline, new CtrlInt(_isJp ? "タイムアウト(秒)" : "Timeout", 6)));
		    return new OneVal("GroupServer", null, Crlf.Nextline, new CtrlGroup(_isJp ? "サーバ基本設定" : "Server Basic Option",list));
	    }

	    //ダイアログ作成時の処理
    	public void CreateDlg(Panel mainPanel) {
	    	// 表示開始の基準位置
		    const int x = 0;
		    const int y = 0;
    	    int tabIndex = 0;
		    ListVal.CreateCtrl(mainPanel, x, y,ref tabIndex);
		    //ListVal.setListener(this);

		    // 基底クラスのセットアップされる「サーバ設定」などのコントロールの状態を初期化するため、このダミーのイベントを発生させる
		    ArOnChange();
	    }


        //OKボタンを押したときの処理
        public bool OnOk(bool isComfirm){
            return ListVal.ReadCtrl(isComfirm);
        }
        //ダイアログが閉じるときの処理
        public void CloseDlg() {
		    ListVal.DeleteCtrl();
        }

        //名前からコントロールを詮索する
	    //処理だと処理が重くなるので、該当が無い場合nullを返す
    	protected OneCtrl GetCtrl(String name) {
		    OneVal oneVal = ListVal.Search(name);
		    if (oneVal == null) {
			    return null;
		    }
		    return oneVal.OneCtrl;
	}
            //OneValの追加
    	public void Add(OneVal oneVal) {
		    ListVal.Add(oneVal);
	    }

        //値の設定
        public void SetVal(IniDb iniDb, string name, object value) {
            var oneVal = ListVal.Search(name);
		    if (oneVal == null) {
			    Util.RuntimeException(string.Format("名前が見つかりません name={0}", name));
		        return;
		    }
		    //コントロールの値を変更
		    oneVal.OneCtrl.Write(value);

            //Ver6.0.0
            oneVal.SetValue(value);

		    //レジストリへ保存
            Save(iniDb);

        }
        //値の取得
        public object GetValue(string name) {
		    var oneVal = ListVal.Search(name);
		    if (oneVal == null) {
			    Util.RuntimeException(string.Format("名前が見つかりません name={0}", name));
			    return null;
		    }
		    return oneVal.Value;
        }


        //「サーバを使用する」の状態取得
        public bool UseServer {
            get {
		        var oneVal = ListVal.Search("useServer");
		        if (oneVal == null) {
			        return false;
		        }
		        return (bool) oneVal.Value;
            }
        }


        //コントロールが変化した時のイベント処理
        virtual public void OnChange() { }
        void ArOnChange() {
            var o = GetCtrl("protocolKind");
            if (o != null)
                o.SetEnable(false);// プロトコル 変更不可

//            o = GetCtrl("port");
//			if (o != null) 
//				o.SetEnable(false); // ポート番号変更禁止
            OnChange();
        }

        public void Dispose() {
            
        }
    }
}

