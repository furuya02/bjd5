using System;
using System.Collections.Generic;
using Bjd;
using Bjd.log;
using Bjd.mail;
using Bjd.net;
using Bjd.util;

namespace SmtpServer {
    class MailSave{
        readonly Kernel _kernel;
        readonly MailBox _mailBox;
        readonly Logger _logger;
        readonly MailQueue _mailQueue;
        readonly List<string> _domainList;
        int _idCounter;//id作成のための順次番号生成カウンタ
        readonly string _receivedHeader;//Receivedヘッダ文字列
        public MailSave(Kernel kernel,MailBox mailBox, Logger logger, MailQueue mailQueue, string receivedHeader, List<string> domainList) {
            _kernel = kernel;
            _mailBox = mailBox;
            _logger = logger;
            _mailQueue = mailQueue;
            _receivedHeader = receivedHeader;
            _domainList = domainList;

        }
        //Server及びMlから使用される
        //メールの保存(宛先はML以外であることが確定してから使用する)
        //テスト用のモックオブジェクト(TsMailSaveでSave()をオーバーライドできるようにvirtualにする
        virtual public bool Save(MailAddress from, MailAddress to, Mail orgMail, string host, Ip addr) {

            //Mailのヘッダ内容等を変更するので、この関数内だけの変更にとどめるため、テンポラリを作成する
            var mail = orgMail.CreateClone();

            //ユニークなID文字列の生成
            var uidStr = string.Format("bjd.{0:D20}.{1:D3}", DateTime.Now.Ticks, _idCounter++);
            //日付文字列の生成
            var date = Util.LocalTime2Str(DateTime.Now);

            //Receivedヘッダの追加
            var received = "";
            if (_receivedHeader != null) {
                received = _receivedHeader;

                //Ver5.0.0-b5 $aと$hをkernel.ChangeTag()に送る前に修正する
                received = Util.SwapStr("$a", addr.ToString(), received);
                received = Util.SwapStr("$h", host, received);

                received = Util.SwapStr("$i", uidStr, received);
                received = Util.SwapStr("$t", to.ToString(), received);
                received = Util.SwapStr("$d", date, received);
                received = _kernel.ChangeTag(received);
            }
            mail.AddHeader("Received", received);
            //Ver5.0.0-a12 To:ヘッダの書き換えは必要ない
            //mail.ConvertHeader("To", string.Format("<{0}>", to.ToString()));

            //Message-Idの追加
            if (null == mail.GetHeader("Message-ID"))
                mail.AddHeader("Message-ID", string.Format("<{0}@{1}>", uidStr, _domainList[0]));
            //Fromの追加
            if (null == mail.GetHeader("From"))
                mail.AddHeader("From", string.Format("<{0}>", @from));
            //Dateの追加
            if (null == mail.GetHeader("Date"))
                mail.AddHeader("Date", string.Format("{0}", date));

            //ローカル宛(若しくはローカルファイル)
            if (to.IsLocal(_domainList)) {

                //ローカル保存の場合は、X-UIDLを追加する
                mail.AddHeader("X-UIDL", uidStr);
                //ヘッダを追加してサイズが変わるので、ここで初期化する
                var mailInfo = new MailInfo(uidStr, mail.Length, host, addr, date, from, to);

                if (to.IsFile()) {  //ローカルファイルの場合(直接ファイルにAppendする)
                    if (mail.Append(to.ToString())) {
                        _logger.Set(LogKind.Normal, null, 21, string.Format("[{0}] {1}", to.User, mailInfo));
                    } else {
                        _logger.Set(LogKind.Error, null, 22, string.Format("[{0}] {1}", to.User, mailInfo));
                    }
                } else { //ローカルユーザの場合（メールボックスへSaveする）
                    if (!_mailBox.Save(to.User, mail, mailInfo))
                        return false;
                }

                _logger.Set(LogKind.Normal, null, 8, mailInfo.ToString());


            } else {

                //Toの追加
                if (null == mail.GetHeader("To")) {
                    mail.AddHeader("To", string.Format("<{0}>", to));
                }

                //ヘッダを追加してサイズが変わるので、ここで初期化する
                var mailInfo = new MailInfo(uidStr, mail.Length, host, addr, date, from, to);
                if (!_mailQueue.Save(mail, mailInfo)) {
                    return false;
                }
                _logger.Set(LogKind.Normal, null, 9, mailInfo.ToString());

            }
            return true;

        }
    }


}
