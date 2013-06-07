using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.acl;
using Bjd.option;
using Bjd.sock;
using Bjd.util;

namespace SmtpServer {
    class SmtpAuthServer {
        enum AuthType {
            Login = 0,
            Plain = 1,
            CramMd5 = 2,
            Unknown = 3
        }

        //SMTP認証リスト（内部でMailBoxをカプセリングしている）
        readonly SmtpAuthUserList _smtpAuthUserList;

        AuthType _authType = AuthType.Unknown;//認証方式 PLAIN LOGIN CRAM-MD5
        readonly bool _usePlain;//AUTH PLAIN の有効無効
        readonly bool _useLogin;//AUTH LOGIN の有効無効
        readonly bool _useCramMd5;//AUTH CRAM-MD5 の有効無効
        string _user = "";//AUTH LOGINのとき、２回の受信が必要なので、１回目の受信（ユーザ名）を保管する
        string _timestamp="";//CRAM-MD5でサーバから送信するタイムスタンプ

        public SmtpAuthServer(Logger logger, MailBox mailBox, Conf conf, SockTcp sockTcp) {

            Finish = true;//認証が完了しているかどうか（認証が必要ない場合はtrueを返す）
            var useEsmtp = (bool)conf.Get("useEsmtp");//ESMTPを使用するかどうか
            if(!useEsmtp)
                return;

            //適用範囲の確認
            var arV4 = new List<Acl>();
            var arV6 = new List<Acl>();
            foreach (var o in (Dat)conf.Get("range")) {
                if(o.Enable) {//有効なデータだけを対象にする
                    var name = o.StrList[0];
                    var ipStr = o.StrList[1];

                    if(ipStr.IndexOf('.') != -1) {//IPv4ルール
                        var acl = new AclV4(name,ipStr);
                        if(!acl.Status) {
                            logger.Set(LogKind.Error,null,9000040,string.Format("Name:{0} Address{1}",name,ipStr));
                        } else {
                            arV4.Add(acl);
                        }
                    } else {//IPv6ルール
                        var acl = new AclV6(name,ipStr);
                        if(!acl.Status) {
                            logger.Set(LogKind.Error,null,9000040,string.Format("Name:{0} Address{1}",name,ipStr));
                        } else {
                            arV6.Add(acl);
                        }
                    }
                }
            }
            //0:適用しない　1:適用する
            var enableEsmtp = (int)conf.Get("enableEsmtp");
            var ip = sockTcp.RemoteIp;
            Acl target = null;
            if(ip.InetKind == InetKind.V4) {//IPv4
                foreach (var p in arV4.Where(p => p.IsHit(ip))){
                    target = p;
                    break;
                }
            } else {//IPv6
                foreach (var p in arV6.Where(p => p.IsHit(ip))){
                    target = p;
                    break;
                }
            }
            if(enableEsmtp == 0 && target != null) {
                logger.Set(LogKind.Detail,null,26,string.Format("user:{0} address:{1}",target.Name,ip));
                return;//適用除外
            }
            if(enableEsmtp == 1 && target == null) {
                logger.Set(LogKind.Detail,null,26,string.Format("address:{0}",ip));
                return;//適用除外
            }

            _usePlain = (bool)conf.Get("useAuthPlain");
            _useLogin = (bool)conf.Get("useAuthLogin");
            _useCramMd5 = (bool)conf.Get("useAuthCramMD5");
            if (_usePlain || _useLogin || _useCramMd5) {
                Finish = false;//認証が必要
                var usePopAcount = (bool) conf.Get("usePopAcount");
                //usePopAcount==trueの時だけmailBoxへのリンクを渡す
                _smtpAuthUserList = new SmtpAuthUserList(usePopAcount?mailBox:null, (Dat)conf.Get("esmtpUserList"));
            }
        }
        public bool Finish { get; private set; }//認証が完了しているかどうか（認証が必要ない場合はtrueを返す）
        bool _isBusy;//認証中かどうか

        //EHLOリクエストに対するHELP文字列
        public string EhloStr() {
            if (!Finish) {
                var tmp = "250-AUTH";
                if (_useLogin)
                    tmp = tmp + " LOGIN";
                if (_usePlain)
                    tmp = tmp + " PLAIN";
                if (_useCramMd5)
                    tmp = tmp + " CRAM-MD5";
                return tmp;
            }
            return null;
        }

        //AUTHコマンドに対する処理
        public string SetType(List<string> paramList) {

            if (Finish) {
                return "503 Duplicate AUTH";
            }
            if (paramList.Count < 1) {
                return "504 Unrecognized authentication type.";
            }
            if (paramList[0].ToUpper() == "LOGIN") {
                if (_useLogin) {
                    _authType = AuthType.Login;
                    _isBusy = true; //認証中
                    //AUTH LOGIN [初期パラメータ]
                    if(paramList.Count == 2) {

                        _user = Base64.Decode(paramList[1]);
                        return "334 UGFzc3dvcmQ6";
                    }
                    return "334 VXNlcm5hbWU6";
                }
            }
            if (paramList[0] == "PLAIN") {
                if (_usePlain) {
                    if (paramList.Count == 2) {//同時にユーザ名＋パスワードが指定されている場合
                        var str = Base64.Decode(paramList[1]);
                        var tmp = str.Split('\0');
                        if (tmp.Length == 3) {
                            if (_smtpAuthUserList.Auth(tmp[1],tmp[2])) {//認証OK
                                _isBusy = false;//認証完了
                                Finish = true;
                                return "235 Authentication successful.";
                            }
                        }
                    } else {
                        _authType = AuthType.Plain;
                        _isBusy = true; //認証中
                        return "334 ";
                    }
                }
            }
            if (paramList[0] == "CRAM-MD5") {
                if (_useCramMd5) {
                    _authType = AuthType.CramMd5;
                    _isBusy = true; //認証中
                    _timestamp = string.Format("{0}.{1}@{2}",Process.GetCurrentProcess().Id,DateTime.Now.Ticks,"domain");
                    return "334 " + Base64.Encode(_timestamp);
                    //return "334 " + Base64.Encode(string.Format("{0}",timestamp),Encoding.ASCII);
                }
            }
            return "504 Unrecognized authentication type.";
        }
        //AUTH以降の受信に対する処理
        public string Set(string str) {
            var pass = "";
            if (Finish || !_isBusy)
                return null;
            if (_authType == AuthType.Login) {
                if (_user == "") {
                    _user = Encoding.ASCII.GetString(Convert.FromBase64String(str));
                    //user = Base64.Decode(str,Encoding.ASCII);
                    return "334 UGFzc3dvcmQ6";
                }
                pass = Encoding.ASCII.GetString(Convert.FromBase64String(str));
                //pass = Base64.Decode(str, Encoding.ASCII);
                goto auth;
            }
            if (_authType == AuthType.Plain) {
                str = Encoding.ASCII.GetString(Convert.FromBase64String(str));
                //str = Base64.Decode(str, Encoding.ASCII);
                string [] tmp = str.Split('\0');
                if (tmp.Length == 3) {
                    _user = tmp[1];
                    pass = tmp[2];
                }
                goto auth;
            }
            if (_authType == AuthType.CramMd5) {
                str = Encoding.ASCII.GetString(Convert.FromBase64String(str));
                //str = Base64.Decode(str, Encoding.ASCII);
                var tmp = str.Split(new[]{' '},2);
                if (tmp.Length == 2) {
                    _user = tmp[0];
                    pass = _smtpAuthUserList.GetPass(_user);
                    if (pass != null) {
                        var ret = Md5.Hash(pass,_timestamp);
                        if (ret == tmp[1]) {
                            _isBusy = false;//認証完了
                            Finish = true;
                            return "235 Authentication successful.";
                        }
                    }
                }
            }
            return null;
        auth:
            if (_smtpAuthUserList.Auth(_user,pass)) {//認証OK
                _isBusy = false;//認証完了
                Finish = true;
                return "235 Authentication successful.";
            }
            return null;
        }
    }
}
