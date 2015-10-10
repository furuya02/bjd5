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

        //���M����Ă����F�؏��i���[�U�{�p�X���[�h�j�̎擾
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
            //�F�؃��X�g
            var authList = new AuthList((Dat)_conf.Get("authList"));
            
            //�F�؃��X�g�Ƀq�b�g���Ă��邩�ǂ����̊m�F
            var oneAuth = authList.Search(uri);
            if (oneAuth == null)
                return true;//�F�؃��X�g�Ƀq�b�g�Ȃ�

            //���M����Ă����F�؏��i���[�U�{�p�X���[�h�j�̎擾
            var user = "";
            var pass = "";
            if (!CheckHeader(authorization, ref user, ref pass))
                goto err;

            //�F�؃��X�g�iAuthList�j�ɓ��Y���[�U�̒�`�����݂��邩�ǂ���
            if (!oneAuth.Seartch(user)) {
                var find = false;//�O���[�v���X�g���烆�[�U�������ł��邩�ǂ���
                //�F�؃��X�g�Œ��ڃ��[�U����������Ȃ������ꍇ�A�O���[�v���X�g���������
                //�O���[�v���X�g
                var groupList = new GroupList((Dat)_conf.Get("groupList"));
                foreach (OneGroup o in groupList){
                    if (!oneAuth.Seartch(o.Group))
                        continue;
                    if (!o.Seartch(user))
                        continue;
                    find = true;//�ꉞ���[�U�Ƃ��ĔF�߂��Ă���
                    break;
                }
                if (!find) {
                    _logger.Set(LogKind.Secure,null,6, string.Format("user:{0} pass:{1}", user, pass));//�F�؃G���[�i�F�؃��X�g�ɒ�`����Ă��Ȃ����[�U����̃A�N�Z�X�ł��j";
                    goto err;
                }
            }
            //�p�X���[�h�̊m�F
            var userList = new UserList((Dat)_conf.Get("userList"));
            var oneUser = userList.Search(user);
            if (oneUser == null) {
                //���[�U���X�g�ɏ�񂪑��݂��Ȃ�
                _logger.Set(LogKind.Secure,null,7,string.Format("user:{0} pass:{1}", user, pass));//�F�؃G���[�i���[�U���X�g�ɓ��Y���[�U�̏�񂪂���܂���j";
            } else {
                if (oneUser.Pass == pass) {//�p�X���[�h��v
                    _logger.Set(LogKind.Detail,null, 8,string.Format("Authrization success user:{0} pass:{1}", user, pass));//�F�ؐ���
                    return true;
                }
                //�p�X���[�h�s��v
                _logger.Set(LogKind.Secure,null,9,string.Format("user:{0} pass:{1}", user, pass));//�F�؃G���[�i�p�X���[�h���Ⴂ�܂��j";
            }
err:
            authName = oneAuth.AuthName;
            return false;//�F�؃G���[����
        }
        
        /***********************************************************/
        // �F�؃��X�g
        /***********************************************************/
        class OneAuth {
            readonly List<string> _requireList = new List<string>();//���[�U���ƃO���[�v���̃��X�g
            public OneAuth(string uri,string authName, string requires) {
                
                Uri = uri;
                AuthName = authName;

                foreach(string require in requires.Split(';')){
                    _requireList.Add(require);
                }
            }
            public string Uri { get; private set; }
            public string AuthName { get; private set; }
            //���[�U�B�O���[�v�̃��X�g�Ƀq�b�g���L�邩�ǂ����̌���
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

            //�F�؃��X�g�Ƀq�b�g���L�邩�ǂ����̌���
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
        // ���[�U���X�g
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
            //���[�U���X�g�Ƀq�b�g���L�邩�ǂ����̌���
            public OneUser Search(string user){
                return _ar.FirstOrDefault(o => o.User == user);
            }
        }
        /***********************************************************/
        // �O���[�v���X�g
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

            //���[�U���X�g�Ƀq�b�g���L�邩�ǂ����̌���
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
            //�C�e���[�^
            public IEnumerator GetEnumerator(){
                return _ar.GetEnumerator();
            }
        }
    }
}
