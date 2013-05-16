using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.util;

namespace SmtpServer {
    class Ml : IDisposable {
        public bool Status { get; private set; }//ステータス（管理領域の初期化に失敗した場合falseとなる）
        readonly Kernel _kernel;
        readonly Logger _logger;
        readonly MlMailDb _mlMailDb;
        readonly MlAddr _mlAddr;
        readonly MlUserList _mlUserList;
        readonly MlSender _mlSender;
        readonly MlCreator _mlCreator2;
        readonly MlSubscribeDb _mlSubscribeDb;
        readonly MlDelivery _mlDevivery;
        readonly bool _autoRegistration;
        public Ml(Kernel kernel, Logger logger, MailSave mailSave, MlOption mlOption,string mlName,List<string>domainList){

            Status = false;

            _kernel = kernel;
            _logger = logger;
            //_mlOption = mlOption;
            
            _mlMailDb = new MlMailDb(logger, mlOption.ManageDir, mlName);
            if (!_mlMailDb.Status) {
                return;//初期化中断
            }

            _mlAddr = new MlAddr(mlName, domainList);
            _mlUserList = new MlUserList(mlOption.MemberList);
            _mlSender = new MlSender(mailSave, logger);
            var mlSubject = new MlSubject(mlOption.TitleKind, mlName);
            _mlDevivery = new MlDelivery(mailSave, logger,_mlUserList, _mlAddr, _mlMailDb, mlSubject, mlOption.Docs,mlOption.MaxGet);
            _mlCreator2 = new MlCreator(_mlAddr,mlOption.Docs);
            _autoRegistration = mlOption.AutoRegistration;
            const double effectiveMsec = 120 * 1000; //有効時間120秒
            _mlSubscribeDb = new MlSubscribeDb(mlOption.ManageDir, mlName, effectiveMsec);//confirm文字列データベース
            if (!_mlMailDb.Status) {
                return;//初期化中断
            }
            Status = true;//ステータス
            
        }
        //終了処理
        public void Dispose() {
            _mlMailDb.Dispose();
            _mlSubscribeDb.Dispose();
        }

        //メール連番の取得
        public int Count() {
            return _mlMailDb.Count();
        }
        
        //メール情報の削除
        public void Remove() {
            _mlMailDb.Remove();
            _mlSubscribeDb.Remove();
        }
        //メール保存
        public bool Save(Mail mail) {
            return _mlMailDb.Save(mail);
        }
            
        //有効なあて先かどうかの確認
        public bool IsUser(MailAddress mailAddress) {
            return _mlAddr.IsUser(mailAddress);
        }

        //ＭＬメイン処理（投稿者の有効無効・管理者アドレス･制御アドレス宛の処理も、この中で分岐して行う）
        public bool Job(MlEnvelope mlEnvelope, Mail mail) {

            //メンバーの検索
            var mlOneUser = _mlUserList.Search(mlEnvelope.From);

            switch (_mlAddr.GetKind(mlEnvelope.To)) {
                case MlAddrKind.Post://「投稿アドレス」宛の処理
                    return JobMain(mlEnvelope, mail, mlOneUser);
                case MlAddrKind.Ctrl://「制御アドレス」宛の処理
                    JobCtrl(mlEnvelope, mail, mlOneUser);
                    return true;//MailBoxの処置としては成功
                case MlAddrKind.Admin://「管理者アドレス」宛の処理
                    return _mlDevivery.SendAllAdmin(mlEnvelope, mail);//全管理者への送信
            }
            return false;//無効アドレス
        }

        //「投稿アドレス」宛の処理
        bool JobMain(MlEnvelope mlEnvelope, Mail mail, MlOneUser mlOneUser) {

            //投稿者がメンバー以外若しくは投稿が許可されていない場合
            if (mlOneUser == null || !mlOneUser.IsContributor) {
                _logger.Set(LogKind.Detail, null, 34, string.Format("from:{0}", mlEnvelope.From));

                //投稿者にDenyメールを送信
                _mlDevivery.Deny(mail, mlEnvelope);

                //メールを添付して管理者へ
                var subject = string.Format("NOT MEMBER article from {0} ({1} ML)", mlEnvelope.From, _mlAddr.Name);
                return _mlDevivery.AttachToAmdin(mail, subject, mlEnvelope);
            }
            //投稿者が有効な場合
            _logger.Set(LogKind.Detail, null, 33, string.Format("from:{0}", mlEnvelope.From));
            //各メンバーへの配信
            return _mlDevivery.Post(mail, mlEnvelope);
        }

        //「制御アドレス」宛の処理
        void JobCtrl(MlEnvelope mlEnvelope, Mail mail, MlOneUser mlOneUser) {

            var adminLogin = false;//管理者としての認証が済んでいるかどうかのフラグ
            var log = new StringBuilder();//コマンドLog


            var mlCmd = new MlCmd(_logger, mail, mlOneUser);//コマンド解釈

            //var envelopeAdmin = mlEnvelope.ChangeFrom(mlAddr.Admin);
            var envelopeReturn = mlEnvelope.Swap().ChangeFrom(_mlAddr.Admin);

            //コマンドの処理
            foreach (OneMlCmd oneCmd in mlCmd) {

                //ログ出力
                var memberStr = (oneCmd.MlOneUser == null) ? "not member" : oneCmd.MlOneUser.MailAddress.ToString();
                var prompt = adminLogin ? "#" : "$";
                var logStr = string.Format("{0}>{1} {2} [{3}]", prompt, oneCmd.CmdKind.ToString().ToLower(), oneCmd.ParamStr, memberStr);
                log.Append(logStr + "\r\n");
                _logger.Set(LogKind.Detail, null, 41, logStr);

                if (mlOneUser == null) {
                    //メンバー外からのリクエストの場合、Guide以外は受け付けない
                    if (oneCmd.CmdKind != MlCmdKind.Guide) {
                        //投稿者にDenyメールを送信
                        _mlDevivery.Deny(mail, mlEnvelope);
                        break;
                    }
                }


                //権限確認 return "" エラーなし
                var errStr = "";
                switch (oneCmd.CmdKind) {
                    case MlCmdKind.Guide:
                        break;
                    case MlCmdKind.Exit:
                    case MlCmdKind.Quit:
                    case MlCmdKind.Members:
                    case MlCmdKind.Member:
                    case MlCmdKind.Summary:
                    case MlCmdKind.Subject:
                        break;
                    case MlCmdKind.Bye:
                    case MlCmdKind.Unsubscribe:
                        if (oneCmd.MlOneUser.IsManager) {
                            errStr = _kernel.IsJp() ? "管理者は、このコマンドを使用できません" : "cannot use a manager";
                        }
                        break;
                    case MlCmdKind.Subscribe:
                    case MlCmdKind.Confirm:
                        break;
                    case MlCmdKind.Password:
                    case MlCmdKind.Add:
                    case MlCmdKind.Del:
                        if (!oneCmd.MlOneUser.IsManager) {//管理者しか使用できない
                            errStr = _kernel.IsJp() ? "このコマンドは管理者しか使用できません" : "net administrator";
                        }
                        break;
                }

                if (errStr != "") {//権限に問題あり
                    _logger.Set(LogKind.Error, null, 47, errStr);
                    log.Append(errStr + "\r\n");


                    goto end;
                }
                //管理者ログイン確認
                if (!adminLogin) {
                    switch (oneCmd.CmdKind) {
                        case MlCmdKind.Del:
                        case MlCmdKind.Add:
                            _logger.Set(LogKind.Error, null, 50, "The certification is not over");
                            log.Append(errStr + "\r\n");
                            goto end;
                    }
                }

                //パラメータの処理
                MlParamSpan mlParamSpan = null;
                switch (oneCmd.CmdKind) {
                    case MlCmdKind.Get:
                    case MlCmdKind.Summary:
                    case MlCmdKind.Subject:
                        mlParamSpan = new MlParamSpan(oneCmd.ParamStr, _mlMailDb.Count());
                        if (mlParamSpan.Start == -1) {
                            errStr = _kernel.IsJp() ? "パラメータに矛盾がありあます" : "Appointment includes contradiction";
                            _logger.Set(LogKind.Error, null, 51, errStr);
                            log.Append(errStr + "\r\n");
                            
                            //ここエラーメールを返すようにする
                            _mlDevivery.Error(mlEnvelope, string.Format("ERROR \"{0} {1}\"", oneCmd.CmdKind.ToString().ToUpper(),oneCmd.ParamStr));
                            goto end;
                        }
                        break;
                }


                switch (oneCmd.CmdKind) {
                    case MlCmdKind.Bye:
                    case MlCmdKind.Unsubscribe:
                        //メンバーの削除
                        using (var dat = _mlUserList.Del(mlEnvelope.From)){
                            if (dat == null){
                                errStr = _kernel.IsJp() ? "メンバーの削除に失敗しました" : "Failed in delete of a member";
                            }
                        }
                        break;
                    case MlCmdKind.Password:
                        if (mlOneUser.Psssword == oneCmd.ParamStr) {
                            adminLogin = true;//管理者として認証
                        } else {
                            errStr = _kernel.IsJp() ? "パスワードが違います" : "A password is different";
                        }
                        break;
                    case MlCmdKind.Del:
                        var tmp = oneCmd.ParamStr.Split(new char[] { ' ' }, 2);
                        var mailAddress = new MailAddress(tmp[0]);

                        //メンバーの削除
                        using (var dat = _mlUserList.Del(mailAddress)){
                            if (dat == null){
                                errStr = _kernel.IsJp() ? "メンバーの削除に失敗しました" : "Failed in addition of a member";
                            }
                        }
                        break;
                    case MlCmdKind.Add:
                        //メンバーの追加
                        var tmp2 = oneCmd.ParamStr.Split(new char[] { ' ' }, 2);
                        var mailAddress2 = new MailAddress(tmp2[0]);
                        if (null != _mlUserList.Search(mailAddress2)) {
                            errStr = _kernel.IsJp() ? "既にメンバーが登録されています" : "There is already a member";
                        } else {
                            using(var dat = _mlUserList.Add(mailAddress2, tmp2[1])){
                                if (dat == null) {//メンバーの追加
                                    errStr = _kernel.IsJp() ? "メンバーの追加に失敗しました" : "Failed in addition of a member";
                                }
                            }
                        }
                        break;
                    case MlCmdKind.Subscribe: {
                            var oneSubscribe = _mlSubscribeDb.Search(mlEnvelope.From);
                            if (oneSubscribe == null) {
                                oneSubscribe = _mlSubscribeDb.Add(mlEnvelope.From, oneCmd.ParamStr);//subscribeDbへの追加
                            }
                            var confirmStr = string.Format("confirm {0} {1}", oneSubscribe.ConfirmStr, oneSubscribe.Name);
                            _mlSender.Send(envelopeReturn, _mlCreator2.Confirm(confirmStr));
                            log.Length = 0;//ログメールの送信抑制
                        }
                        break;
                    case MlCmdKind.Confirm: {
                            var success = false;
                            var oneSubscribe = _mlSubscribeDb.Search(mlEnvelope.From);
                            if (oneSubscribe != null) {
                                //confirm行の検索
                                var confirmStr = string.Format("confirm {0} {1}", oneSubscribe.ConfirmStr, oneSubscribe.Name);
                                var lines = Inet.GetLines(mail.GetBody());
                                foreach (var line in lines) {
                                    var str = mail.GetEncoding().GetString(line);
                                    if (str.IndexOf(confirmStr) != -1) {
                                        success = true;//認証成功

                                        _mlSubscribeDb.Del(mlEnvelope.From);//subscribeDbの削除

                                        if (_autoRegistration) {//自動登録の場合
                                            //メンバーの追加
                                            using (var dat = _mlUserList.Add(mlEnvelope.From, oneSubscribe.Name)) {
                                                if (dat != null){
                                                    //Welcodeメールの送信
                                                    _mlSender.Send(envelopeReturn, _mlCreator2.Welcome());
                                                    _logger.Set(LogKind.Detail, null, 46, mlEnvelope.From.ToString());

                                                    //オプションのUpDate(dat);

                                                }
                                                else{
                                                    _logger.Set(LogKind.Detail, null, 48, mlEnvelope.From.ToString());
                                                }
                                            }
                                        } else {
                                            //管理者による登録
                                            //管理者宛にconfirmが有ったことを連絡する
                                            var mlenv = mlEnvelope.ChangeFrom(_mlAddr.Admin).ChangeTo(_mlAddr.Admin);
                                            var appendStr = string.Format("{0} {1}", mlEnvelope.From, oneSubscribe.Name);
                                            _mlSender.Send(mlenv, _mlCreator2.Append(appendStr));

                                        }
                                    }
                                }
                            }
                            if (!success) { //認証失敗
                                _mlSender.Send(envelopeReturn, _mlCreator2.Guide());
                            }
                        }
                        break;
                    case MlCmdKind.Members:
                    case MlCmdKind.Member:
                        var sb = new StringBuilder();
                        foreach (var o in from MlOneUser o in _mlUserList where !o.IsManager select o){
                            sb.Append(o.MailAddress + "\r\n");
                        }
                        _mlSender.Send(envelopeReturn, _mlCreator2.Member(sb.ToString()));
                        break;
                    case MlCmdKind.Get:
                        _mlDevivery.Get(mail, mlEnvelope, mlParamSpan);
                        break;
                    case MlCmdKind.Summary:
                    case MlCmdKind.Subject:
                        _mlDevivery.Summary(mail, mlEnvelope, mlParamSpan);
                        break;
                    case MlCmdKind.Guide:
                        _mlDevivery.Doc(MlDocKind.Guide, mail, mlEnvelope);
                        break;
                    case MlCmdKind.Help:
                        _mlDevivery.Doc(mlOneUser.IsManager ? MlDocKind.Admin : MlDocKind.Help, mail, mlEnvelope);
                        break;
                    case MlCmdKind.Exit:
                    case MlCmdKind.Quit:
                        goto end;//コマンド終了
                }
                if (errStr != ""){
                    //コマンド実行にエラー発生
                    log.Append(string.Format("error! {0}\r\n", errStr));
                    _logger.Set(LogKind.Error, null, 49, errStr);
                    goto end;
                }
                log.Append("success!\r\n");
            }
        end:
            ;
            //ログが必要なコマンド時だけログを送信する
            //if (log.Length != 0) {
            //    mlSender2.Send(envelopeAdmin,mlCreator2.Log(log.ToString()));
            //}
        }
    }
}