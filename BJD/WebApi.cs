using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.server;

namespace Bjd {
    public class WebApi {
        //サーバの起動・停止
        public bool ServiceSmtp { get; set; }
        private Dictionary<String, int> _responseSmtp; 
        
        public WebApi(){
            ControlInit();
            ResponseInit();
        }

        //コントロールに関してデフォルト値での初期化
        public void ControlInit(){
            ServiceSmtp = true; 
        }
        //レスポンス制御に関してデフォルト値での初期化
        public void ResponseInit() {
            _responseSmtp = new Dictionary<string, int>();
        }
        //レスポンス制御の追加
        public void ResponseAdd(string key, string value){
            int n;
            if (Int32.TryParse(value, out n)){
                _responseSmtp.Add(key,n);
            }
        }

        public int ResponseSmtp(string cmd){
            foreach (var p in _responseSmtp){
                if (p.Key.ToLower() == cmd.ToLower()){
                    return p.Value;
                }
            }
            return -1;
        }
    }
}
