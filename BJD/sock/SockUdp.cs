using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Bjd.net;

namespace Bjd.sock{
    public class SockUdp : SockObj{
        private readonly Ssl _ssl;

        private readonly SockKind _sockKind;

        private readonly Socket _socket;

        private readonly byte[] _recvBuf = new byte[0];

        //***************************************************************************
        //パラメータのKernelはSockObjにおけるTrace()のためだけに使用されているので、
        //Traceしない場合は削除することができる
        //***************************************************************************

        protected SockUdp(Kernel kernel):base(kernel){
            //隠蔽する
        }
        

        //ACCEPT
        public SockUdp(Kernel kernel,Socket s, byte[] buf, int len, IPEndPoint ep):base(kernel){
            _sockKind = SockKind.ACCEPT;

            _socket = s;
            _recvBuf = new byte[len];
            Buffer.BlockCopy(buf,0,_recvBuf,0,len);

            //************************************************
            //selector/channel生成
            //************************************************

            Set(SockState.Connect, (IPEndPoint) s.LocalEndPoint, ep);


            //ACCEPTの場合は、既に受信しているので、これ以上待機する必要はない
            //    doRead(channel);
            //UDPの場合、doReadの中で、remoteAddressがセットされる
            //あとは、クローズされるまで待機

            //Ver5.8.5 Java fix
//            ThreadStart action = () =>{
//                while (SockState == SockState.Connect){
//                    Thread.Sleep(10);
//                }
//            };
//            var t = new Thread(action){IsBackground = true};
//            t.Start();
        }

        //CLIENT
        public SockUdp(Kernel kernel,Ip ip, int port, Ssl ssl, byte[] buf):base(kernel){
            //SSL通信を使用する場合は、このオブジェクトがセットされる 通常の場合は、null
            _ssl = ssl;

            _sockKind = SockKind.CLIENT;

            _socket = new Socket((ip.InetKind == InetKind.V4) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

            Set(SockState.Connect, null, new IPEndPoint(ip.IPAddress, port));

            //************************************************
            //送信処理
            //************************************************
            Send(buf);
        }

        public byte[] Recv(int sec) {
            _socket.ReceiveTimeout = sec * 1000;
            try {
                EndPoint ep = RemoteAddress;
                var tmp = new byte[1620];
                var l = _socket.ReceiveFrom(tmp, ref ep);
                //_recvBuf = new byte[l];
                //Buffer.BlockCopy(tmp, 0, _recvBuf, 0, l);
                var buf = new byte[l];
                Buffer.BlockCopy(tmp, 0, buf, 0, l);
                Set(SockState.Connect, LocalAddress, (IPEndPoint)ep);
                
                return buf;
            } catch (Exception){
                return new byte[0];
            }
        }

        //ACCEPTの場合は、既に受信できているので、こちらでアクセスする
        public int Length() {
            return _recvBuf.Length;
        }
        //ACCEPTの場合は、既に受信できているので、こちらでアクセスする
        public byte[] RecvBuf {
            get { return _recvBuf; }
            //set { throw new NotImplementedException(); }
        }

        //ACCEPTのみで使用する　CLIENTは、コンストラクタで送信する
        public int Send(byte[] buf){
            if (buf.Length == 0){
                return 0;
            }
            if (RemoteAddress.AddressFamily == AddressFamily.InterNetwork){
                //警告 GetAddressBytes() を使用してください。
                //if (RemoteEndPoint.Address.Address == 0xffffffff) {
                var addrBytes = RemoteAddress.Address.GetAddressBytes();
                if (addrBytes[0] == 0xff && addrBytes[1] == 0xff && addrBytes[2] == 0xff && addrBytes[3] == 0xff){
                    // ブロードキャストはこのオプション設定が必要
                    try{
                        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    } catch{
                        return -1;
                    }
                }
                //IPv4
                return _socket.SendTo(buf, buf.Length, SocketFlags.None, RemoteAddress);
            } //IPv6
            return _socket.SendTo(buf, buf.Length, SocketFlags.None, RemoteAddress);
        }

        public override void Close(){
            //ACCEPT
            if (_sockKind == SockKind.ACCEPT){
                return;
            }
            _socket.Close();
            SetError("close()");
        }
    }
}
/*
 using System;
using System.Net;
using System.Net.Sockets;
using Bjd.log;
using Bjd.sock;
using Bjd.util;

namespace Bjd.net {
    //UDP接続用
    public class UdpObj : SockObj {
        public byte[] RecvBuf { get; private set; }
        const int UdpBufferSize = 65515;

        //【コンストラクタ（サーバ用）】bind　（not クローン）
        //listenMaxはコンストラクタに変化を与えるための擬似パラメータ（未使用）
        public UdpObj(Kernel kernel, Logger logger, Ip ip, Int32 port, int listenMax)
            : base(kernel, logger, ip.InetKind) {
            //Ver5.1.3
            //RecvBuf = new byte[1600];//１パケットの最大サイズで受信待ちにする
            RecvBuf = new byte[UdpBufferSize];//１パケットの最大サイズで受信待ちにする

            try {
                //socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Socket = new Socket((ip.InetKind == InetKind.V4) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            } catch (Exception e) {
                state = SocketObjState.Error;
                //Ver5.0.0-a8
                var detailInfomation = Util.SwapStr("\n", "", Util.SwapStr("\r", "", e.Message));
                logger.Set(LogKind.Error, null, 9000036, detailInfomation);//Socket生成でエラーが発生しました。[UDP]
                return;
            }

            try {
                Socket.Bind(new IPEndPoint(ip.IPAddress, port));
            } catch (Exception e) {
                state = SocketObjState.Error;
                //Ver5.0.0-a8
                var detailInfomation = Util.SwapStr("\n", "", Util.SwapStr("\r", "", e.Message));
                logger.Set(LogKind.Error, null, 9000006, detailInfomation);//Socket.Bind()でエラーが発生しました。[UDP]
                return;
            }
            LocalEndPoint = (IPEndPoint)Socket.LocalEndPoint;
        }

        //【コンストラクタ（クライアント用）】（not クローン）
        public UdpObj(Kernel kernel, Logger logger, Ip ip, Int32 port)
            : base(kernel, logger, ip.InetKind) {
            RecvBuf = new byte[UdpBufferSize];//１パケットの最大サイズで受信待ちにする
            //socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Socket = new Socket((ip.InetKind == InetKind.V4) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

            RemoteEndPoint = new IPEndPoint(ip.IPAddress, port);
        }

        //受け取ったデータとEndPointでクローンオブジェクトを生成する（クローン）
        public UdpObj(Kernel kernel, Logger logger, InetKind inetKind, Socket socket, IPEndPoint remoteEndPoint, byte[] udpBuffer)
            : base(kernel, logger, inetKind) {
            //サーバオブジェクトからコピーされた場合は、clone=trueとなり、closeは無視される
            Clone = true;
            state = SocketObjState.Connect;
            Socket = socket;
            RecvBuf = udpBuffer;//受信が完了しているバッファ
            LocalEndPoint = (IPEndPoint)socket.LocalEndPoint;
            RemoteEndPoint = remoteEndPoint;
        }

        override public void StartServer(AsyncCallback callBack) {
            var retry = 10;
            state = SocketObjState.Idle;

            //待機開始
            if (callBack == null) {
                //UDPの場合、callBack==nullは許可していない
                state = SocketObjState.Error;
                //設計ミス
                Logger.Set(LogKind.Error, null, 9000007, "");//callBack関数が指定されていません[UDP]
            } else {
            //Ver5.0.0-a9
            //EndPoint ep = (EndPoint)new IPEndPoint(IPAddress.Any,0);
            again:
                var ep = (EndPoint)new IPEndPoint((InetKind == InetKind.V4) ? IPAddress.Any : IPAddress.IPv6Any, 0);
                try {
                    Socket.BeginReceiveFrom(RecvBuf, 0, RecvBuf.Length, SocketFlags.None, ref ep, callBack, this);
                } catch (Exception e) {
                    //Ver5.0.0-a8
                    var detailInfomation = Util.SwapStr("\n", "", Util.SwapStr("\r", "", e.Message));
                    Logger.Set(LogKind.Error, null, 9000008, detailInfomation);//BeginReceiveFrom()でエラーが発生しました[UDP]
                    retry--;
                    if (0 <= retry)
                        goto again;
                }
                RemoteEndPoint = (IPEndPoint)ep;
            }
        }

        override public SockObj CreateChildObj(IAsyncResult ar) {
            try {
                //いったん自分自身で受信データを受け取る
                var ep = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                var len = Socket.EndReceiveFrom(ar, ref ep);

                //受け取ったデータとEndPointでクローンを生成する
                return new UdpObj(Kernel, Logger, InetKind, Socket, (IPEndPoint)ep, Bytes.Create(RecvBuf, len));
                //} catch (Exception e) {
            } catch {
                return null;
            }
        }

        public void SendTo(byte[] sendBuffer) {
            if (RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork) {
                //警告 GetAddressBytes() を使用してください。
                //if (RemoteEndPoint.Address.Address == 0xffffffff) {
                var addrBytes = RemoteEndPoint.Address.GetAddressBytes();
                if (addrBytes[0] == 0xff && addrBytes[1] == 0xff && addrBytes[2] == 0xff && addrBytes[3] == 0xff) {
                    // ブロードキャストはこのオプション設定が必要
                    try {
                        Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    } catch {
                        return;
                    }
                }
                Socket.SendTo(sendBuffer, sendBuffer.Length, SocketFlags.None, RemoteEndPoint);
            } else { //IPv6
                Socket.SendTo(sendBuffer, sendBuffer.Length, SocketFlags.None, RemoteEndPoint);
            }
        }

        public bool ReceiveFrom(int timeout) {
            Socket.ReceiveTimeout = timeout;
            try {
                // EndPoint ep = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                EndPoint ep = RemoteEndPoint;
                var tmp = new byte[1620];
                var l = Socket.ReceiveFrom(tmp, ref ep);
                RecvBuf = new byte[l];
                Buffer.BlockCopy(tmp, 0, RecvBuf, 0, l);

                RemoteEndPoint = (IPEndPoint)ep;
            } catch {
                return false;
            }
            return true;
        }
    }
}*/
