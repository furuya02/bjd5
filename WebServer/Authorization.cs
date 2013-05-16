using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Bjd;
using Bjd.log;
using Bjd.option;
using Bjd.util;

namespace WebServer {
    class Authorization {
        //readonly OneOption _oneOption;
        readonly Conf _conf;
        readonly Logger _logger;

        public Authorization(Conf conf, Logger logger) {
            //_oneOption = oneOption;
            _conf = conf;
            _logger = logger;

        }

        //送信されてきた認証情報（ユーザ＋パスワード）の取得
        bool CheckHeader(string authorization,ref string user,ref string pass) {

            if (authorization == null) {
                return false;
            }
            int index = authorization.IndexOf(' ');
            if (0 <= index) {

                var str = authorization.Substring(index + 1);
                //Ver5.0.0-b13
                //byte[] bytes = Convert.FromBase64String(str);
                //string s = Encoding.ASCII.GetString(bytes);
                var s = Base64.Decode(str);

                index = s.IndexOf(':');
                if (0 <= index) {
                    user = s.Substring(0, index);
                    pass = s.Substring(index + 1);
                    return true;
                }
            }
            return false;
        }

        public bool Check(string uri,string authorization,ref string authName){
            //認証リスト
            var authList = new AuthList((Dat)_conf.Get("authList"));
            
            //認証リストにヒットしているかどうかの確認
            var oneAuth = authList.Search(uri);
            if (oneAuth == null)
                return true;//認証リストにヒットなし

            //送信されてきた認証情報（ユーザ＋パスワード）の取得
            var user = "";
            var pass = "";
            if (!CheckHeader(authorization, ref user, ref pass))
                goto err;

            //認証リスト（AuthList）に当該ユーザの定義が存在するかどうか
            if (!oneAuth.Seartch(user)) {
                var find = false;//グループリストからユーザが検索できるかどうか
                //認証リストで直接ユーザ名を見つけられなかった場合、グループリストを検索する
                //グループリスト
                var groupList = new GroupList((Dat)_conf.Get("groupList"));
                foreach (OneGroup o in groupList){
                    if (!oneAuth.Seartch(o.Group))
                        continue;
                    if (!o.Seartch(user))
                        continue;
                    find = true;//一応ユーザとして認められている
                    break;
                }
                if (!find) {
                    _logger.Set(LogKind.Secure,null,6, string.Format("user:{0} pass:{1}", user, pass));//認証エラー（認証リストに定義されていないユーザからのアクセスです）";
                    goto err;
                }
            }
            //パスワードの確認
            var userList = new UserList((Dat)_conf.Get("userList"));
            var oneUser = userList.Search(user);
            if (oneUser == null) {
                //ユーザリストに情報が存在しない
                _logger.Set(LogKind.Secure,null,7,string.Format("user:{0} pass:{1}", user, pass));//認証エラー（ユーザリストに当該ユーザの情報がありません）";
            } else {
                if (oneUser.Pass == pass) {//パスワード一致
                    _logger.Set(LogKind.Detail,null, 8,string.Format("Authrization success user:{0} pass:{1}", user, pass));//認証成功
                    return true;
                }
                //パスワード不一致
                _logger.Set(LogKind.Secure,null,9,string.Format("user:{0} pass:{1}", user, pass));//認証エラー（パスワードが違います）";
            }
err:
            authName = oneAuth.AuthName;
            return false;//認証エラー発生
        }
        
        /***********************************************************/
        // 認証リスト
        /***********************************************************/
        class OneAuth {
            readonly List<string> _requireList = new List<string>();//ユーザ名とグループ名のリスト
            public OneAuth(string uri,string authName, string requires) {
                
                Uri = uri;
                AuthName = authName;

                foreach(string require in requires.Split(';')){
                    _requireList.Add(require);
                }
            }
            public string Uri { get; private set; }
            public string AuthName { get; private set; }
            //ユーザ。グループのリストにヒットが有るかどうかの検索
            public bool Seartch(string user) {
                if (_requireList.IndexOf(user) != -1)
                    return true;
                return false;
            }
        }
        class AuthList {

            readonly List<OneAuth> _ar = new List<OneAuth>();

            public AuthList(Dat authList) {
                foreach (var o in authList) {
                    if (!o.Enable)
                        continue;
                    string uri = o.StrList[0];
                    string authName = o.StrList[1];
                    string requires = o.StrList[2];
                    _ar.Add(new OneAuth(uri,authName,requires));
                }
            }

            //認証リストにヒットが有るかどうかの検索
            public OneAuth Search(string uri) {
                var sUri = uri.ToLower();
                foreach (OneAuth oneAuth in _ar) {

                    //Ver5.5.7
                    //if (uri.IndexOf(oneAuth.Uri)==0)
                    //    return oneAuth;
                    var sUri2 = oneAuth.Uri.ToLower();
                    if (sUri.IndexOf(sUri2)==0)
                        return oneAuth;
                    //Ver5.5.7
                    //Ver5.0.6
                    //if (uri.Length > 1 && uri[uri.Length - 1] != '/') {
                    //    uri = uri + '/';
                    //    if (uri.IndexOf(oneAuth.Uri) == 0)
                    //        return oneAuth;
                    //}
                    //Ver5.0.6
                    if (sUri.Length > 1 && sUri[sUri.Length - 1] != '/') {
                        sUri = sUri + '/';
                        if (sUri.IndexOf(sUri2) == 0)
                            return oneAuth;
                    }
                }
                return null;
            }

        }


        /***********************************************************/
        // ユーザリスト
        /***********************************************************/
        class OneUser {
            public OneUser(string user,string pass) {
                User = user;
                Pass = pass;
            }
            public string User { get; private set; }
            public string Pass { get; private set; }
        }

        class UserList {

            readonly List<OneUser> _ar = new List<OneUser>();

            public UserList(Dat userList) {
                foreach (var o in userList) {
                    if (!o.Enable)
                        continue;
                    var user = o.StrList[0];
                    var pass = Crypt.Decrypt(o.StrList[1]);
                    _ar.Add(new OneUser(user, pass));
                }
            }
            //ユーザリストにヒットが有るかどうかの検索
            public OneUser Search(string user){
                return _ar.FirstOrDefault(o => o.User == user);
            }
        }
        /***********************************************************/
        // グループリスト
        /***********************************************************/
        class OneGroup {
            readonly List<string> _userList = new List<string>();
            public OneGroup(string group,string users) {
                Group = group;
                foreach(var user in users.Split(';')){
                    _userList.Add(user);
                }
            }
            public string Group { get; private set; }

            //ユーザリストにヒットが有るかどうかの検索
            public bool Seartch(string user){
                return _userList.IndexOf(user) != -1;
            }
        }

        class GroupList : IEnumerable {

            readonly List<OneGroup> _ar = new List<OneGroup>();

            public GroupList(Dat groupList) {
                foreach (var o in groupList) {
                    if (!o.Enable)
                        continue;
                    var group = o.StrList[0];
                    var users = o.StrList[1];
                    _ar.Add(new OneGroup(group,users));
                }
            }
            //イテレータ
            public IEnumerator GetEnumerator(){
                return _ar.GetEnumerator();
            }
        }
    }
}
