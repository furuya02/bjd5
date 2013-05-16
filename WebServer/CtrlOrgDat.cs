using Bjd.ctrl;
using Bjd.option;

namespace WebServer {
    class CtrlOrgDat : CtrlDat {
        readonly OneCtrl _protocol;
        readonly OneCtrl _port;

        public CtrlOrgDat(string help, ListVal listVal, int width, int height, bool jp)
            : base(help, listVal, height, jp) {
            foreach (var o in listVal) {
                switch (o.Name){
                    case "protocol":
                        _protocol = o.OneCtrl;
                        break;
                    case "port":
                        _port = o.OneCtrl;
                        break;
                }
            }
        }
        /*
        //コントロールの入力内容に変化があった場合
        override public void ListValOnChange(object sender, EventArgs e) {
            if (0 == (int)_protocol.GetValue()) { //HTTP
                var n = (int)_port.GetValue();
                if (n == 443) {
                    _port.SetValue(80);
                }

                _port.SetEnable(true);
            } else { //HTTPS
                _port.SetValue(443);
                _port.SetEnable(false);
            }
            //Ver5.4.2 修正
            base.ListValOnChange(sender, e);
        }
         * */
    }
}
