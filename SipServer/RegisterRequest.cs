using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;

namespace SipServer {

    class RegisterRequest {
        public SipUri To{ get; private set; }
        public SipUri From{ get; private set; }
        public SipUri Contact { get; private set; }
        public SipUri Server { get; private set; }
        public int Expires { get; private set; }

        public RegisterRequest(Reception reception){
            Expires = 1800;//デフォルト値

            To = new SipUri(reception.Header.GetVal("To"));
            From = new SipUri(reception.Header.GetVal("From"));
            var contact = reception.Header.GetVal("Contact");
            Contact = new SipUri(contact);
            if (contact != null){
                var index = contact.IndexOf(";");
                if (index != -1){
                    var tmp = contact.Substring(index + 1);
                    var i = tmp.ToLower().IndexOf("expires");
                    if (i != -1){
                        var expires = tmp.Substring(i+8);
                        var result = 0;
                        if (Int32.TryParse(expires, out result)){
                            Expires = result;
                        }
                    }
                }
            }
            var s = reception.Header.GetVal("Expires");
            if (s != null){
                var result = 0;
                if (Int32.TryParse(s, out result)) {
                    Expires = result;
                }
            }

            Server = reception.StartLine.RequestUri;

        }
    }
}
