using System;
using Bjd;

namespace SmtpServer {
    //自動受信
    class CtrlOrg2Dat : CtrlDat {
        readonly OneCtrl receptionInterval;
        readonly OneCtrl server;
        readonly OneCtrl port;
        readonly OneCtrl user;
        readonly OneCtrl pass;
        readonly OneCtrl localUser;
        readonly OneCtrl synchronize;
        readonly OneCtrl time;

        public CtrlOrg2Dat(string help, ListVal listVal, int width, int height, bool jp)
            : base(help, listVal, width, height, jp) {
            foreach (var o in listVal.Vals) {
                if (o.Name == "receptionInterval") {
                    receptionInterval = o.OneCtrl;
                } else if (o.Name == "server") {
                    server = o.OneCtrl;
                } else if (o.Name == "port") {
                    port = o.OneCtrl;
                } else if (o.Name == "user") {
                    user = o.OneCtrl;
                } else if (o.Name == "pass") {
                    pass = o.OneCtrl;
                } else if (o.Name == "localUser") {
                    localUser = o.OneCtrl;
                } else if (o.Name == "synchronize") {
                    synchronize = o.OneCtrl;
                } else if (o.Name == "time") {
                    time = o.OneCtrl;
                }
            }
        }

        //コントロールの入力内容に変化があった場合
        override public void ListValOnChange(object sender, EventArgs e) {
            var b = ((int)synchronize.GetValue()) == 0 ? true : false;
            time.SetEnable(b);

            base.ListValOnChange(sender, e);
        }

        //コントロールの入力が完了しているか
        override protected bool IsComplete() {
            //コントロールの入力が完了しているか
            bool isComplete = true;
            if (!receptionInterval.IsComplete())
                isComplete = false;
            if (!server.IsComplete())
                isComplete = false;
            if (!port.IsComplete())
                isComplete = false;
            if (!user.IsComplete())
                isComplete = false;
            if (!pass.IsComplete())
                isComplete = false;
            if (!localUser.IsComplete())
                isComplete = false;
            if ((int)synchronize.GetValue() == 0) {
                if (!time.IsComplete())
                    isComplete = false;
            }
            return isComplete;
        }
    }
}