using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.remote;
using Bjd.server;
using Bjd.sock;
using Bjd.util;


namespace RemoteServer {

    partial class Server : OneServer {
        readonly Queue<OneRemoteData> _queue = new Queue<OneRemoteData>();

        //コンストラクタ
        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel,conf,oneBind) {

        }
        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }
        //接続単位の処理
        SockTcp _sockTcp;//ここで宣言する場合、マルチスレッドでは使用できない
        override protected void OnSubThread(SockObj sockObj) {
            _sockTcp = (SockTcp)sockObj;

            //*************************************************************
            // パスワード認証
            //*************************************************************
            var password = (string)Conf.Get("password");
            if (password == ""){
                Logger.Set(LogKind.Normal,_sockTcp,5,"");
            }else{//パスワード認証が必要な場合
                var challengeStr = Inet.ChallengeStr(10);//チャレンジ文字列の生成

                RemoteData.Send(_sockTcp, RemoteDataKind.DatAuth, challengeStr);

                //パスワードの応答待ち
                var success = false;//Ver5.0.0-b14
                while (IsLife() && _sockTcp.SockState == Bjd.sock.SockState.Connect) {
                    var o = RemoteData.Recv(_sockTcp,this);
                    if(o!=null){
                        if (o.Kind == RemoteDataKind.CmdAuth) {

                            //ハッシュ文字列の作成（MD5）
                            var md5Str = Inet.Md5Str(password + challengeStr);
                            if(md5Str != o.Str) {
                                Logger.Set(LogKind.Secure,_sockTcp,4,"");

                                //DOS対策 3秒間は次の接続を受け付けない
                                //for (int i = 0; i < 30 && life; i++) {
                                //    Thread.Sleep(100);
                                //}
                                //tcpObj.Close();//この接続は破棄される
                                //return;
                            } else {
                                success = true;//Ver5.0.0-b14
                            }
                            break;
                        }
                    }else{
                        Thread.Sleep(500);
                    }
                }
		        //Ver5.0.0-b14
		        if(!success) {
                    //認証失敗（パスワードキャンセル・パスワード違い・強制切断）
                    //DOS対策 3秒間は次の接続を受け付けない
                    for(var i = 0;i < 30 && IsLife();i++) {
                        Thread.Sleep(100);
                    }
                    _sockTcp.Close();//この接続は破棄される
                    return;
                }
            }

            //*************************************************************
            // 認証完了
            //*************************************************************
            
            Logger.Set(LogKind.Normal,_sockTcp,1,string.Format("address={0}",_sockTcp.RemoteAddress.Address));

            //バージョン/ログイン完了の送信
            RemoteData.Send(_sockTcp, RemoteDataKind.DatVer, Kernel.Ver.VerData());

            //kernel.LocalAddressをRemote側で生成する
            RemoteData.Send(_sockTcp, RemoteDataKind.DatLocaladdress, LocalAddress.GetInstance().RemoteStr());

            //オプションの送信
            var optionFileName = string.Format("{0}\\Option.ini",Kernel.ProgDir());
            string optionStr;
            using (var sr = new StreamReader(optionFileName, Encoding.GetEncoding("Shift_JIS"))) {
                optionStr = sr.ReadToEnd();
                sr.Close();
            }
            RemoteData.Send(_sockTcp, RemoteDataKind.DatOption, optionStr);
            Kernel.RemoteConnect = new Bjd.remote.RemoteConnect(_sockTcp);//リモートクライアント接続開始
            Kernel.View.SetColor();//ウインド色の初期化

            while (IsLife() && _sockTcp.SockState == Bjd.sock.SockState.Connect) {
                var o = RemoteData.Recv(_sockTcp,this);
                if (o==null)
                    continue;
                //コマンドは、すべてキューに格納する
                _queue.Enqueue(o);
                if (_queue.Count == 0) {
                    GC.Collect();
                    Thread.Sleep(500);
                } else {
                    Cmd(_queue.Dequeue());
                }
            }

            Kernel.RemoteConnect = null;//リモートクライアント接続終了

            Logger.Set(LogKind.Normal, _sockTcp, 2, string.Format("address={0}", _sockTcp.RemoteAddress.Address));
            Kernel.View.SetColor();//ウインド色の初期化

            _sockTcp.Close();

        }

        void Cmd(OneRemoteData o) {
            //サービスから呼び出された場合は、コントロール処理はないのでInvokeはしない
            //if (mainForm != null && mainForm.InvokeRequired) {
            //    mainForm.Invoke(new MethodInvoker(() => Cmd(remoteObj)));
            //} else {
                switch (o.Kind) {
                    case RemoteDataKind.CmdRestart:
                        //自分自身（スレッド）を停止するため非同期で実行する
                        Kernel.Menu.EnqueueMenu("StartStop_Restart", false/*synchro*/);
                        break;
                    case RemoteDataKind.CmdTool:
                        var tmp = (o.Str).Split(new[] { '-' }, 2);
                        if (tmp.Length == 2) {
                            var nameTag = tmp[0];
                            var cmdStr = tmp[1];

                            var buffer = "";

                            if (nameTag == "BJD") {
                                buffer = Kernel.Cmd(cmdStr);//リモート操作（データ取得）
                            } else {
                                var server = Kernel.ListServer.Get(nameTag);
                                if (server != null) {
                                    buffer = server.Cmd(cmdStr);//リモート操作（データ取得）
                                }
                            }
                            RemoteData.Send(_sockTcp, RemoteDataKind.DatTool, cmdStr + "\t" + buffer);
                        }
                        break;
                    case RemoteDataKind.CmdBrowse:
                        var lines = Kernel.GetBrowseInfo(o.Str);
                        RemoteData.Send(_sockTcp, RemoteDataKind.DatBrowse, lines);
                        break;
                    case RemoteDataKind.CmdOption:
                        //string optionStr = remoteObj.STR;
                        //Option.iniを上書きする

                        //クライアントでオプションを変更してサーバ側へ送っているが反映されていない様子
                        //c:\outでクライアントを立ち上げ、「FTPサーバ使用する」にして変更して送ってみて
                        //    変更された内容が、ここに到着しているかどうかを確認する


                        var optionFileName = string.Format("{0}\\Option.ini", Kernel.ProgDir());
                        using (var sw = new StreamWriter(optionFileName, false, Encoding.GetEncoding("Shift_JIS"))) {
                            sw.Write(o.Str);
                            sw.Close();
                        }
                        Kernel.ListInitialize();//Option.iniを読み込む
                        
                        //Ver5.8.6 Java fix 新しくDefから読み込んだオプションがあった場合に、そのオプションを保存するため
                        Kernel.ListOption.Save(Kernel.IniDb);


                        //自分自身（スレッド）を停止するため非同期で実行する
                        Kernel.Menu.EnqueueMenu("StartStop_Reload",false/*synchro*/);
                        break;
                    case RemoteDataKind.CmdTrace:
                        Kernel.RemoteConnect.OpenTraceDlg = (o.Str=="1");
                        break;
            //    }
            }
        }


        //ログのAppendイベントでリモートクライアントへログを送信する
        public override void Append(OneLog oneLog) {
            RemoteData.Send(_sockTcp, RemoteDataKind.DatLog, oneLog.ToString());
        }
    }
}
