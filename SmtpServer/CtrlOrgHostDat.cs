using Bjd.ctrl;
using Bjd.option;
using Bjd.util;

namespace SmtpServer {
    //ホスト
    class CtrlOrgHostDat : CtrlDat {
        readonly OneCtrl _target;
        readonly OneCtrl _server;
        readonly OneCtrl _port;
        readonly OneCtrl _smtpAuth;
        readonly OneCtrl _user;
        readonly OneCtrl _pass;

        public CtrlOrgHostDat(string help, ListVal listVal, int height, LangKind langKind)
            : base(help, listVal, height, langKind) {
            foreach (var o in listVal) {
                if (o.Name == "transferTarget") {
                    _target = o.OneCtrl;
                } else if (o.Name == "transferServer") {
                    _server = o.OneCtrl;
                } else if (o.Name == "transferPort") {
                    _port = o.OneCtrl;
                } else if (o.Name == "transferSmtpAuth") {
                    _smtpAuth = o.OneCtrl;
                } else if (o.Name == "transferUser") {
                    _user = o.OneCtrl;
                } else if (o.Name == "transferPass") {
                    _pass = o.OneCtrl;
                }
            }
        }
        /*
        //コントロールの入力内容に変化があった場合
        override public void ListValOnChange(object sender, EventArgs e) {
            var b = (bool)_smtpAuth.GetValue();

            _user.SetEnable(b);
            _pass.SetEnable(b);

            if (!b) {
                _user.SetValue("");
                _pass.SetValue("");
            }

            base.ListValOnChange(sender, e);
        }
        */
        //コントロールの入力が完了しているか
        override protected bool IsComplete() {
            //コントロールの入力が完了しているか
            var isComplete = _target.IsComplete();
            if (!_server.IsComplete())
                isComplete = false;
            if (!_port.IsComplete())
                isComplete = false;

            if ((bool)_smtpAuth.Read()) {
                if (!_user.IsComplete())
                    isComplete = false;
                if (!_pass.IsComplete())
                    isComplete = false;
            }
            return isComplete;
        }
    }
}
