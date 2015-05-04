using System;
using Bjd.ctrl;
using Bjd.option;
using Bjd.util;

namespace SmtpServer {
    class CtrlOrgMemberDat : CtrlDat {
        readonly OneCtrl _name;
        readonly OneCtrl _address;
        readonly OneCtrl _manager;
        readonly OneCtrl _reacer;
        readonly OneCtrl _contributor;
        readonly OneCtrl _pass;

        public CtrlOrgMemberDat(string help, ListVal listVal, int height, LangKind langKind)
            : base(help, listVal, height, langKind) {
            foreach (var o in listVal) {
                if (o.Name == "name") {
                    _name = o.OneCtrl;
                } else if (o.Name == "address") {
                    _address = o.OneCtrl;
                } else if (o.Name == "manager") {
                    _manager = o.OneCtrl;
                } else if (o.Name == "reacer") {
                    _reacer = o.OneCtrl;
                } else if (o.Name == "contributor") {
                    _contributor = o.OneCtrl;
                } else if (o.Name == "pass") {
                    _pass = o.OneCtrl;
                }
            }
        }
        

        //コントロールの入力内容に変化があった場合
        //DEBUG
        override public void ListValOnChange() {
            var m = (bool)_manager.Read();//管理者
            _pass.SetEnable(m);
            if (!m) {
                _pass.Write("");
            }
            base.ListValOnChange();
        }
        
        //コントロールの入力が完了しているか
        override protected bool IsComplete() {
            //コントロールの入力が完了しているか
            var isComplete = _name.IsComplete();

            if (!_address.IsComplete())
                isComplete = false;
            if (!_reacer.IsComplete())
                isComplete = false;
            if (!_contributor.IsComplete())
                isComplete = false;

            var m = (bool)_manager.Read();//管理者
            if (m && !_pass.IsComplete()) {
                isComplete = false;
            }
            return isComplete;
        }
    }
}