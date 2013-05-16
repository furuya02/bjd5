using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.util;

//MD5

namespace Bjd.mail {

    public class MailBox{

        readonly Conf _conf;
        readonly List<OneUser> _userList = new List<OneUser>();
        readonly Logger _logger;

        public MailBox(Kernel kernel, Conf conf) {
            _conf = conf;
            Status = true;//初期化状態 falseの場合は、初期化に失敗しているので使用できない

            _logger = kernel.CreateLogger(_conf.NameTag, (bool)_conf.Get("useDetailsLog"), null);
            
            //基底クラスのstring dirの初期化
            Dir = (string)_conf.Get("dir");
            Dir = kernel.ReplaceOptionEnv(Dir);
            
            try {
                Directory.CreateDirectory(Dir);
            }catch{
                
            }

            if (!Directory.Exists(Dir)) {
                _logger.Set(LogKind.Error,null,9000029,string.Format("dir="));
                Status = false;
                Dir = null;
                return;//以降の初期化を処理しない
            }

            //ユーザリストの初期化
            InitUserList();

        }
        //****************************************************************
        //プロパティ
        //****************************************************************
        public string Dir { get; private set; }
        public bool Status { get; private set; }//初期化成否の確認
        //リモート操作（データの取得）用
        public List<string> UserList {
            get{
                return _userList.Select(o => o.User).ToList();
            }
        }

        
        //ユーザリストの初期化
        void InitUserList() {
            _userList.Clear();
            var dat = (Dat)_conf.Get("user");
            if (dat != null){
                foreach (var o in dat){
                    if (!o.Enable)
                        continue; //有効なデータだけを対象にする
                    var name = o.StrList[0];
                    var pass = Crypt.Decrypt(o.StrList[1]);
                    _userList.Add(new OneUser(name, pass));
                    var folder = string.Format("{0}\\{1}", Dir, name);
                    if (!Directory.Exists(folder)){
                        Directory.CreateDirectory(folder);
                    }
                }
            }
        }

        //重複しないファイル名を取得する
        //protected string CreateName() {
        //    while(true) {
        //        //Ver5.0.2 スレッドセーフでユニークなファイル名を生成する
        //        //string str = string.Format("{0:D20}",DateTime.Now.Ticks);
        //        //Ver5.0.0-b18
        //        //Thread.Sleep(1);
        //        Interlocked.Increment(ref createNameNumber);
        //        //Ver5.0.3
        //        //string str = string.Format("{0:D20}_{1:D2}",DateTime.Now.Ticks,createNameNumber);
        //        string str = string.Format("{0:D20}_{1:D2}",DateTime.Now.Ticks,createNameNumber % 100);

        //        string fileName = string.Format("{0}\\MF_{1}",Dir,str);
        //        if(!Directory.Exists(fileName)) {
        //            return str;
        //        }
        //    }
        //}
        protected string CreateName() {
            lock(this) {//Ver5.0.4 スレッドセーフの確保
                while(true) {
                    var str = string.Format("{0:D20}",DateTime.Now.Ticks);
                    Thread.Sleep(1);//Ver5.0.4 ウエイトでDateTIme.Nowの重複を避ける
                    var fileName = string.Format("{0}\\MF_{1}",Dir,str);
                    if(!File.Exists(fileName)) {
                        return str;
                    }
                }
            }
        }


        public bool Save(string user, Mail mail, MailInfo mailInfo) {
            //Ver_Ml
            if(!IsUser(user)) {
                _logger.Set(LogKind.Error,null,9000047,string.Format("[{0}] {1}",user,mailInfo));
                return false;
            }
            
            var folder = string.Format("{0}\\{1}",Dir,user);
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            var name = CreateName();

            //logger.Set(LogKind.Debug,null,7777,name);

            string fileName = string.Format("{0}\\MF_{1}",folder,name);
            if(mail.Save(fileName)) {
                fileName = string.Format("{0}\\DF_{1}",folder,name);
                mailInfo.Save(fileName);
                return true;
            }
            return false;
        }
        //ユーザが存在するかどうか
        public bool IsUser(string user){
            return _userList.Any(oneUser => oneUser.User == user);
        }

        //最後にログインに成功した時刻の取得 (PopBeforeSMTP用）
        public DateTime LastLogin(Ip addr){
            foreach (OneUser oneUser in _userList.Where(oneUser => oneUser.Addr == addr.ToString())){
                return oneUser.Dt;
            }
            return new DateTime(0);
        }

        //パスワード変更
        public bool Chps(string user, string pass) {
            if(pass==null){//無効なパスワードの指定は失敗する
                return false;
            }
            if (_userList.Any(oneUser => oneUser.User == user)){
                var dat = (Dat)_conf.Get("user");
                foreach (var o in dat.Where(o => o.StrList[0] == user)){
                    o.StrList[1] = Crypt.Encrypt(pass);
                    break;
                }
                _conf.Set("user", dat);//データ変更
                InitUserList();//ユーザリストの初期化（再読込）
                return true;
            }
            return false;
        }
        //認証（パスワード確認) ※パスワードの無いユーザが存在する?
        public bool Auth(string user, string pass) {
            foreach (OneUser oneUser in _userList) {
                if (oneUser.User == user){
                    return oneUser.Pass == pass;
                }
            }
            return false;
        }
        //パスワード取得
        public string GetPass(string user){
            return (from oneUser in _userList where oneUser.User == user select oneUser.Pass).FirstOrDefault();
        }

        //認証（パスワード確認) APOP対応
        public bool Auth(string user,string authStr,string recvStr) {
            foreach (OneUser oneUser in _userList) {
                if (oneUser.User != user)
                    continue;
                if (oneUser.Pass == null)//パスワードが無効
                    return false;

                var data = Encoding.ASCII.GetBytes(authStr + oneUser.Pass);
                var md5 = new MD5CryptoServiceProvider();
                var result = md5.ComputeHash(data);
                var sb = new StringBuilder();
                for (int i = 0; i < 16; i++) {
                    sb.Append(string.Format("{0:x2}", result[i]));
                }
                if (sb.ToString() == recvStr)
                    return true;
                return false;
            }
            return false;
        }
        public string Login(string user,Ip addr) {
            foreach (var oneUser in _userList){
                if (oneUser.User != user)
                    continue;
                if(oneUser.Login(addr.ToString())){
                    return string.Format("{0}\\{1}", Dir, user);
                }
            }
            return null;
        }
        public void Logout(string user) {
            foreach (var oneUser in _userList) {
                if (oneUser.User == user) {
                    oneUser.Logout();
                    return;
                }
            }
        }



        //クラスMailBoxの内部で使用するデータ構造
        class OneUser {

            bool _login;//ログイン中かどうかのフラグ

            public OneUser(string user, string pass) {
                User = user;
                Pass = pass;
                Addr = "";//最後にログインしたアドレス
                Dt = new DateTime(0);//最後にログインした時間
                _login = false;
            }

            //****************************************************************
            //プロパティ
            //****************************************************************
            public string User { get; private set; }
            public string Pass { get; private set; }
            public string Addr { get; private set; }
            public DateTime Dt { get; private set; }
            public bool Login(string addr) {
                if (_login)
                    return false;

                _login = true;//Ver5.6.4
                
                Addr = addr;
                Dt = DateTime.Now;
                return true;
            }
            public void Logout() {
                _login = false;
            }
        }

    }



}

