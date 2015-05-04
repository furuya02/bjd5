using Bjd.ctrl;
using Bjd.option;
using Bjd.util;

namespace SmtpServer {
    //自動受信
    class CtrlOrgAutoReceptionDat : CtrlDat {
        readonly OneCtrl _receptionInterval;
        readonly OneCtrl _server;
        readonly OneCtrl _port;
        readonly OneCtrl _user;
        readonly OneCtrl _pass;
        readonly OneCtrl _localUser;
        readonly OneCtrl _synchronize;
        readonly OneCtrl _time;

        public CtrlOrgAutoReceptionDat(string help, ListVal listVal, int height, LangKind langKind)
            : base(help, listVal, height, langKind) {
            foreach (var o in listVal) {
                if (o.Name == "fetchReceptionInterval") {
                    _receptionInterval = o.OneCtrl;
                } else if (o.Name == "fetchServer") {
                    _server = o.OneCtrl;
                } else if (o.Name == "fetchPort") {
                    _port = o.OneCtrl;
                } else if (o.Name == "fetchUser") {
                    _user = o.OneCtrl;
                } else if (o.Name == "fetchPass") {
                    _pass = o.OneCtrl;
                } else if (o.Name == "fetchLocalUser") {
                    _localUser = o.OneCtrl;
                } else if (o.Name == "fetchSynchronize") {
                    _synchronize = o.OneCtrl;
                } else if (o.Name == "fetchTime") {
                    _time = o.OneCtrl;
                }
            }
        }
        /*
        //コントロールの入力内容に変化があった場合
        override public void ListValOnChange(object sender, EventArgs e) {
            var b = ((int)_synchronize.GetValue()) == 0;
            _time.SetEnable(b);

            base.ListValOnChange(sender, e);
        }
        */
        //コントロールの入力が完了しているか
        override protected bool IsComplete() {
            //コントロールの入力が完了しているか
            bool isComplete = _receptionInterval.IsComplete();
            if (!_server.IsComplete())
                isComplete = false;
            if (!_port.IsComplete())
                isComplete = false;
            if (!_user.IsComplete())
                isComplete = false;
            if (!_pass.IsComplete())
                isComplete = false;
            if (!_localUser.IsComplete())
                isComplete = false;
            if ((int)_synchronize.Read() == 0) {
                if (!_time.IsComplete())
                    isComplete = false;
            }
            return isComplete;

            /*
            var isComplete = true;
            if (!_receptionInterval.IsComplete())
                isComplete = false;
            if (!_server.IsComplete())
                isComplete = false;
            if (!_port.IsComplete())
                isComplete = false;
            if (!_user.IsComplete())
                isComplete = false;
            if (!_pass.IsComplete())
                isComplete = false;
            if (!_localUser.IsComplete())
                isComplete = false;
            if ((int)_synchronize.GetValue() == 0) {
                if (!_time.IsComplete())
                    isComplete = false;
            }
            return isComplete;             */
        }
    }
}