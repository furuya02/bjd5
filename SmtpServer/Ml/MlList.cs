using System;
using System.Collections.Generic;
using System.Linq;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.option;

namespace SmtpServer {
    class MlList:IDisposable {
        readonly List<Ml> _ar = new List<Ml>();
        public MlList(Kernel kernel,Server server,MailSave mailSave, List<string> domainList) {

            var optionMl = kernel.ListOption.Get("Ml");

            //メーリングストの一覧を取得する
            var dat = (Dat) optionMl.GetValue("mlList");
            if (dat != null){
                foreach (var o2 in dat){
                    if (!o2.Enable)
                        continue;
                    //メーリングリスト名の読込
                    var mlName = o2.StrList[0];
                    var op = kernel.ListOption.Get("Ml-" + mlName);
                    var logger = kernel.CreateLogger(mlName, (bool) op.GetValue("useDetailsLog"), server);
                    var mlOption = new MlOption(kernel,op);
                    //無効なメンバ指定の確認と警告
                    foreach (var d in mlOption.MemberList){
                        var mailAddress = new MailAddress(d.StrList[1]); //メールアドレス
                        if (mailAddress.User != "" && mailAddress.Domain != "")
                            continue;
                        if (logger != null){
                            logger.Set(LogKind.Error, null, 53, string.Format("{0}", d.StrList[1]));
                        }
                    }
                    if (mlOption.MemberList.Count == 0){
                        logger.Set(LogKind.Error, null, 57, string.Format("{0}", mlName));
                        continue;
                    }
                    var ml = new Ml(kernel, logger, mailSave, mlOption, mlName, domainList);
                    //MLの管理領域の初期化に失敗している場合は、追加しない
                    if (!ml.Status)
                        continue;
                    _ar.Add(ml);
                    if (logger != null)
                        logger.Set(LogKind.Normal, null, 44, mlName);
                }
            }

        }
        //終了処理
        public void Dispose() {
            foreach(var ml in _ar) {
                ml.Dispose();//終了処理
            }
        }
        //有効なメーリングリスト名かどうかの確認
        public bool IsUser(MailAddress mailAddress){
            return _ar.Any(ml => ml.IsUser(mailAddress));
        }

        //当該メーリングリストに処理を分岐する
        public bool Job(MlEnvelope mlEnvelope,Mail mail) {
            foreach(var ml in _ar) {
                if (ml.IsUser(mlEnvelope.To)) {
                    return ml.Job(mlEnvelope,mail);
                }
            }
            return false;
        }

    }
}
