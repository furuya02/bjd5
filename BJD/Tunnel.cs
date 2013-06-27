using System;
using System.Collections.Generic;
using System.Threading;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;


//TCPによるプロキシのベースクラス
namespace Bjd {

    public class Tunnel {
        //ソケット
        protected Dictionary<CS, SockTcp> Sock = new Dictionary<CS, SockTcp>(2);

        //バッファ（デフォルトは byte[] ）
        readonly Dictionary<CS, byte[]> _byteBuf = new Dictionary<CS, byte[]>(2);
        readonly Dictionary<CS, string> _strBuf = new Dictionary<CS, string>(2);


        //アイドル(分）　0の場合、アイドル処理は無効になる
        protected int IdleTime;
        protected Logger Logger;
        protected int Timeout;
        private DateTime _dt;

        //アイドル処理用のタイマ初期化
        public void ResetIdle(){
            //アイドル処理有効の場合
            if (IdleTime != 0) {
                _dt = DateTime.Now.AddMinutes(IdleTime);
            }
        }
        //アイドル処理 タイムアウトの確認
        private bool IsTimeout() {
            if (IdleTime != 0){
                if (_dt < DateTime.Now){
                    return true;
                }
            }
            return false;
        }



        public Tunnel(Logger logger,int idleTime,int timeout) {

            _byteBuf[CS.Client] = new byte[0];
            _byteBuf[CS.Server] = new byte[0];
            _strBuf[CS.Client] = "";
            _strBuf[CS.Server] = "";

            IdleTime = idleTime;
            Logger = logger;
            Timeout = timeout;
        }
        public void Pipe(SockTcp server, SockTcp client,ILife iLife) {

            Sock[CS.Client] = client;
            Sock[CS.Server] = server;

            //アイドル処理用のタイマ初期化
            ResetIdle();

            var cs = CS.Server;
            while(iLife.IsLife()) {
                cs = Reverse(cs);//サーバ側とクライアント側を交互に処理する
                Thread.Sleep(1);

                // クライアントの切断の確認
                if(Sock[CS.Client].SockState != SockState.Connect) {

                    //Ver5.2.8
                    //クライアントが切断された場合でも、サーバ側が接続中で送信するべきデータが残っている場合は処理を継続する
                    if (Sock[CS.Server].SockState == SockState.Connect && Sock[CS.Client].Length() != 0) {
                        
                    } else {
                        Logger.Set(LogKind.Detail, Sock[CS.Server], 9000043, "close client");
                        break;
                    }
                }

                //*******************************************************
                //処理するデータが到着していない場合の処理
                //*******************************************************
                if(Sock[CS.Client].Length() == 0 && Sock[CS.Server].Length() == 0 && _byteBuf[CS.Client].Length == 0 && _byteBuf[CS.Server].Length == 0) {

                    // サーバの切断の確認
                    if(Sock[CS.Server].SockState != SockState.Connect) {

                        //送信するべきデータがなく、サーバが切断された場合は、処理終了
                        Logger.Set(LogKind.Detail,Sock[CS.Server],9000044,"close server");
                        break;
                    }

                    Thread.Sleep(100);

                    //アイドル処理 タイムアウトの確認
                    if(IsTimeout()){
                        Logger.Set(LogKind.Normal,Sock[CS.Server],9000019,string.Format("option IDLETIME={0}min",IdleTime));
                        break;
                    }
                } else {
                    //アイドル処理用のタイマ初期化
                    ResetIdle();
                }

                //*******************************************************
                // 受信処理 
                //*******************************************************
                if(_byteBuf[cs].Length == 0) { //バッファが空の時だけ処理する
                    //処理すべきデータ数の取得
                    var len = Sock[cs].Length();
                    if(len > 0) {
                        const int sec = 10; //受信バイト数がわかっているので、ここでのタイムアウト値はあまり意味が無い
                        var b = Sock[cs].Recv(len,sec,iLife);
                        if(b != null){
                            //Assumption() 受信時の処理
                            _byteBuf[cs] = Bytes.Create(_byteBuf[cs],Assumption(b,iLife));
                        }
                    }
                }
                //*******************************************************
                // 送信処理
                //*******************************************************
                if(_byteBuf[cs].Length != 0) { //バッファにデータが入っている場合だけ処理する

                    var c = Sock[Reverse(cs)].SendUseEncode(_byteBuf[cs]);
                    if(c == _byteBuf[cs].Length) {
                        _byteBuf[cs] = new byte[0];
                    } else {
                        Logger.Set(LogKind.Error,server,9000020,string.Format("sock.Send() return {0}",c));
                        break;
                    }
                }
            }
        }

        //受信時の処理
        //受信した内容によって処理を行う必要がある場合は、このメソッドをオーバーライドする
        virtual protected byte [] Assumption(byte [] buf,ILife iLife) {
            //デフォルトでは処理なし
            return buf;
        }

        CS Reverse(CS cs){
            return cs == CS.Client ? CS.Server : CS.Client;
        }
    }

}
