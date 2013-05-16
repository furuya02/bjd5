using System;
using System.Linq;
using Bjd;
using System.Text;
using Bjd.util;

namespace SipServer {
    //*****************************************************
    //先頭行のSIPメソッドを扱うクラス
    //*****************************************************
    
    class StartLine {
        public ReceptionKind ReceptionKind { get; private set; }
        public SipMethod SipMethod { get; private set; }
        public string RequestUri { get; private set; }//宛先
        public SipVer SipVer { get; private set; }
        public int StatusCode { get; private set; }
        public string ResponseStr { get; private set; }
        
        public StartLine(byte [] buf) {

            Init();
            
            string str = Encoding.ASCII.GetString(Inet.TrimCrlf(buf));
            
            //3カラムで無い場合はエラーと認識される
            var tmp = str.Split(' ');
            if (tmp.Count() != 3) {
                return;
            }
            
            //先頭がSIP/で始まっている場合、ステータスラインとして処理する
            if (tmp[0].IndexOf("SIP/") == 0) {
                SipVer = new SipVer(tmp[0]);


                int result;
                if (Int32.TryParse(tmp[1], out result)) {
                    StatusCode = result;
                }
                ResponseStr = tmp[2];

                //すべての初期化が成功したとき、リクエストラインとして認める
                if (StatusCode != 0 && ResponseStr != "" && SipVer.No != 0) {
                    ReceptionKind = ReceptionKind.Status;
                }
 


            } else {
                foreach (SipMethod m in Enum.GetValues(typeof(SipMethod))) {
                    if (m.ToString().ToUpper() == tmp[0].ToUpper()) {
                        SipMethod = m;
                        break;
                    }
                }
                if (tmp[1].IndexOf("sip:") == 0) {
                    RequestUri = tmp[1].Substring(4);
                }
                SipVer = new SipVer(tmp[2]);

                //すべての初期化が成功したとき、リクエストラインとして認める
                if (SipMethod != SipMethod.Unknown && RequestUri != "" && SipVer.No != 0) {
                    ReceptionKind = ReceptionKind.Request;
                }

            }
            if (ReceptionKind == ReceptionKind.Unknown) {
                Init();//無効の場合は、誤動作の発見を早くするため、すべてを初期化する
            }
        }

        void Init() {
            ReceptionKind = ReceptionKind.Unknown;
            SipMethod = SipMethod.Unknown;
            RequestUri = "";
            SipVer = new SipVer();
            StatusCode = 0;
            ResponseStr = "";

        }
    }
}
