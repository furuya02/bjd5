using System;
using System.Collections.Generic;
using Bjd.ctrl;
using Bjd.util;

namespace Bjd.option{
    public class Conf{
        //Optionクラスへ結合を排除するためのクラス<br>
        //Optionの値を個別に設定できる（テスト用）<br>

        private readonly Dictionary<string, object> _ar = new Dictionary<string, object>();
        public string NameTag { get; private set; }

        //テスト用コンストラクタ
        public Conf(){
            NameTag = "";
        }

        public Conf(OneOption oneOption){

            NameTag = oneOption.NameTag;

            var list = oneOption.ListVal.GetList(null);
            foreach (var o in list){
                var ctrlType = o.OneCtrl.GetCtrlType();
                switch (ctrlType){
                    case CtrlType.Dat:
                        if (o.Value != null){
                            _ar.Add(o.Name, o.Value);
                        } else {
                            _ar.Add(o.Name, new Dat(((CtrlDat)o.OneCtrl).CtrlTypeList));
                        }
                        break;
                    case CtrlType.CheckBox:
                    case CtrlType.TextBox:
                    case CtrlType.AddressV4:
                    case CtrlType.AddressV6:
                    case CtrlType.BindAddr:
                    case CtrlType.Folder:
                    case CtrlType.File:
                    case CtrlType.ComboBox:
                    case CtrlType.Int:
                    case CtrlType.Memo:
                    case CtrlType.Font:
                    case CtrlType.Radio:
                    case CtrlType.Hidden:
                        _ar.Add(o.Name, o.Value);
                        break;
                    case CtrlType.TabPage:
                    case CtrlType.Group:
                    case CtrlType.Label:
                        break;
                    default:
                        Util.RuntimeException(string.Format("未定義 {0}", ctrlType));
                        break;
                }
            }
        }

        //値の取得
        //存在しないタグを指定すると実行事例がが発生する
        public Object Get(String name){
            if (!_ar.ContainsKey(name)) {
                //HashMapの存在確認
                Util.RuntimeException(string.Format("未定義 {0}", name));
            }
            return _ar[name];
        }

        //値の設定
        //存在しないタグを指定すると実行事例がが発生する
        public void Set(String name, Object value){
            if (!_ar.ContainsKey(name)) {
                //HashMapの存在確認
                Util.RuntimeException(string.Format("未定義 {0}", name));
            }
            _ar[name] = value;
        }

        //値の設定
        //存在しないタグを指定できる（テスト用）
        public void Add(String name, Object value) {
            if (!_ar.ContainsKey(name)){
                _ar.Add(name,value);
            } else{
                _ar[name] = value;
            }
        }

        public void Save(IniDb iniDb){
            throw new NotImplementedException();
        }
    }
}

