using System;
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

        //コントロールの入力内容に変化があった場合
        public override void ListValOnChange() {
            if (0 == (int)_protocol.Read()) { //HTTP
                var n = (int)_port.Read();
                if (n == 443) {
                    _port.Write(80);
                }

                _port.SetEnable(true);
            } else { //HTTPS
                _port.Write(443);
                _port.SetEnable(false);
            }
            base.ListValOnChange();
        }
        
    }
}
