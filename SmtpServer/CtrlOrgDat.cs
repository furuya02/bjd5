using System;
using Bjd;

namespace SmtpServer {
    //ホスト
    class CtrlOrgDat : CtrlDat {
        readonly OneCtrl target;
        readonly OneCtrl transfer;
        readonly OneCtrl port;
        readonly OneCtrl smtpAuth;
        readonly OneCtrl user;
        readonly OneCtrl pass;

        public CtrlOrgDat(string help, ListVal listVal, int width, int height, bool jp)
            : base(help, listVal, width, height, jp) {
            foreach (var o in listVal.Vals) {
                if (o.Name == "target") {
                    target = o.OneCtrl;
                } else if (o.Name == "transfer") {
                    transfer = o.OneCtrl;
                } else if (o.Name == "port") {
                    port = o.OneCtrl;
                } else if (o.Name == "smtpAuth") {
                    smtpAuth = o.OneCtrl;
                } else if (o.Name == "user") {
                    user = o.OneCtrl;
                } else if (o.Name == "pass") {
                    pass = o.OneCtrl;
                }
            }
        }

        //コントロールの入力内容に変化があった場合
        override public void ListValOnChange(object sender, EventArgs e) {
            var b = (bool)smtpAuth.GetValue();

            user.SetEnable(b);
            pass.SetEnable(b);

            if (!b) {
                user.SetValue((object)"");
                pass.SetValue((object)"");
            }

            base.ListValOnChange(sender, e);
        }

        //コントロールの入力が完了しているか
        override protected bool IsComplete() {
            //コントロールの入力が完了しているか
            bool isComplete = true;
            if (!target.IsComplete())
                isComplete = false;
            if (!transfer.IsComplete())
                isComplete = false;
            if (!port.IsComplete())
                isComplete = false;

            if ((bool)smtpAuth.GetValue()) {
                if (!user.IsComplete())
                    isComplete = false;
                if (!pass.IsComplete())
                    isComplete = false;
            }
            return isComplete;
        }
    }
}
