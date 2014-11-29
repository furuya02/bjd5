using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SipServer {
    class SipUri {
        public Protocol Protocol { get; private set; }
        public string Display { get; private set; }
        public string Name { get; private set; }
        public string Pass { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }

        void ParseUri(string str){
            //;tagが、あれば排除する
            var index = str.IndexOf(";");
            if (index != -1) {
                str = str.Substring(0, index);
            }
            str = str.Trim();

            //プロトコル検出
            index = str.IndexOf(":");
            if (index == -1){
                return;
            }
            var tmp = str.Substring(0, index);
            if (tmp.ToUpper() == "SIPS") {
                Protocol = Protocol.Sips;
            } else if (tmp.ToUpper() == "SIP") {
                Protocol = Protocol.Sip;
            }
            str = str.Substring(index + 1);

            var host = str;
            var name = "";
            //ユーザ検出
            index = str.IndexOf("@");
            if (index != -1){
                name = str.Substring(0, index);
                host = str.Substring(index + 1);
            } 
            ParseHost(host);
            ParseName(name);
        }

        void ParseName(string str){
            if (str != ""){
                Name = str;
                var i = str.IndexOf(":");
                if (i != -1) {
                    Name = str.Substring(0, i);
                    Pass = str.Substring(i + 1);
                }
            }
        }

        void ParseHost(string str) {
            if (str != "") {
                Host = str;
                var i = str.IndexOf(":");
                if (i != -1) {
                    Host = str.Substring(0, i);
                    Port = Int32.Parse(str.Substring(i + 1));
                }
            }
        }

        public SipUri(string str) {
            Protocol = Protocol.Unknown;
            Name = "";
            Pass = "";
            Host = "";
            Port = 5060; //デフォルト値
            Display = "";
            if (str == null){
                return;
            }

            var uri = str;
            //<>で括られている場合、その中は有効なアドレスである
            var index = str.IndexOf("<");
            if (index != -1){
                Display = str.Substring(0, index).Trim(new[] { ' ', '"' });
                var tmp = str.Substring(index + 1);
                var i = tmp.IndexOf(">");
                if (i != -1){
                    str = tmp.Substring(0, i).Trim();
                    ParseUri(str);
                }
            } else{
                
                //;tagが、あれば排除する
                index = str.IndexOf(";");
                if (index != -1) {
                    str = str.Substring(0, index);
                }

                //プロトコル検出
                if (str.IndexOf(":") != -1) {
                    ParseUri(str);
                } else {
                    Display = str.Trim();
                }
            }
        }
        public override String ToString(){
            if (Display == "" && Protocol == Protocol.Unknown) {
                return "ERROR";
            }
            var sb = new StringBuilder();
            if (Display != "") {
                sb.Append(Display);
            }
            if (Protocol != Protocol.Unknown){
                sb.Append("<");
                if (Protocol == Protocol.Sip){
                    sb.Append("sip:");
                }else if (Protocol == Protocol.Sips){
                    sb.Append("sips:");
                }

                if (Name != ""){
                    sb.Append(Name);
                    if (Pass != ""){
                        sb.Append(":" + Pass);
                    }
                    sb.Append("@");
                }
                sb.Append(Host);
                sb.Append(String.Format(":{0}", Port));
                sb.Append(">");
            }
            return sb.ToString();
        }
    }
}
