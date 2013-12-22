using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Bjd.ctrl;
using Bjd.net;
using Bjd.util;

namespace Bjd.option {
    //1つの値を表現するクラス<br>
    //ListValと共に再帰処理が可能になっている<br>
    public class OneVal : IDisposable{

        //[C#]コントロール変化時のイベント
        public delegate void OnChangeHandler();//デリゲート
        public event OnChangeHandler OnChange;//イベント

        public String Name { get; private set; }
        public Object Value { get; private set; }
        public OneCtrl OneCtrl { get; private set; }
        public Crlf Crlf { get; private set; }


        public OneVal(String name, Object value, Crlf crlf, OneCtrl oneCtrl){
            Name = name;
            Value = value;
            Crlf = crlf;
            OneCtrl = oneCtrl;

            oneCtrl.Name = name;

            //*************************************************************
            //仕様上、階層構造をなすOneValの名前は、ユニークである必要がる
            //プログラム作成時に重複を発見できるように、重複があった場合、ここでエラーをポップアップする
            //*************************************************************

            //名前一覧
            var tmp = new List<String>();

            //このlistの中に重複が無いかどうかをまず確認する
            List<OneVal> list = GetList(null);
            foreach (OneVal o in list){
                if (0 <= tmp.IndexOf(o.Name)){
                    //名前一覧に重複は無いか
                    Msg.Show(MsgKind.Error, String.Format("OneVal(OnePage)の名前に重複があります {0}", o.Name));
                }
                tmp.Add(o.Name); //名前一覧への蓄積
                //			if (o != this) { // 自分自身は検査対象外とする
                //				if (name.equals(o.getName())) {
                //					Msg.Show(MsgKind.Error, string.Format("OneVal(OnePage)の名前に重複があります %s", name));
                //				}
                //			}
            }
            //CtrlTabPageの場合は、array+ist<OnePage>の重複を確認する
            if (oneCtrl.GetCtrlType() == CtrlType.TabPage){
                foreach (OnePage onePage in ((CtrlTabPage) oneCtrl).PageList){
                    if (0 <= tmp.IndexOf(onePage.Name)){
                        //名前一覧に重複は無いか
                        Msg.Show(MsgKind.Error, string.Format("OneVal(OnePage)の名前に重複があります {0}", onePage.Name));
                    }
                    tmp.Add(onePage.Name);
                }
            }
        }
        
        //Ver6.0.0
        public void SetValue(object value){
            Value = value;

        }

        // 階層下のOneValを一覧する
        public List<OneVal> GetList(List<OneVal> list){
            if (list == null){
                list = new List<OneVal>();
            }

            if (OneCtrl.GetCtrlType() == CtrlType.Dat){
                list = ((CtrlDat) OneCtrl).ListVal.GetList(list);
            }
            else if (OneCtrl.GetCtrlType() == CtrlType.Group){
                list = ((CtrlGroup) OneCtrl).ListVal.GetList(list);
            }
            else if (OneCtrl.GetCtrlType() == CtrlType.TabPage){
                List<OnePage> pageList = ((CtrlTabPage) OneCtrl).PageList;
                foreach (OnePage onePage in pageList){
                    list = onePage.ListVal.GetList(list);
                }
            }
            list.Add(this);
            return list;
        }

        public List<OneVal> GetSaveList(List<OneVal> list) {
            if (list == null) {
                list = new List<OneVal>();
            }
            //if (OneCtrl.GetCtrlType() == CtrlType.Dat) {
            //    list = ((CtrlDat)OneCtrl).ListVal.GetList(list);
            if (OneCtrl.GetCtrlType() == CtrlType.Group) {
                list = ((CtrlGroup)OneCtrl).ListVal.GetSaveList(list);
            } else if (OneCtrl.GetCtrlType() == CtrlType.TabPage) {
                List<OnePage> pageList = ((CtrlTabPage)OneCtrl).PageList;
                foreach (OnePage onePage in pageList) {
                    list = onePage.ListVal.GetSaveList(list);
                }
            }
            list.Add(this);
            return list;
        }


        //入力を完了しているかどうか
        public bool IsComplete(){
            foreach (OneVal oneVal in GetList(null)){
                if (oneVal != this){
                    // 自分自身はループになるので対象外とする
                    if (!oneVal.IsComplete()){
                        return false;
                    }
                }
            }
            return OneCtrl.IsComplete();
        }

        public void Dispose(){}

        //public OneCtrl GetOneCtrl() {
        //    return oneCtrl;
        //}

        //public Crlf GetCrlf() {
        //    return crlf;
        //}

        //public Object GetValue() {
        //    return value;
        //}

        //public String GetName() {
        //    return name;
        //}

        //コントロール生成
        public void CreateCtrl(Control mainPanel, int baseX, int baseY, ref int tabIndex){
            OneCtrl.Create(mainPanel, baseX, baseY, Value, ref tabIndex);
            OneCtrl.OnChange += OneCtrl_OnChange;//[C#]
        }

        //[C#] コントロールの変化を伝達する
        void OneCtrl_OnChange() {
            if(OnChange!=null){
                OnChange();
            }
        }

        //コントロール破棄
        public void DeleteCtrl(){
            OneCtrl.OnChange -= OneCtrl_OnChange; //[C#]
            OneCtrl.Delete();
        }

        //コントロールからの値のコピー (isComfirm==true 確認のみ)
        public bool ReadCtrl(bool isConfirm){
            Object o = OneCtrl.Read();
            if (o == null){
                if (isConfirm){
                    // 確認だけの場合は、valueへの値セットは行わない
                    Msg.Show(MsgKind.Error, string.Format("データに誤りがあります 「{0}」", OneCtrl.Help));
                }
                return false;
            }
            Value = o; // 値の読込
            return true;
        }

        public Size Size{
            get { return OneCtrl.CtrlSize; }
        }

        //public void setListener(ICtrlEventListener listener) {
        //    for (OneVal oneVal : GetList(null)) {
        //        oneVal.getOneCtrl().setListener(listener);
        //    }
        //}

        //設定ファイル(Option.ini)への出力
        //isSecret=true デバッグ用の設定ファイル出力用（パスワード等を***で表現する）
        public String ToReg(bool isSecret){
            switch (OneCtrl.GetCtrlType()){
                case CtrlType.Dat:
                    if (Value == null){
                        var d = new Dat(((CtrlDat)OneCtrl).CtrlTypeList);
                        return d.ToReg(isSecret);
                    }
                    return ((Dat) Value).ToReg(isSecret);
                case CtrlType.CheckBox:
                    return ((bool)Value).ToString().ToLower();
                case CtrlType.Font:
                    if (Value != null) {
                        var font = (Font)Value;
                        return string.Format("{0},{1},{2}", font.FontFamily.Name, font.Size, font.Style.ToString());
                    }
                    return "";
                case CtrlType.File:
                case CtrlType.Folder:
                case CtrlType.TextBox:
                    return (String) Value;
                case CtrlType.Hidden:
                    if (isSecret){
                        return "***";
                    }
                    try{
                        return Crypt.Encrypt((String) Value);
                    }
                    catch (Exception){
                        return "ERROR";
                    }

                case CtrlType.Memo:
                    return Util.SwapStr("\r\n", "\t", (string)Value);
                case CtrlType.Radio:
                case CtrlType.ComboBox:
                case CtrlType.Int:
                    return ((int)Value).ToString();
                case CtrlType.BindAddr:
                    return Value.ToString();
                case CtrlType.AddressV4:
                    return Value.ToString();
                case CtrlType.TabPage:
                case CtrlType.Group:
                    return "";
                default:
                    return ""; // "実装されていないCtrlTypeが指定されました OneVal.toReg()"
            }
        }

        //出力ファイル(Option.ini)からの入力用<br>
        //不正な文字列があった場合は、無効行として無視される<br>
        public bool FromReg(String str){
            if (str == null){
                Value = null;
                return false;
            }
            switch (OneCtrl.GetCtrlType()){
                case CtrlType.Dat:
                    CtrlDat ctrlDat = (CtrlDat) OneCtrl;
                    Dat dat = new Dat(ctrlDat.CtrlTypeList);
                    if (!dat.FromReg(str)){
                        Value = null;
                        return false;
                    }
                    //Ver5.8.7 Java fix Datの中にComboBoxが存在する場合の旧バージョンの変換
                    foreach (var d in dat){
                        for (int i = 0; i < ctrlDat.ListVal.Count; i++){
                            if (ctrlDat.ListVal[i].OneCtrl.GetCtrlType() == CtrlType.ComboBox){
                                int val;
                                if(!int.TryParse(d.StrList[i],out val)){
                                    //Ver5.7.x以前のデータ
                                    OneVal dmy = new OneVal("dmy",null,Crlf.Nextline, ctrlDat.ListVal[i].OneCtrl);
                                    if (dmy.FromRegConv(d.StrList[i])){
                                        d.StrList[i] = dmy.ToReg(false);
                                    }
                                }

                            }
                        }
                    }
                    Value = dat;
                    break;
                case CtrlType.CheckBox:
                     try {
                        Value = Boolean.Parse(str);
                    } catch {
                        Value = false;
                        return false;
                    }
                    break;
                case CtrlType.Font:
                    Value = null;
                    var tmp = str.Split(',');
                    if (tmp.Length == 3){
                        try{

                            var family = new FontFamily(tmp[0]);
                            var size = (float) Convert.ToDouble(tmp[1]);
                            var style = (FontStyle) Enum.Parse(typeof (FontStyle), tmp[2]);
                            Value = new Font(family, size, style);
                        }catch(Exception){
                            Value = null;
                        }
                    }
                    if(Value==null){
                        return false;
                    }
                    break;
                case CtrlType.Memo:
                    Value = Util.SwapStr("\t", "\r\n", str);
                    break;
                case CtrlType.File:
                case CtrlType.Folder:
                case CtrlType.TextBox:
                    Value = str;
                    break;
                case CtrlType.Hidden:
                    try{
                        Value = Crypt.Decrypt(str);
                    }
                    catch (Exception){
                        Value = "";
                        return false;
                    }
                    break;
                case CtrlType.Radio:
                    try{
                        Value = Int32.Parse(str);
                    }catch (Exception){
                        Value = 0;
                        return false;
                    }
                    if ((int) Value < 0){
                        Value = 0;
                        return false;
                    }
                    break;
                case CtrlType.ComboBox:
                    int max = ((CtrlComboBox) OneCtrl).Max;
                    try {
                        var n = Int32.Parse(str);
                        if (n < 0 || max <= n) {
                            Value = 0;
                            return false;
                        }
                        Value = n;
                    } catch {
                        Value = 0;
                        //Ver5.9.2 Ver5.7.x以前のデータのコンバート
                        OneVal dmy = new OneVal("dmy", null, Crlf.Nextline, OneCtrl);
                        if (dmy.FromRegConv(str)) {
                            int n;
                            Int32.TryParse(dmy.ToReg(false), out n);
                            if (n < 0 || max <= n) {
                                Value = 0;
                                return false;
                            }
                            Value = n;
                        }
                        return false;
                    }
                    break;
                case CtrlType.Int:
                    try {
                        Value = Int32.Parse(str);
                    } catch {
                        Value = 0;
                        return false;
                    }
                    break;
                case CtrlType.BindAddr:
                    try{
                        Value = new BindAddr(str);
                    }
                    catch (ValidObjException){
                        Value = 0;
                        return false;
                    }
                    break;
                case CtrlType.AddressV4:
                    try{
                        Value = new Ip(str);
                    }
                    catch (ValidObjException){
                        Value = null;
                        return false;
                    }
                    break;
                case CtrlType.TabPage:
                case CtrlType.Group:
                    break;
                default:
                    Value = 0;
                    return false;
                    // "実装されていないCtrlTypeが指定されました OneVal.fromReg()"
            }
            return true;
        }

        //Ver5.8.4 Ver5.7.xデータのコンバートバージョン
        //コンバートした場合 return true 
        public bool FromRegConv(String str){
            if (str != null){
                switch (OneCtrl.GetCtrlType()){
                    case CtrlType.ComboBox:
                        var n = ((CtrlComboBox) OneCtrl).GetNewVal(str);
                        if (n != -1){
                            Value = n;
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

    }


}

