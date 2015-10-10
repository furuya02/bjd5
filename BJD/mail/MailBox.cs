using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.util;

namespace Bjd.mail{

    public class MailBox{
        private readonly List<OneMailBox> _ar = new List<OneMailBox>();
        private Log _log;

        public string Dir { get; private set; } //���[���{�b�N�X�̃t�H���_
        public bool Status { get; private set; } //���������ۂ̊m�F
        //���[�U�ꗗ
        public List<string> UserList {
            get {
                return _ar.Select(o => o.User).ToList();
            }
        }

        public MailBox(Logger logger,Dat datUser,String dir){
            Status = true; //��������� false�̏ꍇ�́A�������Ɏ��s���Ă���̂Ŏg�p�ł��Ȃ�
            
            _log = new Log(logger);

            //MailBox��z�u����t�H���_
            Dir = dir;
            try{
                Directory.CreateDirectory(Dir);
            } catch(Exception){

            }

            if (!Directory.Exists(Dir)){
                _log.Set(LogKind.Error, null, 9000029, string.Format("dir="));
                Status = false;
                Dir = null;
                return; //�ȍ~�̏�������������Ȃ�
            }
            //���[�U���X�g�̏�����
            Init(datUser);
        }

        //���[�U���X�g�̏�����
        private void Init(IEnumerable<OneDat> datUser){
            _ar.Clear();
            if (datUser != null){
                foreach (var o in datUser) {
                    if (!o.Enable)
                        continue; //�L���ȃf�[�^������Ώۂɂ���
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

        protected string CreateFileName(){
            lock (this){
                while (true){
                    var str = string.Format("{0:D20}", DateTime.Now.Ticks);
                    //�X���b�h�Z�[�t�̊m��(�E�G�C�g��DateTIme.Now�̏d��������)
                    Thread.Sleep(1);
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
                _log.Set(LogKind.Error, null, 9000047, string.Format("[{0}] {1}", user, mailInfo));
                return false;
            }

            //�t�H���_�쐬
            var folder = string.Format("{0}\\{1}", Dir, user);
            if (!Directory.Exists(folder)){
                Directory.CreateDirectory(folder);
            }

            //�t�@�C��������
            var name = CreateFileName();
            var mfName = string.Format("{0}\\MF_{1}", folder, name);
            var dfName = string.Format("{0}\\DF_{1}", folder, name);
            
            //�t�@�C���ۑ�
            var success = false;
            try{
                if (mail.Save(mfName)){
                    if (mailInfo.Save(dfName)){
                        success = true;
                    }
                } else{
                    _log.Set(LogKind.Error, null, 9000059, mail.GetLastError());                    
                }
            }catch (Exception){
                ;
            }
            //���s�����ꍇ�́A�쐬�r���̃t�@�C����S���폜
            if (!success){
                if (File.Exists(mfName)) {
                    File.Delete(mfName);
                }
                if (File.Exists(dfName)) {
                    File.Delete(dfName);
                }
                return false;
            }
            //_logger.Set(LogKind.Normal, null, 8, mailInfo.ToString());

            return true;
        }

        //���[�U�����݂��邩�ǂ���
        public bool IsUser(string user){
            return _ar.Any(o => o.User == user);
        }

        //�Ō�Ƀ��O�C���ɐ������������̎擾 (PopBeforeSMTP�p�j
        public DateTime LastLogin(Ip addr){
            foreach (var oneMailBox in _ar.Where(oneMailBox => oneMailBox.Addr == addr.ToString())){
                return oneMailBox.Dt;
            }
            return new DateTime(0);
        }

        //�F�؁i�p�X���[�h�m�F) ���p�X���[�h�̖������[�U�����݂���?
        public bool Auth(string user, string pass){
            foreach (var o in _ar){
                if (o.User == user){
                    return o.Pass == pass;
                }
            }
            return false;
        }

        //�p�X���[�h�擾
        public string GetPass(string user){
            foreach (var oneUser in _ar){
                if (oneUser.User == user){
                    return oneUser.Pass;
                }
            }
            return null;
        }
        //�p�X���[�h�ύX pop3Server.Chps����g�p�����
        public bool SetPass(string user, string pass) {
            foreach (var oneUser in _ar) {
                if (oneUser.User == user) {
                    oneUser.SetPass(pass);
                    return true;
                }
            }
            return false;
        }

        
        public bool Login(string user, Ip addr) {
            foreach (var oneUser in _ar) {
                if (oneUser.User != user)
                    continue;
                if (oneUser.Login(addr.ToString())) {
                    return true;
                }
            }
            return false;
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
