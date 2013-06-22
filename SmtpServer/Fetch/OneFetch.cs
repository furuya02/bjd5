using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.net;

namespace SmtpServer {
    //自動受信オプション
    public class OneFetch{
        public int Interval { get; private set; }//受信間隔(分)
        public Ip Ip { get; private set; }//サーバ
        private readonly String _host;
        public int Port { get; private set; }//ポート
        public string User { get; private set; }//ユーザ
        public string Pass { get; private set; }//パスワード
        public string LocalUser { get; private set; }//ローカルユーザ
        public int Synchronize { get; private set; }//同期
        public int KeepTime { get; private set; }//サーバに残す時間（分）
        public OneFetch(int interval, string host, int port, string user, string pass, string localUser, int synchronize, int keepTime){
            Interval = interval;
            _host = host;
            Port = port;
            User = user;
            Pass = pass;
            LocalUser = localUser;
            Synchronize = synchronize;
            KeepTime = keepTime;

            Ip = null;
            try{
                Ip = new Ip(_host);
            } catch (Exception){
                var tmp = Lookup.QueryA(_host);
                try{
                    if (tmp.Count > 0)
                        Ip = new Ip(tmp[0]);
                } catch (Exception){
                    Ip = null;
                }

            }
        }
        //ログ等表示用
        public override String ToString(){
            return String.Format("{0}:{1} USER:{2} => LOCAL:{3}", _host, Port, User, LocalUser);
        }
        
        //データベース作成用の名前
        public String Name{
            get{
                return String.Format("{0}.{1}.{2}.{3}", _host, Port, User, LocalUser);
            }
        }
    }
}
