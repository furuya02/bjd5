using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;


namespace Bjd {
    public class Define {

        private Define(){}//デフォルトコンストラクタの隠蔽

        static string _executablePath = Application.ExecutablePath;
        static string _productVersion  = Application.ProductVersion;
        
        //Test用
        public static void SetEnv(string path,string ver) {
            _executablePath = path;
            _productVersion = ver;
        }

        public static string Copyright() {
            return "Copyright(c) 1998/05.. by SIN/SapporoWorks";
        }
        public static string HostName() {
            InitLocalInformation();//メンバ変数「localAddress」の初期化
            return _localName;
        }
        public static string ApplicationName() {
            return "BlackJumboDog";
        }
        public static string ExecutablePath() {
            return _executablePath;
        }
        public static string ProductVersion() {
            return _productVersion;
        }

        public static string Date() {
            DateTime dt = DateTime.Now;
            return dt.ToShortDateString() + " " + dt.ToLongTimeString();
        }
        public static string ServerAddress() {
            InitLocalInformation();//メンバ変数「localAddress」の初期化
            if (_localAddress.Count > 0)
                return _localAddress[0];
            return "127.0.0.1";
        }
        public static List<string> ServerAddressList() {
            InitLocalInformation();//メンバ変数「localAddress」の初期化
            return _localAddress;
        }
        public static string WebHome() {
            return "http://www.sapporoworks.ne.jp/spw/";
        }
        public static string WebDocument() {
            return "http://www.sapporoworks.ne.jp/spw/?page_id=517";
        }
        public static string WebSupport() {
            return "http://www.sapporoworks.ne.jp/sbbs/sbbs.cgi?book=bjd";
        }


        static List<string> _localAddress;//アドレス
        static string _localName;//ホスト名
        static void InitLocalInformation() {
            if (_localAddress == null) {//プログラム起動から初めて呼び出されたとき、１度だけ実行される
                _localAddress = new List<string>();
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface nic in nics) {
                    if (nic.OperationalStatus != OperationalStatus.Up)
                        continue;
                    IPInterfaceProperties props = nic.GetIPProperties();
                    foreach (UnicastIPAddressInformation info in props.UnicastAddresses) {
                        if(info.Address.AddressFamily == AddressFamily.InterNetwork)
                            _localAddress.Add(info.Address.ToString());
                    }
                }

                _localName = Dns.GetHostName();
            }
        }
    }    
}
