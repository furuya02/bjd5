using System;
using System.Collections.Generic;
using System.Text;
using Bjd.log;
using Bjd.mail;

namespace SmtpServer{
    public class Alias{

        private readonly Dictionary<String, String> _ar = new Dictionary<string, string>();
        private readonly List<string> _domainList;
        private readonly MailBox _mailBox;

        public Alias(List<string> domainList, MailBox mailBox){
            _domainList = domainList;
            _mailBox = mailBox;
            if (domainList == null || domainList.Count < 1){
                throw new Exception("Alias.cs Alias() domainList.Count<1");
            }
        }

        //�e�X�g�p logger��null�ł��
        public void Add(String name, String alias, Logger logger){
            System.Diagnostics.Debug.Assert(logger != null, "logger != null");
            
            //alias�̕�����ɖ������Ȃ����ǂ�����m�F����
            var tmp = alias.Split(',');
            var sb = new StringBuilder();
            foreach (var str in tmp){
                if (str.IndexOf('@') != -1){
                    //�O���[�o���A�h���X�̒ǉ�
                    sb.Append(str);
                    sb.Append(',');
                }else if (str.IndexOf('/') == 0){
                    //���[�J���t�@�C���̏ꍇ
                    sb.Append(str);
                    sb.Append(',');
                }else if (str.IndexOf('$') == 0){
                    //��`�̏ꍇ
                    if (str == "$ALL"){
                        if (_mailBox != null){
                            foreach (string user in _mailBox.UserList) {
                                sb.Append(string.Format("{0}@{1}", user, _domainList[0]));
                                sb.Append(',');
                            }
                        }
                    }else if (str == "$USER"){
                        //Ver5.4.3 $USER�ǉ�
                        sb.Append(string.Format("{0}@{1}", name, _domainList[0]));
                        sb.Append(',');
                    }else{
                        logger.Set(LogKind.Error, null, 45, string.Format("name:{0} alias:{1}", name, alias));
                    }
                }else{
                    if (_mailBox==null || !_mailBox.IsUser(str)){
                        //���[�U���͗L�����H
                        logger.Set(LogKind.Error, null, 19, string.Format("name:{0} alias:{1}", name, alias));
                    }else{
                        sb.Append(string.Format("{0}@{1}", str, _domainList[0]));
                        sb.Append(',');
                    }
                }
            }
            string buffer;
            if (_ar.TryGetValue(name, out buffer)){
                logger.Set(LogKind.Error, null, 30, string.Format("user:{0} alias:{1}", name, alias));
            }else{
                _ar.Add(name, sb.ToString());
            }
        }

        //�ݒ肳��Ă��郆�[�U�����ǂ���
        public bool IsUser(string user) {
            string buffer;
            return _ar.TryGetValue(user, out buffer);
        }


        //���惊�X�g�̕ϊ�
        //�e�X�g�p logger��null�ł��
        /*public RcptList Reflection(RcptList rcptList, Logger logger) {
            var ret = new RcptList();
            foreach(var mailAddress in rcptList){

                string buffer;
                if (mailAddress.IsLocal(_domainList) && _ar.TryGetValue(mailAddress.User, out buffer)) {
                    var lines = buffer.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(var line in lines) {
                        if (logger != null){
                            logger.Set(LogKind.Normal, null, 27, string.Format("{0} -> {1}", mailAddress, line));
                        }
                        ret.Add(new MailAddress(line));
                    }
                }else{
                    ret.Add(mailAddress);
                }
            }
            return ret;
        }*/
        public List<MailAddress> Reflection(List<MailAddress> list, Logger logger) {

            System.Diagnostics.Debug.Assert(logger != null, "logger != null");

            //var ret = new RcptList();
            var ret = new List<MailAddress>();

            foreach (var mailAddress in list) {

                string buffer;
                if (mailAddress.IsLocal(_domainList) && _ar.TryGetValue(mailAddress.User, out buffer)) {
                    var lines = buffer.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines) {
                        logger.Set(LogKind.Normal, null, 27, string.Format("{0} -> {1}", mailAddress, line));
                        ret.Add(new MailAddress(line));
                    }
                } else {
                    ret.Add(mailAddress);
                }
            }
            return ret;
        }

    }

}
