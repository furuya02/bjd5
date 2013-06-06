using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bjd.mail {
    //クラスMailBoxの内部で使用するデータ構造
    class OneMailBox {

        bool _isLogin;//ログイン中かどうかのフラグ

        public OneMailBox(string user, string pass) {
            User = user;
            Pass = pass;
            Addr = "";//最後にログインしたアドレス
            Dt = new DateTime(0);//最後にログインした時間
            _isLogin = false;
        }
        public string User { get; private set; }
        public string Pass { get; private set; }
        public string Addr { get; private set; }
        public DateTime Dt { get; private set; }
        public bool Login(string addr) {
            if (_isLogin)
                return false;

            _isLogin = true;//Ver5.6.4

            Addr = addr;
            Dt = DateTime.Now;

            //Ver5.9.1
            Thread.Sleep(1);//これが無いと、PopBeforeSmtpが失敗する場合がある
            return true;
        }
        public void Logout() {
            _isLogin = false;
        }
        public void SetPass(string pass){
            Pass = pass;
        }
    }
}
