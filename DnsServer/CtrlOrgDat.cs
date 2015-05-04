using System;
using Bjd.ctrl;
using Bjd.option;
using Bjd.util;

namespace DnsServer{
    // リソース定義用にCtrlDatを拡張
    public class CtrlOrgDat : CtrlDat{

        private readonly OneCtrl _type;
        private readonly OneCtrl _name;
        private readonly OneCtrl _alias;
        private readonly OneCtrl _address;
        private readonly OneCtrl _priority;

        public CtrlOrgDat(string help, ListVal listVal, int height, LangKind langKkind)
            : base(help, listVal, height, langKkind) {
            foreach (OneVal o in listVal.GetList(null)){
                if (o.Name == "type"){
                    _type = o.OneCtrl;
                } else if (o.Name == "name"){
                    _name = o.OneCtrl;
                } else if (o.Name == "alias"){
                    _alias = o.OneCtrl;
                } else if (o.Name == "address"){
                    _address = o.OneCtrl;
                } else if (o.Name == "priority"){
                    _priority = o.OneCtrl;
                }
            }
        }
        //コントロールの入力内容に変化があった場合
        override public void ListValOnChange() {


            switch (_type.ToText()){
                case "0": //A
                case "1": //NS
                case "4": //AAAA
                    _name.SetEnable(true);
                    _alias.SetEnable(false);
                    _address.SetEnable(true);
                    _priority.SetEnable(false);
                    break;
                case "3": //CNAME
                    _name.SetEnable(true);
                    _alias.SetEnable(true);
                    _address.SetEnable(false);
                    _priority.SetEnable(false);
                    break;
                case "2": //MX
                    _name.SetEnable(true);
                    _alias.SetEnable(false);
                    _address.SetEnable(true);
                    _priority.SetEnable(true);
                    break;
                default:
                    Util.RuntimeException(string.Format("CtrlOrgDat.onChange() unknown type=[{0}]", _type));
                    break;
            }

            base.ListValOnChange();
        }

        //コントロールの入力が完了しているか
        protected override bool IsComplete() {
            bool isComplete = true;

            switch (_type.ToText()){
                case "0": //A
                case "1": //NS
                case "4": //AAAA
                    try{
                        _priority.Clear();
                    } catch (Exception){
                        //原因調査中 「Attempt to mutate in notification」が発生する
                        //System.out.println(string.Format("Exception %s", e.getMessage()));
                    }
                    _alias.FromText("");
                    if (!_name.IsComplete()){
                        isComplete = false;
                    }
                    if (!_address.IsComplete()){
                        isComplete = false;
                    }
                    break;
                case "3": //CNAME
                    try{
                        _priority.Clear();
                    } catch (Exception){
                        //原因調査中 「Attempt to mutate in notification」が発生する
                        //System.out.println(string.Format("Exception %s", e.getMessage()));
                    }
                    _address.FromText("");
                    if (!_name.IsComplete()){
                        isComplete = false;
                    }
                    if (!_alias.IsComplete()){
                        isComplete = false;
                    }
                    break;
                case "2": //MX
                    _alias.FromText("");
                    if (!_name.IsComplete()){
                        isComplete = false;
                    }
                    if (!_address.IsComplete()){
                        isComplete = false;
                    }
                    if (!_priority.IsComplete()){
                        isComplete = false;
                    }
                    break;
                default:
                    Util.RuntimeException(string.Format("CtrlOrgDat.IsComplete() unknown type=[{0}]", _type));
                    break;
            }
            return isComplete;
        }
    }
}
