using System.Collections.Generic;
using System.Collections;
using Bjd.ctrl;
using Bjd.mail;
using Bjd.option;
using Bjd.util;

namespace SmtpServer {
    //***************************************************************
    //ＭＬのユーザ一覧(参加者及び管理者)
    //***************************************************************
    class MlUserList : IEnumerable{

        //全ての参加者のデータベース
        readonly List<MlOneUser> _ar = new List<MlOneUser>();

        //通常のコンストラクタ
        public MlUserList(IEnumerable<OneDat> memberList) {
            foreach (var d in memberList) {
                var name = d.StrList[0]; //名前
                var mailAddress = new MailAddress(d.StrList[1]); //メールアドレス
                if (mailAddress.User == "" || mailAddress.Domain == "") {
                    continue;
                }
                var isManager = (d.StrList[2] == "True"); //管理者
                var isReader = (d.StrList[3] == "True"); //配信する
                var isContributor = (d.StrList[4] == "True"); //投稿者
                var password = Crypt.Decrypt(d.StrList[5]) ?? ""; //パスワード
                _ar.Add(new MlOneUser(d.Enable, name, mailAddress, isManager, isReader, isContributor,password));
            }
        }
        public Dat Export(){
            var dat = new Dat(new CtrlType[] { CtrlType.TextBox, CtrlType.TextBox, CtrlType.TextBox, CtrlType.TextBox, CtrlType.TextBox, CtrlType.TextBox });
            foreach (var o in _ar) {
                dat.Add(o.Enable, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", o.Name, o.MailAddress, o.IsManager, o.IsReader, o.IsContributor, Crypt.Encrypt(o.Psssword)));
            }
            return dat;
        }

        //イテレータ
        public IEnumerator GetEnumerator(){
            return _ar.GetEnumerator();
        }

        //検索
        public MlOneUser Search(MailAddress mailAddress) {
            for (var i = 0; i < _ar.Count; i++) {
                if (_ar[i].MailAddress.Compare(mailAddress)){
                    return !_ar[i].Enable ? null : _ar[i];
                }
            }
            return null;
        }
        //削除（unsubscribeによる）
        public Dat Del(MailAddress mailAddress) {
            var o = Search(mailAddress);
            if (o != null && !o.IsManager) {
                _ar.Remove(o);

                return Export();//更新 Update()が必要
            }
            return null;
        }
        //追加（subscribeによる）
        public Dat Add(MailAddress mailAddress, string name) {
            const bool enabled = true;
            var password = Crypt.Encrypt("");//パスワード
            const bool isManager = false; //管理者
            const bool isReader = true; //配信する
            const bool isContributor = true; //投稿者
            _ar.Add(new MlOneUser(enabled, name, mailAddress, isManager, isReader, isContributor, password));

            return Export();//更新 Update()が必要
        }
    }
}
