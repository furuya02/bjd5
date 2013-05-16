using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Bjd.acl;
using Bjd.ctrl;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;

namespace Bjd.server{

//OneServer １つのバインドアドレス：ポートごとにサーバを表現するクラス<br>
//各サーバオブジェクトの基底クラス<br>
    public abstract class OneServer : ThreadBase{

        protected Conf Conf;
        public Logger Logger;
        protected bool IsJp;
        protected int Timeout;
        SockServer _sockServer;
        readonly OneBind _oneBind;
        readonly Ssl _ssl = null;

        public String NameTag { get; private set; }
        protected Kernel Kernel; //SockObjのTraceのため
        protected AclList AclList = null;
        
        //子スレッド管理
        private static readonly object SyncObj = new object(); //排他制御オブジェクト
        readonly List<Thread> _childThreads = new List<Thread>();
        readonly int _multiple; //同時接続数

        //ステータス表示用
        public override String ToString(){
            String stat = IsJp ? "+ サービス中 " : "+ In execution ";
            //if (!IsRunning){
            if (KindThreadBase != KindThreadBase.Running){
                stat = IsJp ? "- 停止 " : "- Initialization failure ";
            }
            return string.Format("{0}\t{1,20}\t[{2}\t:{3} {4}]\tThread {5}/{6}", stat, NameTag, _oneBind.Addr, _oneBind.Protocol.ToString().ToUpper(), (int) Conf.Get("port"), Count(), _multiple);
        }



        public int Count(){
            //Java fix try-catch追加
            try{
                //チャイルドスレッドオブジェクトの整理
                for (int i = _childThreads.Count - 1; i >= 0; i--){
                    if (!_childThreads[i].IsAlive){
                        _childThreads.RemoveAt(i);
                    }
                }
                return _childThreads.Count;
            } catch (Exception){
                return 0;
            }

        }

        //リモート操作(データの取得)
        public String cmd(String cmdStr){
            return "";
        }

        public SockState SockState(){
            if (_sockServer == null){
                return sock.SockState.Error;
            }
            return _sockServer.SockState;
        }

        //コンストラクタ
        protected OneServer(Kernel kernel, Conf conf, OneBind oneBind) 
            : base(kernel.CreateLogger(conf.NameTag,true,null)){
            Kernel = kernel;
            NameTag = conf.NameTag;
            Conf = conf;
            _oneBind = oneBind;
            IsJp = kernel.IsJp();

            //DEBUG用
            if (Conf == null){
                OptionSample optionSample = new OptionSample(kernel, "");
                Conf = new Conf(optionSample);
                Conf.Set("port", 9990);
                Conf.Set("multiple", 10);
                Conf.Set("acl", new Dat(new CtrlType[0]));
                Conf.Set("enableAcl", 1);
                Conf.Set("timeOut", 3);
            }
            //DEBUG用
            if (_oneBind == null){
                var ip = new Ip(IpKind.V4Localhost);
                _oneBind = new OneBind(ip, ProtocolKind.Tcp);
            }

            Logger = kernel.CreateLogger(conf.NameTag, (bool)Conf.Get("useDetailsLog"), this);
            _multiple = (int) Conf.Get("multiple");

            //DHCPにはACLが存在しない
            if (NameTag != "Dhcp"){
                //ACLリスト 定義が無い場合は、aclListを生成しない
                var acl = (Dat)Conf.Get("acl");
                AclList = new AclList(acl, (int)Conf.Get("enableAcl"), Logger);
            }
            Timeout = (int) Conf.Get("timeOut");
        }



        public new void Start(){

            base.Start();

            //bindが完了するまで待機する
            while (_sockServer == null || _sockServer.SockState == sock.SockState.Idle){
                Thread.Sleep(100);
            }
        }


        public new void Stop(){
            if (_sockServer == null){
                return; //すでに終了処理が終わっている
            }
            base.Stop(); //life=false ですべてのループを解除する
            _sockServer.Close();

            // 全部の子スレッドが終了するのを待つ
            while (Count() > 0){
                Thread.Sleep(500);
            }
            _sockServer = null;

        }

        public new void Dispose(){
            // super.dispose()は、ThreadBaseでstop()が呼ばれるだけなので必要ない
            Stop();
        }

        //スレッド停止処理
        protected abstract void OnStopServer(); //スレッド停止処理

        protected override void OnStopThread(){
            OnStopServer(); //子クラスのスレッド停止処理
            if (_ssl != null){
                _ssl.Dispose();
            }
        }

        //スレッド開始処理
        //サーバが正常に起動できる場合(isInitSuccess==true)のみスレッド開始できる
        protected abstract bool OnStartServer(); //スレッド開始処理

        protected override bool OnStartThread(){
            return OnStartServer(); //子クラスのスレッド開始処理
        }

        protected override void OnRunThread(){

            var port = (int) Conf.Get("port");
            String bindStr = string.Format("{0}:{1} {2}", _oneBind.Addr, port, _oneBind.Protocol);
            Logger.Set(LogKind.Normal, null, 9000000, bindStr);

            //DOSを受けた場合、multiple数まで連続アクセスまでは記憶してしまう
            //DOSが終わった後も、その分だけ復帰に時間を要する

            _sockServer = new SockServer(this.Kernel,_oneBind.Protocol);

            if (_sockServer.SockState != sock.SockState.Error){
                if (_sockServer.ProtocolKind == ProtocolKind.Tcp){
                    RunTcpServer(port);
                } else{
                    RunUdpServer(port);
                }
            }
            //Java fix
            _sockServer.Close();
            Logger.Set(LogKind.Normal, null, 9000001, bindStr);

        }

        private void RunTcpServer(int port){

            int listenMax = 5;

            //[C#]
            //IsRunning = true;
            KindThreadBase = KindThreadBase.Running;

            if (!_sockServer.Bind(_oneBind.Addr, port, listenMax)) {
                Logger.Set(LogKind.Error, _sockServer, 9000006, _sockServer.GetLastEror());
            } else{

                while (IsLife()){
                    SockTcp child = (SockTcp) _sockServer.Select(this);
                    if (child == null){
                        break;
                    }
                    if (Count() >= _multiple){
                        Logger.Set(LogKind.Secure, _sockServer, 9000004, string.Format("count:{0}/multiple:{1}", Count(), _multiple));
                        //同時接続数を超えたのでリクエストをキャンセルします
                        child.Close();
                        continue;
                    }

                    // ACL制限のチェック
                    if (AclCheck(child) == AclKind.Deny){
                        child.Close();
                        continue;
                    }
                    lock (SyncObj){
                        var t = new Thread(SubThread){IsBackground = true};
                        t.Start(child);
                        _childThreads.Add(t);
                    }
                }

            }
        }

        private void RunUdpServer(int port) {

            //[C#]
            //IsRunning = true;
            KindThreadBase = KindThreadBase.Running;


            if (!_sockServer.Bind(_oneBind.Addr, port)) {
                Logger.Set(LogKind.Error, _sockServer, 9000006, _sockServer.GetLastEror());
                //println(string.Format("bind()=false %s", sockServer.getLastEror()));
            } else{
                while (IsLife()){
                    SockUdp child = (SockUdp) _sockServer.Select(this);
                    if (child == null){
                        //Selectで例外が発生した場合は、そのコネクションを捨てて、次の待ち受けに入る
                        continue;
                    }
                    if (Count() >= _multiple){
                        Logger.Set(LogKind.Secure, _sockServer, 9000004, string.Format("count:{0}/multiple:{1}", Count(), _multiple));
                        //同時接続数を超えたのでリクエストをキャンセルします
                        child.Close();
                        continue;
                    }

                    // ACL制限のチェック
                    if (AclCheck(child) == AclKind.Deny){
                        child.Close();
                        continue;
                    }
                    lock (SyncObj) {
                        var t = new Thread(SubThread) { IsBackground = true };
                        t.Start(child);
                        _childThreads.Add(t);
                    }
                }

            }
        }

        //ACL制限のチェック
	    //sockObj 検査対象のソケット
        private AclKind AclCheck(SockObj sockObj){
            var aclKind = AclKind.Allow;
            if (AclList != null){
                Ip ip = new Ip(sockObj.RemoteAddress.Address.ToString());
                aclKind = AclList.Check(ip);
            }

            if (aclKind == AclKind.Deny){
                denyAddress = sockObj.RemoteAddress.ToString();
            }
            return aclKind;
        }

        protected abstract void OnSubThread(SockObj sockObj);

        private String denyAddress = ""; //Ver5.3.5 DoS対処

	    //１リクエストに対する子スレッドとして起動される
        public void SubThread(Object o){
            SockObj sockObj = (SockObj) o;

            //クライアントのホスト名を逆引きする
            sockObj.Resolve((bool) Conf.Get("useResolve"), Logger);

            //_subThreadの中でSockObjは破棄する（ただしUDPの場合は、クローンなのでClose()してもsocketは破棄されない）
            Logger.Set(LogKind.Detail, sockObj, 9000002, string.Format("count={0} Local={1} Remote={2}", Count(), sockObj.LocalAddress, sockObj.RemoteAddress));

            OnSubThread(sockObj); //接続単位の処理
            sockObj.Close();

            Logger.Set(LogKind.Detail, sockObj, 9000003, string.Format("count={0} Local={1} Remote={2}", Count(), sockObj.LocalAddress, sockObj.RemoteAddress));

        }

        //Java Fix
        //RemoteServerでのみ使用される
        public abstract void Append(OneLog oneLog);

        //1行読込待機
        public Cmd WaitLine(SockTcp sockTcp){
            var tout = new util.Timeout(Timeout*1000);

            while (IsLife()){
                Cmd cmd = recvCmd(sockTcp);
                if (cmd == null){
                    return null;
                }
                if (!(cmd.CmdStr  == "")){
                    return cmd;
                }
                if (tout.IsFinish()){
                    return null;
                }
                Thread.Sleep(100);
            }
            return null;
        }

        //TODO RecvCmdのパラメータ形式を変更するが、これは、後ほど、Web,Ftp,SmtpのServerで使用されているため影響がでる予定
        //コマンド取得
	    //コネクション切断などエラーが発生した時はnullが返される
        protected Cmd recvCmd(SockTcp sockTcp){
            if (sockTcp.SockState != sock.SockState.Connect){
                //切断されている
                return null;
            }
            byte[] recvbuf = sockTcp.LineRecv(Timeout, this);
            //切断された場合
            if (recvbuf == null){
                return null;
            }

            //受信待機中の場合
            if (recvbuf.Length == 0){

                //Ver5.8.5 Java fix
                //return new Cmd("", "", "");
                return new Cmd("waiting", "", ""); //待機中の場合、そのことが分かるように"waiting"を返す
            }

            //CRLFの排除
            recvbuf = Inet.TrimCrlf(recvbuf);

            //String str = new String(recvbuf, Charset.forName("Shift-JIS"));
            String str = Encoding.GetEncoding("Shift-JIS").GetString(recvbuf);
            if (str == ""){
                return new Cmd("", "", "");
            }
            //受信行をコマンドとパラメータに分解する（コマンドとパラメータは１つ以上のスペースで区切られている）
            String cmdStr = null;
            String paramStr = null;
            for (int i = 0; i < str.Length; i++){
                if (str[i] == ' '){
                    if (cmdStr == null){
                        cmdStr = str.Substring(0, i);
                    }
                }
                if (cmdStr == null || str[i] == ' '){
                    continue;
                }
                paramStr = str.Substring(i);
                break;
            }
            if (cmdStr == null){
                //パラメータ区切りが見つからなかった場合
                cmdStr = str; //全部コマンド
            }
            return new Cmd(str, cmdStr, paramStr);
        }

        //未実装
//        public void Append(OneLog oneLog){
//            Util.RuntimeException("OneServer.Append(OneLog) 未実装");
//        }

        //リモート操作(データの取得)
    	public virtual String Cmd(String cmdStr) {
		    return "";
	    }

        /********************************************************/
        //移植のための暫定処置(POP3及びSMTPでのみ使用されている)
        /********************************************************/
        protected bool RecvCmd(SockTcp sockTcp, ref string str, ref string cmdStr, ref string paramStr){

            var cmd = recvCmd(sockTcp);
            if (cmd == null){
                return false;
            }
            cmdStr = cmd.CmdStr;
            paramStr = cmd.ParamStr;
            str = cmd.Str;
            return true;
        }
        public bool WaitLine(SockTcp sockTcp, ref string cmdStr, ref string paramStr) {
            Cmd cmd = WaitLine(sockTcp);
            if (cmd == null){
                return false;
            }
            cmdStr = cmd.CmdStr;
            paramStr = cmd.ParamStr;
            return true;
        }

    }
}

/*
    //各サーバオブジェクトの基底クラス
    //****************************************************************
    // OneServer １つのバインドアドレス：ポートごとにサーバを表現するクラス
    //****************************************************************
    public abstract class OneServer : ThreadBase,ILogger{

        protected OneBind OneBind;

        protected AclList AclList;
        protected Ssl Ssl;
        public Logger Logger { get; protected set; }
        public OneOption OneOption { get; protected set; }

        private bool _isBusy; //排他制御

        public new abstract string GetMsg(int messageNo);

        protected int Timeout;

        //子スレッド管理
        private static readonly object SyncObj = new object(); //排他制御オブジェクト
        private readonly List<Thread> _childThreads = new List<Thread>();
        private int _childCount; //多重度のカウンタ
        private readonly int _multiple; //同時接続数

        //ステータス表示用
        public override string ToString(){
            var stat = (Kernel.IsJp()) ? "+ サービス中 " : "+ In execution ";
            if (!IsRunnig)
                stat = (Kernel.IsJp()) ? "- 停止 " : "- Initialization failure ";
            return string.Format("{0}\t{1,-20}\t[{2}\t:{3} {4}]\tThread {5}/{6}", stat, NameTag, OneBind.Addr,
                                 OneBind.Protocol.ToString().ToUpper(), (int) OneOption.GetValue("port"), _childCount,
                                 _multiple);
        }

        //リモート操作(データの取得)
        public virtual string Cmd(string cmdStr){
            return "";
        }

        //コンストラクタ
        protected OneServer(Kernel kernel, string nameTag, OneBind oneBind)
            : base(kernel, nameTag){

            OneOption = kernel.ListOption.Get(nameTag);

            OneBind = oneBind;
            Logger = kernel.CreateLogger(nameTag, (bool) OneOption.GetValue("useDetailsLog"), this);

            _multiple = (int) OneOption.GetValue("multiple");

            //ACLリスト
            try{
                //定義が存在するかどうかのチェック
                var acl = (Dat) OneOption.GetValue("acl");
                AclList = new AclList(acl, (int) OneOption.GetValue("enableAcl"), Logger);
            }catch (Exception){
                
            }
            Timeout = (int) OneOption.GetValue("timeOut");
        }

        //スレッド停止処理
        protected abstract void OnStopServer(); //スレッド停止処理
        protected override void OnStopThread(){
            OnStopServer(); //子クラスのスレッド停止処理
            if (Ssl != null){
                Ssl.Dispose();
            }
        }

        //スレッド開始処理
        //サーバが正常に起動できる場合(isInitSuccess==true)のみスレッド開始できる
        protected abstract bool OnStartServer(); //スレッド開始処理
        protected override bool OnStartThread(){
            return OnStartServer(); //子クラスのスレッド開始処理
        }

        protected override void OnLoopThread(){

            var port = (int) OneOption.GetValue("port");

            var bindStr = string.Format("{0}:{1} {2}", OneBind.Addr, port, OneBind.Protocol);

            logger.Set(LogKind.Normal, null, 9000000, bindStr);

            //DOSを受けた場合、multiple数まで連続アクセスまでは記憶してしまう
            //DOSが終わった後も、その分だけ復帰に時間を要する

            var sockObj = OneBind.Protocol == ProtocolKind.Tcp
                              ? (SockObj) new sockTcp(Kernel, Logger, OneBind.Addr, port, _multiple, Ssl)
                              : new UdpObj(Kernel, Logger, OneBind.Addr, port, _multiple);

            if (sockObj.State == SocketObjState.Error){
                Thread.Sleep(1000); //このウエイトが無いと応答不能になる
                goto close;
            }
            _isBusy = false; //排他制御
            while (Life){
                while (_isBusy){
                    if (!Life)
                        break;
                    Thread.Sleep(10);
                }
                //callBack関数の中で、子オブジェクトを作成し、作成完了すると排他制御が解除される
                _isBusy = true;
                sockObj.StartServer(CallBackFunc);

                //チャイルドスレッドオブジェクトの整理
                lock (SyncObj){
                    for (var i = _childThreads.Count - 1; i >= 0; i--){
                        if (_childThreads[i].IsAlive)
                            continue;
                        _childThreads[i] = null;
                        _childThreads.RemoveAt(i);
                    }
                }
            }
            close:
            sockObj.Close();

            while (_childCount != 0){
                Thread.Sleep(100);
                //サーバ停止でハングアップして、中断をかけた時ここにくる場合、動作中の子プロセスが終了していない
                //ここで、opBase.nameTagを確認すれば、何のサーバプロセスが動作中かどうかが分かる
            }
            logger.Set(LogKind.Normal, null, 9000001, bindStr);
        }

        public void CallBackFunc(IAsyncResult ar){
            var sockObj = ((SockObj) (ar.AsyncState)).CreateChildObj(ar);
            _isBusy = false; //排他制御解除
            if (sockObj != null){
                if (_childCount >= _multiple){
                    logger.Set(LogKind.Secure, sockObj, 9000004,
                               string.Format("count:{0}/multiple:{1}", _childCount, _multiple));
                        //同時接続数を超えたのでリクエストをキャンセルします
                    sockObj.Close(); //2009.06.04
                }
                else{
                    //Ver5.3.5
                    if (sockObj.RemoteEndPoint.Address.ToString() == _denyAddress){
                        logger.Set(LogKind.Secure, null, 9000016, string.Format("address:{0}", _denyAddress));
                            //このアドレスからのリクエストは許可されていません
                        Thread.Sleep(100);
                        sockObj.Close();
                    }
                    else{

                        lock (SyncObj){
                            var t = new Thread(SubThread){IsBackground = true};
                            t.Start(sockObj);
                            _childThreads.Add(t);
                        }
                    }
                }
            }

        }

        //Ver5.4.1
        //abstract public void _subThread(SockObj sockObj,RemoteInfo remoteInfo);
        protected abstract void OnSubThread(SockObj sockObj);

        private string _denyAddress = ""; //Ver5.3.5 DoS対処

        //１リクエストに対する子スレッドとして起動される
        public void SubThread(object o){
            _childCount++; //多重度のカウンタ

            var sockObj = (SockObj) o;

            try{
                //***************************************************************
                // ACL制限のチェック
                //***************************************************************
                if (AclList != null){
                    if (!AclList.Check(new Ip(sockObj.RemoteEndPoint.Address.ToString()))){
                        sockObj.Close(); //2009.06.8
                        _denyAddress = sockObj.RemoteEndPoint.Address.ToString();
                        goto end;
                    }
                }

                //クライアントのホスト名を逆引きする
                sockObj.Resolve((bool) OneOption.GetValue("useResolve"), Logger);



                //_subThreadの中でSockObjは破棄する（ただしUDPの場合は、クローンなのでClose()してもsocketは破棄されない）
                logger.Set(LogKind.Detail, sockObj, 9000002,
                           string.Format("count={0} Local={1} Remote={2}", _childCount, sockObj.LocalEndPoint,
                                         sockObj.RemoteEndPoint));

                //接続元情報
                //Ip remoteAddr = new Ip(sockObj.RemoteEndPoint.Address.ToString());
                //Ver5.4.1 sockObjのプロパティ(RemoteAddr)に変更
                //Ip remoteAddr = new Ip(sockObj.RemoteEndPoint.Address.ToString());
                sockObj.RemoteAddr = new Ip(sockObj.RemoteEndPoint.Address.ToString());

                //Ver5.4.1 sockObjのプロパティ(RemoteHost)に変更
                //string remoteHost = sockObj.RemoteHostName;

                //Ver5.4.1
                //_subThread(sockObj, new RemoteInfo(remoteHost, remoteAddr));//接続単位の処理
                OnSubThread(sockObj); //接続単位の処理

                sockObj.Close();

                logger.Set(LogKind.Detail, sockObj, 9000003,
                           string.Format("count={0} Local={1} Remote={2}", _childCount, sockObj.LocalEndPoint,
                                         sockObj.RemoteEndPoint));

            }
            catch (Exception ex){
                logger.Set(LogKind.Error, sockObj, 9000037, ex.Message);
                Logger.Exception(ex, null, 9000038);
            }
            end:
            _childCount--; //多重度のカウンタ
        }

        public bool WaitLine(sockTcp sockTcp, ref string cmdStr, ref string paramStr){
            var dt = DateTime.Now.AddSeconds(Timeout);
            cmdStr = "";
            var str = "";
            while (Life){
                if (!RecvCmd(sockTcp, ref str, ref cmdStr, ref paramStr)){
                    return false;
                }
                if (cmdStr != "")
                    return true;

                Thread.Sleep(100);
                if (dt < DateTime.Now)
                    return false;
            }
            return false;
        }

        protected bool RecvCmd(sockTcp sockTcp, ref string str, ref string cmdStr, ref string paramStr){
            if (sockTcp.State != SocketObjState.Connect) //切断されている
                return false;
            var recvbuf = sockTcp.LineRecv(Timeout, OperateCrlf.Yes, ref Life);
            if (recvbuf == null){
                return false; //切断された
            }
            str = Encoding.GetEncoding("shift-jis").GetString(recvbuf);
            if (str == ""){
                return true;
            }

            //logger.Set(LogKind.Detail,sockTcp,9000016,str);//"command Received."

            //受信行をコマンドとパラメータに分解する（コマンドとパラメータは１つ以上のスペースで区切られている）
            cmdStr = null;
            paramStr = null;
            for (var i = 0; i < str.Length; i++){
                if (str[i] == ' '){
                    if (cmdStr == null)
                        cmdStr = str.Substring(0, i);
                }
                if (cmdStr == null || str[i] == ' ')
                    continue;
                paramStr = str.Substring(i);
                break;
            }
            if (cmdStr == null) //パラメータ区切りが見つからなかった場合
                cmdStr = str; //全部コマンド
            return true;
        }

        //RemoteServerでのみ使用される
        public virtual void Append(OneLog oneLog){}

    }
}
    */