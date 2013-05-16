using System;

using Bjd;
using Bjd.util;

namespace SmtpServer {
    class SmtpAuthClient {
        enum AuthType {
            Login = 0,
            Plain = 1,
            CramMd5 = 2,
            Unknown = 3
        }
        AuthType _authType = AuthType.Unknown;

        readonly string _user;
        readonly string _pass;
        bool _finish;//認証動作が終了しているかどうかのフラグ
        int _mode;
        public SmtpAuthClient(string user,string pass) {
            _user = user;
            _pass = pass;

            if (user == null)//ユーザ名が指定されていない場合、認証動作ができない
                _finish = true;
        }

        public void Ehlo(string str) {
            //250-AUTH NTLM LOGIN PLAIN DIGEST-MD5 CRAM-MD5
            var index = str.IndexOf("AUTH");
            
            //Ver5.5.9 SubStringのstartindexに文字列より大きな数字が入って例外が発生する問題に対処
            //if ( 0 <= index) {
            if ( 0 <= index && index < str.Length-5) {
                //動作モードの決定
                str = str.Substring(index + 5);
                if (str.IndexOf("CRAM-MD5") != -1)
                    _authType = AuthType.CramMd5;
                else if (str.IndexOf("LOGIN") != -1)
                    _authType = AuthType.Login;
                else if(str.IndexOf("PLAIN") != -1)
                    _authType = AuthType.Plain;
            }
        }

        public string Set(string recvStr) {
            if (_finish || _authType == AuthType.Unknown)//認証動作　もしくは　認証モードが未対応
                return null;
            int response = 0;
            if (recvStr.IndexOf(' ') == 3) {
                response = Convert.ToInt32(recvStr.Substring(0,3));
            }
            if (_authType == AuthType.Plain) {
                switch(_mode){
                    case 0:
                        _mode++;
                        return "AUTH PLAIN " + Base64.Encode(string.Format("\0{0}\0{1}", _user, _pass));
                    case 1:
                        if (response == 235) {
                            _mode++;
                            _finish = true;
                            return null;
                        }
                        if (response == 334) {
                            return Base64.Encode(string.Format("\0{0}\0{1}", _user, _pass));
                        }
                        break;
                }
            }
            if (_authType == AuthType.Login) {
                switch (_mode) {
                    case 0:
                        _mode++;
                        return "AUTH LOGIN";
                        //break;
                    case 1:
                        if (response == 334) {
                            _mode++;
                            return Base64.Encode(_user);
                        }
                        break;
                    case 2:
                        if (response == 334) {
                            _mode++;
                            return Base64.Encode(_pass);
                        }
                        break;
                    case 3:
                        if (response == 235) {
                            _mode++;
                            _finish = true;
                            return null;
                        }
                        break;
                }
            }
            if (_authType == AuthType.CramMd5) {
                switch (_mode) {
                    case 0:
                        _mode++;
                        return "AUTH CRAM-MD5";
                        //break;
                    case 1:
                        if (response == 334) {
                            _mode++;
                            if (recvStr.Length > 5) {
                                string timestamp = recvStr.Substring(4);//AUTH CRAM-MD5用のタイムスタンプ
                                string str = string.Format("{0} {1}",_user,Md5.Hash(_pass,Base64.Decode(timestamp)));
                                return Base64.Encode(str);
                            }
                        }
                        break;
                    case 2:
                        if (response == 235) {
                            _mode++;
                            _finish = true;
                            return null;
                        }
                        break;
                }
            }

            return null;
        }
    }
}
