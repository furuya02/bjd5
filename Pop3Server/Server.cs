using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;



using Bjd;
using Bjd.acl;
using Bjd.log;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;

namespace Pop3Server {

    partial class Server : OneServer
    {
        readonly AttackDb _attackDb;//自動拒否

        //コンストラクタ
        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel, conf,oneBind) {

            //Ver5.8.9
            if (kernel.RunMode == RunMode.Normal || kernel.RunMode == RunMode.Service){
                //メールボックスの初期化状態確認
                if (kernel.MailBox == null || !kernel.MailBox.Status) {
                    Logger.Set(LogKind.Error, null, 4, "");
                }
            }

            var useAutoAcl = (bool)Conf.Get("useAutoAcl");// ACL拒否リストへ自動追加する
            if (!useAutoAcl)
                return;
            var max = (int)Conf.Get("autoAclMax");// 認証失敗数（回）
            var sec = (int)Conf.Get("autoAclSec");// 対象期間(秒)
            _attackDb = new AttackDb(sec, max);
        }
        //リモート操作（データの取得）
        override public string Cmd(string cmdStr) {
            return "";
        }

        enum Pop3LoginState {
            User=0,//USER/APOP待ち状態
            Pass=1,//パスワード待ち状態
            Login=2//ログイン中

        }

        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();

        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }
        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {

            var sockTcp = (SockTcp)sockObj;

            var pop3LoginState = Pop3LoginState.User;

            var authType = (int)Conf.Get("authType");// 0=USER/PASS 1=APOP 2=両方
            var useChps = (bool)Conf.Get("useChps");//パスワード変更[CPHS]の使用・未使用

            
            string user=null;

            //グリーティングメッセージの表示
            var bannerMessage = Kernel.ChangeTag((string)Conf.Get("bannerMessage"));
            
            var authStr = "";//APOP用の認証文字列
            if(authType==0){//USER/PASS
                sockTcp.AsciiSend("+OK " + bannerMessage);
            }else{//APOP
                var random = new Random();
                authStr = string.Format("<{0}.{1}@{2}>",random.Next(GetCurrentThreadId()),DateTime.Now.Ticks,Kernel.ServerName);
                sockTcp.AsciiSend("+OK " + bannerMessage + " " + authStr);
    
            }

            //メールボックスにログインして、その時点のメールリストを取得する
            //実際のメールの削除は、QUIT受信時に、mailList.Update()で処理する
            MessageList messageList = null;

            while (IsLife()) {
                //このループは最初にクライアントからのコマンドを１行受信し、最後に、
                //sockCtrl.LineSend(resStr)でレスポンス処理を行う
                //continueを指定した場合は、レスポンスを返さずに次のコマンド受信に入る（例外処理用）
                //breakを指定した場合は、コネクションの終了を意味する（QUIT ABORT 及びエラーの場合）

                Thread.Sleep(0);

                var str = "";
                var cmdStr = "";

                var remoteIp = new Ip(sockTcp.RemoteAddress.Address.ToString());

                var paramStr2 = "";
                if (!RecvCmd(sockTcp, ref str, ref cmdStr, ref paramStr2))
                    break;//切断された

                if (str == "waiting") {
                    Thread.Sleep(100);//受信待機中
                    continue;
                }

                //コマンド文字列の解釈
                var cmd = Pop3Cmd.Unknown;
                foreach (Pop3Cmd n in Enum.GetValues(typeof(Pop3Cmd))) {
                    if (n.ToString().ToUpper() == cmdStr.ToUpper()) {
                        cmd = n;
                        break;
                    }
                }
                if (cmd == Pop3Cmd.Unknown) {//無効コマンド
                    goto UNKNOWN;
                }
                
                //パラメータ分離
                var paramList = new List<string>();
                if(paramStr2!=null){
                    paramList.AddRange(paramStr2.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim(' ')));
                }

                //いつでも受け付ける
                if (cmd == Pop3Cmd.Quit) {
                    if (messageList != null) {
                        messageList.Update();//ここで削除処理が実行される
                    }
                    goto END;
                }

                if (pop3LoginState == Pop3LoginState.User) {
                    
                    if (cmd == Pop3Cmd.User && (authType == 0 || authType == 2)) {
                        if (paramList.Count < 1) {
                            goto FEW;
                        }
                        user = paramList[0];
                        pop3LoginState = Pop3LoginState.Pass;
                        sockTcp.AsciiSend(string.Format("+OK Password required for {0}.",user));
                    } else if (cmd == Pop3Cmd.Apop && (authType == 1 || authType == 2)) {  //APOP
                        if (paramList.Count<2) {
                            goto FEW;
                        }
                        user = paramList[0];
    
                        var success = Kernel.MailBox.Auth(user, authStr, paramList[1]);//認証(APOP対応)
                        AutoDeny(success, remoteIp);//ブルートフォース対策
                        if(success) {
                            if (!Login(sockTcp, ref pop3LoginState, ref messageList, user, new Ip(sockObj.RemoteAddress.Address.ToString())))
                                goto END;
                        } else {
                            AuthError(sockTcp, user, paramList[1]);
                            goto END;
                        }
                    } else {
                        goto UNKNOWN;
                    }
                } else if (pop3LoginState == Pop3LoginState.Pass) {
                    if (cmd != Pop3Cmd.Pass) {
                        goto UNKNOWN;
                    }

                    if (paramList.Count<1) {
                        goto FEW;
                    }
                    string pass = paramList[0];

                    var success = Kernel.MailBox.Auth(user, pass);//認証
                    AutoDeny(success, remoteIp);//ブルートフォース対策
                    if (success) {//認証
                        if (!Login(sockTcp, ref pop3LoginState, ref messageList, user, new Ip(sockObj.RemoteAddress.Address.ToString())))
                            goto END;
                    } else {
                        AuthError(sockTcp, user, pass);
                        goto END;
                    }
                } else if (pop3LoginState == Pop3LoginState.Login) {

                    if (cmd == Pop3Cmd.Dele || cmd == Pop3Cmd.Retr) {
                        if (paramList.Count < 1)
                            goto FEW;
                    }
                    if (cmd == Pop3Cmd.Top) {
                        if (paramList.Count < 2)
                            goto FEW;
                    }

                    int index = -1;//メール連番
                    if (cmd!=Pop3Cmd.Chps && 1 <= paramList.Count) {
                        try{
                            index = Convert.ToInt32(paramList[0]);
                        } catch (Exception ex){
                            sockTcp.AsciiSend("-ERR Invalid message number.");
                            continue;
                        }
                        
                        index--;
                        if (index < 0 || messageList.Max <= index) {
                            sockTcp.AsciiSend(string.Format("-ERR Message {0} does not exist.",index + 1));
                            continue;
                        }
                    }
                    int count = -1;//TOP 行数
                    if (cmd != Pop3Cmd.Chps && 2 <= paramList.Count) {
                        try{
                            count = Convert.ToInt32(paramList[1]);
                        } catch (Exception ex){
                            sockTcp.AsciiSend("-ERR Invalid line number.");
                            continue;
                        }
                        if (count < 0) {
                            sockTcp.AsciiSend(string.Format("-ERR Linenumber range over: {0}",count));
                            continue;
                        }
                    }

                    if (cmd == Pop3Cmd.Noop) {
                        sockTcp.AsciiSend("+OK");
                        continue;
                    }
                    if (cmd == Pop3Cmd.Stat) {
                        sockTcp.AsciiSend(string.Format("+OK {0} {1}",messageList.Count,messageList.Size));
                        continue;
                    }
                    if (cmd == Pop3Cmd.Rset) {
                        messageList.Rset();
                        sockTcp.AsciiSend(string.Format("+OK {0} has {1} message ({2} octets).",user,messageList.Count,messageList.Size));
                        continue;
                    }
                    if (cmd == Pop3Cmd.Dele) {
                        if (messageList[index].Del) {
                            sockTcp.AsciiSend(string.Format("-ERR Message {0} has been markd for delete.",index + 1));
                            continue;
                        }
                        messageList[index].Del = true;
                        //Ver5.0.3
                        //sockTcp.AsciiSend(string.Format("+OK {0} octets",messageList.Size),OPERATE_CRLF.YES);
                        sockTcp.AsciiSend(string.Format("+OK {0} octets",messageList[index].Size));
                        continue;
                    }
                    if (cmd == Pop3Cmd.Uidl || cmd == Pop3Cmd.List){
                        if (paramList.Count<1) {
                            sockTcp.AsciiSend(string.Format("+OK {0} message ({1} octets)",messageList.Count,messageList.Size));
                            for (int i = 0; i < messageList.Max; i++) {
                                if (!messageList[i].Del) {
                                    if (cmd == Pop3Cmd.Uidl)
                                        sockTcp.AsciiSend(string.Format("{0} {1}",i + 1,messageList[i].Uid));
                                    else //LIST
                                        sockTcp.AsciiSend(string.Format("{0} {1}",i + 1,messageList[i].Size));
                                }
                            }
                            sockTcp.AsciiSend(".");
                            continue;
                        }
                        if (cmd == Pop3Cmd.Uidl)
                            sockTcp.AsciiSend(string.Format("+OK {0} {1}",index + 1,messageList[index].Uid));
                        else //LIST
                            sockTcp.AsciiSend(string.Format("+OK {0} {1}",index + 1,messageList[index].Size));
                    }
                    if (cmd == Pop3Cmd.Top || cmd == Pop3Cmd.Retr) {
                        //OneMessage oneMessage = messageList[index];
                        sockTcp.AsciiSend(string.Format("+OK {0} octets",messageList[index].Size));
                        if(!messageList[index].Send(sockTcp,count)) {//メールの送信
                            break;
                        }
                        MailInfo mailInfo = messageList[index].GetMailInfo();
                        Logger.Set(LogKind.Normal,sockTcp,5,mailInfo.ToString());

                        sockTcp.AsciiSend(".");
                        continue;

                    }
                    if (cmd == Pop3Cmd.Chps) {
                        if (!useChps)
                            goto UNKNOWN;
                        if (paramList.Count < 1)
                            goto FEW;

                        var password = paramList[0];

                        //最低文字数
                        var minimumLength = (int)Conf.Get("minimumLength");
                        if(password.Length < minimumLength){
                            sockTcp.AsciiSend("-ERR The number of letter is not enough.");
                            continue;
                        }
                        //ユーザ名と同一のパスワードを許可しない
                        if ((bool)Conf.Get("disableJoe")) {
                            if (user.ToUpper() == password.ToUpper()) {
                                sockTcp.AsciiSend("-ERR Don't admit a JOE.");
                                continue;
                            }
                        }

                        //必ず含まなければならない文字のチェック
                        bool checkNum=false;
                        bool checkSmall=false;
                        bool checkLarge=false;
                        bool checkSign=false;
                        foreach (char c in password){
                            if('0'<= c && c<='9')
                                checkNum=true;
                            else if('a'<= c && c<='z')
                                checkSmall=true;
                            else if('A'<= c && c<='Z')
                                checkLarge=true;
                            else
                                checkSign=true;
                        }
                        if(((bool)Conf.Get("useNum") && !checkNum)||
                           ((bool)Conf.Get("useSmall") && !checkSmall) ||
                           ((bool)Conf.Get("useLarge") && !checkLarge) ||
                           ((bool)Conf.Get("useSign") && !checkSign)) {
                            sockTcp.AsciiSend("-ERR A required letter is not included.");
                            continue;
                        }
                        if(!Kernel.MailBox.Chps(user,password)){
                            sockTcp.AsciiSend("-ERR A problem occurred to a mailbox.");
                            continue;
                        }
                        sockTcp.AsciiSend("+OK Password changed.");
                    }
                }
                continue;

            UNKNOWN:
                sockTcp.AsciiSend(string.Format("-ERR Invalid command."));
                continue;

            FEW:
                sockTcp.AsciiSend(string.Format("-ERR Too few arguments for the {0} command.",str));
                continue;

            END:
                sockTcp.AsciiSend(string.Format("+OK Pop Server at {0} signing off.",Kernel.ServerName));
                break;
            }
            Kernel.MailBox.Logout(user);
            if (sockTcp != null)
                sockTcp.Close();

        }
        bool Login(SockTcp sockTcp,ref Pop3LoginState mode,ref MessageList messageList,string user,Ip addr) {
            
            var folder = Kernel.MailBox.Login(user, addr);
            
            if (folder == null) {
                Logger.Set(LogKind.Secure,sockTcp,1,string.Format("user={0}",user));
                sockTcp.AsciiSend("-ERR Double login");
                return false;
            }
            messageList = new MessageList(folder);//初期化

            //if (kernel.MailBox.Login(user, addr)) {//POP before SMTPのために、最後のログインアドレスを保存する
            mode = Pop3LoginState.Login;
            Logger.Set(LogKind.Normal,sockTcp,2,string.Format("User {0} from {1}[{2}]",user,sockTcp.RemoteHostname,sockTcp.RemoteAddress.Address));

            // LOGIN
            //dfList = kernel.MailBox.GetDfList(user);
            sockTcp.AsciiSend(string.Format("+OK {0} has {1} message ({2} octets).",user,messageList.Count,messageList.Size));
            return true;
        }
        void AuthError(SockTcp sockTcp,string user,string pass) {

            Logger.Set(LogKind.Secure,sockTcp,3,string.Format("user={0} pass={1}",user,pass));
            // 認証のエラーはすぐに返答を返さない
            var authTimeout = (int)Conf.Get("authTimeout");
            for (int i = 0; i < (authTimeout * 10) && IsLife(); i++) {
                Thread.Sleep(100);
            }
            sockTcp.AsciiSend(string.Format("-ERR Password supplied for {0} is incorrect.",user));
        }

        void AutoDeny(bool success, Ip remoteIp) {
            if (_attackDb == null)
                return;
            //データベースへの登録
            if (!_attackDb.IsInjustice(success, remoteIp))
                return;
            //ブルートフォースアタック
            if (!AclList.Append(remoteIp))
                return; //ACL自動拒否設定(「許可する」に設定されている場合、機能しない)
            //追加に成功した場合、オプションを書き換える
            var d = (Dat)Conf.Get("acl");
            var name = string.Format("AutoDeny-{0}", DateTime.Now);
            var ipStr = remoteIp.ToString();
            d.Add(true, string.Format("{0}\t{1}", name, ipStr));
            Conf.Set("acl", d);
            Conf.Save(Kernel.IniDb);
            //OneOption.SetVal("acl", d);
            //OneOption.Save(OptionIni.GetInstance());
            Logger.Set(LogKind.Secure, null, 9000055, string.Format("{0},{1}", name, ipStr));
        }
        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }
}
