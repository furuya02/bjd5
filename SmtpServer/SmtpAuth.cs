using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Bjd.util;

namespace SmtpServer{
    internal class SmtpAuth{
        public bool IsFinish { get; private set; } //認証が完了しているかどうか

        private enum AuthType{
            Login = 0,
            Plain = 1,
            CramMd5 = 2,
            Unknown = 3
        }

        //SMTP認証リスト（内部でMailBoxをカプセリングしている）
        private readonly SmtpAuthUserList _smtpAuthUserList;

        private readonly bool _usePlain; //AUTH PLAIN の有効無効
        private readonly bool _useLogin; //AUTH LOGIN の有効無効
        private readonly bool _useCramMd5; //AUTH CRAM-MD5 の有効無効

        private AuthType _authType = AuthType.Unknown;

        //テンポラリ
        private String _timestamp;
        private String _user;



        public SmtpAuth(SmtpAuthUserList smtpAuthUserList, bool usePlain, bool useLogin, bool useCramMd5){

            _smtpAuthUserList = smtpAuthUserList;

            _usePlain = usePlain;
            _useLogin = useLogin;
            _useCramMd5 = useCramMd5;

            if (_usePlain || _useLogin || _useCramMd5){
                IsFinish = false; //認証未完了
            } else{
                IsFinish = true; //認証不要
            }
        }

        //EHLOリクエストに対するHELP文字列
        public String EhloStr(){
            if (!IsFinish){
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

        public string Job(String recvStr){
            if (_authType == AuthType.Unknown){
                return Before(recvStr);
            }
            return After(recvStr);
        }

        //AUTHコマンド前
        private String Before(String recvStr){
            var param = recvStr.Split(' ');
            if (param.Length >= 2 && param[0].ToUpper() == "AUTH"){
                switch (param[1]){
                    case "LOGIN":
                        if (_useLogin){
                            _authType = AuthType.Login;
                            //AUTH LOGIN [初期パラメータ]
                            if (param.Length == 3){
                                _user = Base64.Decode(param[2]);
                                return "334 UGFzc3dvcmQ6";
                            }
                            return "334 VXNlcm5hbWU6";
                        }
                        break;
                    case "PLAIN":
                        if (_usePlain){
                            _authType = AuthType.Plain;
                            if (param.Length == 3){
                                //同時にユーザ名＋パスワードが指定されている場合
                                var str = Base64.Decode(param[2]);
                                var tmp = str.Split('\0');
                                if (tmp.Length == 3){
                                    if (_smtpAuthUserList.Auth(tmp[1], tmp[2])){
                                        //認証OK
                                        IsFinish = true;
                                        return "235 Authentication successful.";
                                    }
                                }
                            } else{
                                _authType = AuthType.Plain;
                                return "334 ";
                            }
                        }
                        break;
                    case "CRAM-MD5":
                        if (_useCramMd5){
                            _authType = AuthType.CramMd5;
                            _timestamp = string.Format("{0}.{1}@{2}", Process.GetCurrentProcess().Id, DateTime.Now.Ticks, "domain");
                            return "334 " + Base64.Encode(_timestamp);
                        }
                        break;
                    default:
                        return "504 Unrecognized authentication type.";
                }
            }
            return string.Format("500 command not understood: {0}\r\n",recvStr);
        }
        
        //AUTHコマンド後
        private String After(String recvStr) {
            var pass = "";
            if (_authType == AuthType.Login){
                if (_user == null){
                    _user = Encoding.ASCII.GetString(Convert.FromBase64String(recvStr));
                    return "334 UGFzc3dvcmQ6";
                }
                pass = Encoding.ASCII.GetString(Convert.FromBase64String(recvStr));
                goto auth;
            }
            if (_authType == AuthType.Plain){
                recvStr = Encoding.ASCII.GetString(Convert.FromBase64String(recvStr));
                string[] tmp = recvStr.Split('\0');
                if (tmp.Length == 3){
                    _user = tmp[1];
                    pass = tmp[2];
                }
                goto auth;
            }
            if (_authType == AuthType.CramMd5){
                recvStr = Encoding.ASCII.GetString(Convert.FromBase64String(recvStr));
                var tmp = recvStr.Split(new[] { ' ' }, 2);
                if (tmp.Length == 2){
                    _user = tmp[0];
                    pass = _smtpAuthUserList.GetPass(_user);
                    if (pass != null){
                        var ret = Md5.Hash(pass, _timestamp);
                        if (ret == tmp[1]){
                            IsFinish = true;
                            return "235 Authentication successful.";
                        }
                    }
                }
            }
            return null;
        auth:
            if (_smtpAuthUserList.Auth(_user, pass)){
                //認証OK
                IsFinish = true;
                return "235 Authentication successful.";
            }
            return null;
        }
    }
}
