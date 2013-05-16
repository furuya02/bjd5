using System.Collections.Generic;
using System.Collections;
using Bjd;

namespace SmtpServer {
    //***************************************************************
    //ＭＬのユーザ一覧(参加者及び管理者)
    //***************************************************************
    class MlUsers : IEnumerable {
        readonly Kernel kernel = null;
        readonly Logger logger = null;
        readonly OneOption op = null;

        //全ての参加者のデータベース
        readonly List<OneUser> ar = new List<OneUser>();

        //通常のコンストラクタ
        public MlUsers(Kernel kernel, Logger logger, OneOption op) {
            this.kernel = kernel;
            this.logger = logger;
            this.op = op;
            Init((Dat)op.GetValue("memberList"));//初期化
        }
        //テスト用コンストラクタ
        public MlUsers(Dat memberList) {
            Init(memberList);//初期化
        }
        void Init(Dat dat) {
            foreach (var o in dat) {
                string name = o.StrList[0];//名前
                MailAddress mailAddress = new MailAddress(o.StrList[1]);//メールアドレス
                if (mailAddress.User == "" || mailAddress.Domain == "") {
                    if (logger != null) {
                        logger.Set(LogKind.Error, null, 53, string.Format("{0}", o.StrList[1]));
                    }
                }
                var isManager = (o.StrList[2] == "True" ? true : false);//管理者
                var isReader = (o.StrList[3] == "True" ? true : false);//配信する
                var isContributor = (o.StrList[4] == "True" ? true : false);//投稿者
                var password = Crypt.Decrypt(o.StrList[5]);//パスワード
                if (password == null)
                    password = "";
                ar.Add(new OneUser(o.Enable, name, mailAddress, isManager, isReader, isContributor, password));
            }
        }
        //イテレータ
        public IEnumerator GetEnumerator() {
            for (int i = 0; i < ar.Count; i++) {
                yield return ar[i];
            }
        }
        //検索
        public OneUser Search(MailAddress mailAddress) {
            for (int i = 0; i < ar.Count; i++) {
                if (ar[i].MailAddress.Compare(mailAddress)) {
                    if (!ar[i].Enable)//無効ユーザは、検索の時点でヒットなしにする
                        return null;
                    return ar[i];
                }
            }
            return null;
        }
        //削除（unsubscribeによる）
        public bool Del(MailAddress mailAddress) {
            var o = Search(mailAddress);
            if (o != null && !o.IsManager) {
                ar.Remove(o);
                Update();//更新
            }
            return true;
        }
        //追加（subscribeによる）
        public bool Add(MailAddress mailAddress, string name) {
            bool enabled = true;
            string password = Crypt.Encrypt("");//パスワード
            bool isManager = false;//管理者
            bool isReader = true;//配信する
            bool isContributor = true;//投稿者
            ar.Add(new OneUser(enabled, name, mailAddress, isManager, isReader, isContributor, password));
            Update();//更新
            return true;
        }
        //更新
        void Update() {
            if (kernel != null) {
                Dat dat = new Dat();
                foreach (var o in ar) {
                    dat.Add(o.Enable, string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", o.Name, o.MailAddress.ToString(), o.IsManager, o.IsReader, o.IsContributor, Crypt.Encrypt(o.Psssword)));
                }
                op.SetVal("memberList", dat);
                kernel.OptionRead2(op.NameTag);//オプションの再読み込み
            }
        }
    }

}
