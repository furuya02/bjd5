using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.net;
using Bjd.option;

namespace SmtpServer {
    class PopBeforeSmtp {
        //PopBeforeSmtpで認証されているかどうかのチェック
        bool CheckPopBeforeSmtp(Ip addr) {
            var usePopBeforeSmtp = (bool)conf.Get("usePopBeforeSmtp");
            if (usePopBeforeSmtp) {
                var span = DateTime.Now - kernel.MailBox.LastLogin(addr);//最終ログイン時刻からの経過時間を取得
                var sec = (int)span.TotalSeconds;//経過秒
                if (0 < sec && sec < (int)conf.Get("timePopBeforeSmtp")) {
                    return true;//認証されている
                }
            }
            return false;
        }
    }
}
