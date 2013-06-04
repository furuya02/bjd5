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

namespace Bjd.mail{

    public class MailBox{
        private readonly List<OneMailBox> _ar = new List<OneMailBox>();
        private readonly Logger _logger; //テストの際はnullでも大丈夫

        public string Dir { get; private set; }
        public bool Status { get; private set; } //初期化成否の確認
        //ユーザ一覧
        public List<string> UserList {
            get {
                return _ar.Select(o => o.User).ToList();
            }
        }

        public MailBox(Logger logger,Dat datUser,String dir){
            Status = true; //初期化状態 falseの場合は、初期化に失敗しているので使用できない

            _logger = logger;

            //MailBoxを配置するフォルダ
            Dir = dir;
            //Dir = kernel.ReplaceOptionEnv(Dir);

            try{
                Directory.CreateDirectory(Dir);
            } catch{

            }

            if (!Directory.Exists(Dir)){
                if (_logger != null){
                    _logger.Set(LogKind.Error, null, 9000029, string.Format("dir="));
                }
                Status = false;
                Dir = null;
                return; //以降の初期化を処理しない
            }

            //ユーザリストの初期化
            Init(datUser);

        }


        //ユーザリストの初期化
        private void Init(Dat datUser){
            _ar.Clear();
            if (datUser != null){
                foreach (var o in datUser) {
                    if (!o.Enable)
                        continue; //有効なデータだけを対象にする
                    var name = o.StrList[0];
                    var pass = Crypt.Decrypt(o.StrList[1]);
                    _ar.Add(new OneMailBox(name, pass));
                    var folder = string.Format("{0}\\{1}", Dir, name);
                    if (!Directory.Exists(folder)){
                        Directory.CreateDirectory(folder);
                    }
                }
            }
        }

        protected string CreateName(){
            lock (this){
                //Ver5.0.4 スレッドセーフの確保
                while (true){
                    var str = string.Format("{0:D20}", DateTime.Now.Ticks);
                    Thread.Sleep(1); //Ver5.0.4 ウエイトでDateTIme.Nowの重複を避ける
                    var fileName = string.Format("{0}\\MF_{1}", Dir, str);
                    if (!File.Exists(fileName)){
                        return str;
                    }
                }
            }
        }


        public bool Save(string user, Mail mail, MailInfo mailInfo){
            //Ver_Ml
            if (!IsUser(user)){
                if (_logger != null){
                    _logger.Set(LogKind.Error, null, 9000047, string.Format("[{0}] {1}", user, mailInfo));
                }
                return false;
            }

            var folder = string.Format("{0}\\{1}", Dir, user);
            if (!Directory.Exists(folder)){
                Directory.CreateDirectory(folder);
            }

            var name = CreateName();

            //logger.Set(LogKind.Debug,null,7777,name);

            string fileName = string.Format("{0}\\MF_{1}", folder, name);
            if (mail.Save(fileName)){
                fileName = string.Format("{0}\\DF_{1}", folder, name);
                mailInfo.Save(fileName);
                return true;
            }
            return false;
        }

        //ユーザが存在するかどうか
        public bool IsUser(string user){
            return _ar.Any(o => o.User == user);
        }

        //最後にログインに成功した時刻の取得 (PopBeforeSMTP用）
        public DateTime LastLogin(Ip addr){
            foreach (OneMailBox oneMailBox in _ar.Where(oneMailBox => oneMailBox.Addr == addr.ToString())){
                return oneMailBox.Dt;
            }
            return new DateTime(0);
        }

        //パスワード変更
        public bool Chps(string user, string pass,Conf conf){
            if (pass == null){
                //無効なパスワードの指定は失敗する
                return false;
            }
            if (_ar.Any(oneUser => oneUser.User == user)){
                var dat = (Dat) conf.Get("user");
                foreach (var o in dat.Where(o => o.StrList[0] == user)){
                    o.StrList[1] = Crypt.Encrypt(pass);
                    break;
                }
                conf.Set("user", dat); //データ変更
                Init(dat); //ユーザリストの初期化（再読込）
                return true;
            }
            return false;
        }

        //認証（パスワード確認) ※パスワードの無いユーザが存在する?
        public bool Auth(string user, string pass){
            foreach (var o in _ar){
                if (o.User == user){
                    return o.Pass == pass;
                }
            }
            return false;
        }

        //パスワード取得
        public string GetPass(string user){
            return (from oneUser in _ar where oneUser.User == user select oneUser.Pass).FirstOrDefault();
        }

        //認証（パスワード確認) APOP対応
        public bool Auth(string user, string authStr, string recvStr){
            foreach (OneMailBox o in _ar){
                if (o.User != user)
                    continue;
                if (o.Pass == null) //パスワードが無効
                    return false;

                var data = Encoding.ASCII.GetBytes(authStr + o.Pass);
                var md5 = new MD5CryptoServiceProvider();
                var result = md5.ComputeHash(data);
                var sb = new StringBuilder();
                for (int i = 0; i < 16; i++){
                    sb.Append(string.Format("{0:x2}", result[i]));
                }
                if (sb.ToString() == recvStr)
                    return true;
                return false;
            }
            return false;
        }

        public string Login(string user, Ip addr){
            foreach (var oneUser in _ar){
                if (oneUser.User != user)
                    continue;
                if (oneUser.Login(addr.ToString())){
                    return string.Format("{0}\\{1}", Dir, user);
                }
            }
            return null;
        }

        public void Logout(string user){
            foreach (var oneUser in _ar){
                if (oneUser.User == user){
                    oneUser.Logout();
                    return;
                }
            }
        }
    }
}
