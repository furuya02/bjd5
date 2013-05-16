using System;
using Bjd;

namespace SmtpServer {
    class CtrlOrg3Dat : CtrlDat {
        readonly OneCtrl name;
        readonly OneCtrl address;
        readonly OneCtrl manager;
        readonly OneCtrl reacer;
        readonly OneCtrl contributor;
        readonly OneCtrl pass;

        public CtrlOrg3Dat(string help, ListVal listVal, int width, int height, bool jp)
            : base(help, listVal, width, height, jp) {
            foreach (var o in listVal.Vals) {
                if (o.Name == "name") {
                    name = o.OneCtrl;
                } else if (o.Name == "address") {
                    address = o.OneCtrl;
                } else if (o.Name == "manager") {
                    manager = o.OneCtrl;
                } else if (o.Name == "reacer") {
                    reacer = o.OneCtrl;
                } else if (o.Name == "contributor") {
                    contributor = o.OneCtrl;
                } else if (o.Name == "pass") {
                    pass = o.OneCtrl;
                }
            }
        }

        //コントロールの入力内容に変化があった場合
        override public void ListValOnChange(object sender, EventArgs e) {
            var m = (bool)manager.GetValue();//管理者
            pass.SetEnable(m);
            if (!m) {
                pass.FromText("");
            }
            base.ListValOnChange(sender, e);
        }

        //コントロールの入力が完了しているか
        override protected bool IsComplete() {
            //コントロールの入力が完了しているか
            bool isComplete = true;

            if (!name.IsComplete())
                isComplete = false;
            if (!address.IsComplete())
                isComplete = false;
            if (!reacer.IsComplete())
                isComplete = false;
            if (!contributor.IsComplete())
                isComplete = false;



            var m = (bool)manager.GetValue();//管理者
            if (m && !pass.IsComplete()) {
                isComplete = false;
            }
            return isComplete;
        }
    }
}