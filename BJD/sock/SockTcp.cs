using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Bjd.net;
using Bjd.trace;
using Bjd.util;

namespace Bjd.sock{
    public class SockTcp : SockObj{

        //private Selector selector = null;
        //private SocketChannel channel = null; //ACCEPTの場合は、コンストラクタでコピーされる
        readonly Socket _socket;
        readonly Ssl _ssl;

        OneSsl _oneSsl;
        SockQueue _sockQueue = new SockQueue();
        //ByteBuffer recvBuf = ByteBuffer.allocate(sockQueue.Max);
        byte[] _recvBuf;//１行処理のためのテンポラリバッファ

        //***************************************************************************
        //パラメータのKernelはSockObjにおけるTrace()のためだけに使用されているので、
        //Traceしない場合は削除することができる
        //***************************************************************************

        protected SockTcp(Kernel kernel)
            : base(kernel) {
            //隠蔽
        }

        //CLIENT
        public SockTcp(Kernel kernel,Ip ip, int port, int timeout, Ssl ssl):base(kernel){
            //SSL通信を使用する場合は、このオブジェクトがセットされる 通常の場合は、null
            _ssl = ssl;

            _socket = new Socket((ip.InetKind == InetKind.V4) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            try {
                //socket.Connect(ip.IPAddress, port);
                _socket.BeginConnect(ip.IPAddress, port, CallbackConnect, this);
            } catch {
                SetError("BeginConnect() faild");
            }
            //[C#] 接続が完了するまで待機する
            while (SockState == SockState.Idle){
                Thread.Sleep(10);
                
            }
            //************************************************
            //ここまでくると接続が完了している
            //************************************************
        }


        //通常のサーバでは、このファンクションを外部で作成する
        void CallbackConnect(IAsyncResult ar) {
            if (_socket.Connected) {
                _socket.EndConnect(ar);
                //ここまでくると接続が完了している
                if (_ssl != null) {//SSL通信の場合は、SSLのネゴシエーションが行われる
                    _oneSsl = _ssl.CreateClientStream(_socket);
                    if (_oneSsl == null) {
                        SetError("_ssl.CreateClientStream() faild");
                        return;
                    }
                }
                BeginReceive();//接続完了処理（受信待機開始）
            } else {
                SetError("CallbackConnect() faild");
            }
        }


        //ACCEPT
        public SockTcp(Kernel kernel,Socket s):base(kernel){

            //************************************************
            //selector/channel生成
            //************************************************
            _socket = s;

            //既に接続を完了している
            if (_ssl != null) { //SSL通信の場合は、SSLのネゴシエーションが行われる
//                _oneSsl = _ssl.CreateServerStream(socket);
//                if (_oneSsl == null) {
//                    SetError("SSL.CreateServerStream() faild.");
//                    return;
//                }
            }

            //************************************************
            //ここまでくると接続が完了している
            //************************************************
            //Set(SockState.Connect, (InetSocketAddress) channel.socket().getLocalSocketAddress(), (InetSocketAddress) channel.socket().getRemoteSocketAddress());

            //************************************************
            //read待機
            //************************************************
            BeginReceive();//接続完了処理（受信待機開始）
        }

        public int Length(){
            Thread.Sleep(1); //次の動作が実行されるようにsleepを置く
            return _sockQueue.Length;
        }

        //接続完了処理（受信待機開始）
        private void BeginReceive() {
            //受信バッファは接続完了後に確保される
            _sockQueue = new SockQueue();
            _recvBuf = new byte[_sockQueue.Space];//キューが空なので、Spaceはバッファの最大サイズになっている

            // Using the LocalEndPoint property.
            string s = string.Format("My local IpAddress is :" + IPAddress.Parse(((IPEndPoint)_socket.LocalEndPoint).Address.ToString()) + "I am connected on port number " + ((IPEndPoint)_socket.LocalEndPoint).Port.ToString());


            try {//Ver5.6.0
                Set(SockState.Connect,(IPEndPoint) _socket.LocalEndPoint,(IPEndPoint)_socket.RemoteEndPoint);
            } catch {
                SetError("set IPENdPoint faild.");
                return;
            }

            //受信待機の開始(oneSsl!=nullの場合、受信バイト数は0に設定する)
            //socket.BeginReceive(tcpBuffer, 0, (oneSsl != null) ? 0 : tcpQueue.Space, SocketFlags.None, new AsyncCallback(EndReceive), this);
            try {
                if (_ssl != null) {
                    //_oneSsl.BeginRead(_recvBuf, 0, sockQueue.Space, EndReceive, this);
                } else {
                    _socket.BeginReceive(_recvBuf, 0, _sockQueue.Space, SocketFlags.None, EndReceive, this);
                }
            } catch {
                SetError("BeginRecvive() faild.");
            }
        }

        //受信処理・受信待機
        public void EndReceive(IAsyncResult ar) {
            if (ar == null) { //受信待機
                while ((_sockQueue.Space) == 0) {
                    Thread.Sleep(10);//他のスレッドに制御を譲る  
                    if (SockState != SockState.Connect)
                        goto err;
                }
            } else { //受信完了
                lock (this) { //ポインタを移動する場合は、排他制御が必要
                    try {
                        //int bytesRead = _oneSsl != null ? _oneSsl.EndRead(ar) : Socket.EndReceive(ar);
                        int bytesRead = _socket.EndReceive(ar);
                        if (bytesRead == 0) {
                            //  切断されている場合は、0が返される?
                            if (_ssl == null)
                                goto err;//エラー発生
                            Thread.Sleep(10);//Ver5.0.0-a19
                        } else if (bytesRead < 0) {
                            goto err;//エラー発生
                        } else {
                            _sockQueue.Enqueue(_recvBuf, bytesRead);//キューへの格納
                        }
                    } catch {
                        //受信待機のままソケットがクローズされた場合は、ここにくる
                        goto err;//エラー発生
                    }
                }
            }

            if (_sockQueue.Space == 0)
                //バッファがいっぱい 空の受信待機をかける
                EndReceive(null);
            else
                //受信待機の開始
                try {
                    //if (_oneSsl != null) {
                    //    _oneSsl.BeginRead(_recvBuf, 0, sockQueue.Space, EndReceive, this);
                    //} else {
                        _socket.BeginReceive(_recvBuf, 0, _sockQueue.Space, SocketFlags.None, EndReceive, this);
                    //}
                } catch {
                    goto err;//切断されている
                }
            return;
        err://エラー発生

            //【2009.01.12 追加】相手が存在しなくなっている
            SetError("disconnect");
            //state = SocketObjState.Disconnect;

            //Close();クローズは外部から明示的に行う
        }


	    //受信<br>
	    //切断・タイムアウトでnullが返される
        public byte[] Recv(int len, int timeout, ILife iLife){

            var tout = new util.Timeout(timeout);

            var buffer = new byte[0];
            try{
                if (len <= _sockQueue.Length){
                    // キューから取得する
                    buffer = _sockQueue.Dequeue(len);

                } else{
                    while (iLife.IsLife()){
                        Thread.Sleep(0);
                        if (0 < _sockQueue.Length){
                            //Java fix 
                            tout.Update();//少しでも受信があった場合は、タイムアウトを更新する

                            //size=受信が必要なバイト数
                            int size = len - buffer.Length;

                            //受信に必要なバイト数がバッファにない場合
                            if (size > _sockQueue.Length){
                                size = _sockQueue.Length; //とりあえずバッファサイズ分だけ受信する
                            }
                            byte[] tmp = _sockQueue.Dequeue(size);
                            buffer = Bytes.Create(buffer, tmp);

                            //Java fix Ver5.8.2
                            if (buffer.Length != 0){
                                break;
                            }
                        } else{
                            if (SockState != SockState.Connect){
                                return null;
                            }
                            Thread.Sleep(10);
                        }
                        if (tout.IsFinish()){
                            buffer = _sockQueue.Dequeue(len); //タイムアウト
                            break;
                        }
                    }
                }
            } catch (Exception){
                //ex.printStackTrace();
                return null;
            }
            Trace(TraceKind.Recv, buffer, false);

            return buffer;
        }

        //1行受信
	    //切断・タイムアウトでnullが返される
        public byte[] LineRecv(int sec, ILife iLife){
            //Socket.ReceiveTimeout = timeout * 1000;

            var tout = new util.Timeout(sec*1000);

            while (iLife.IsLife()){
                //Ver5.1.6
                if (_sockQueue.Length == 0){
                    Thread.Sleep(100);
                }
                byte[] buf = _sockQueue.DequeueLine();
                //noEncode = false;//テキストである事が分かっている
                Trace(TraceKind.Recv, buf, false);
                if (buf.Length != 0) {
                    //Ver5.8.6 Java fix
                    tout.Update();//タイムアウトの更新
                    return buf;
                }
                if (SockState != SockState.Connect){
                    return null;
                }
                if (tout.IsFinish()){
                    return null; //タイムアウト
                }
                Thread.Sleep(1);
            }
            return null;
        }

	    //１行のString受信
        public String StringRecv(String charsetName, int sec, ILife iLife){
            try{
                byte[] bytes = LineRecv(sec, iLife);
                
                //[C#]
                if (bytes == null){
                    return null;
                }

                return Encoding.GetEncoding(charsetName).GetString(bytes);
            } catch (Exception e){
                Util.RuntimeException(e.Message);
            }
            return null;
        }

        //１行受信(ASCII)
	    public String StringRecv(int sec, ILife iLife){
            return StringRecv("ASCII", sec, iLife);
        }


        
        public int Send(byte[] buf,int length){
            try{
                //return _oneSsl != null ? _oneSsl.Write(buf, buf.Length) : Socket.Send(buf, SocketFlags.None);
                if (buf.Length != length){
                    var b = new byte[length];
                    Buffer.BlockCopy(buf, 0, b, 0, length);
                    Trace(TraceKind.Send, b, false);
                } else{
                    Trace(TraceKind.Send, buf, false);
                }
                return _socket.Send(buf, length, SocketFlags.None);
            } catch (Exception e) {
                SetException(e);
                return -1;
            }
        }

        public int Send(byte[] buf){
            return Send(buf,buf.Length);
        }

        //1行送信
	    //内部でCRLFの２バイトが付かされる
        public int LineSend(byte[] buf){
            var b = new byte[buf.Length+2];
            Buffer.BlockCopy(buf, 0, b, 0, buf.Length);
            b[buf.Length] = 0x0d;
            b[buf.Length + 1] = 0x0a;
            return Send(b);
        }

        //１行のString送信(ASCII)  (\r\nが付加される)
        public bool StringSend(String str) {
            return StringSend(str, "ASCII");
        }

        //１行のString送信 (\r\nが付加される)
        public bool StringSend(String str, String charsetName){
            try{

                var buf = Encoding.GetEncoding(charsetName).GetBytes(str);
                //byte[] buf = str.getBytes(charsetName);
                LineSend(buf);
                return true;
            } catch (Exception e){
                Util.RuntimeException(e.Message);
            }
            return false;
        }

        //１行送信(ASCII)  (\r\nが付加される)
        public bool SstringSend(String str){
            return StringSend(str, "ASCII");
        }

        public override void Close(){
            //ACCEPT・CLIENT
//            if (channel != null && channel.isOpen()){
//                try{
//                    selector.wakeup();
//                    channel.close();
//                } catch (IOException ex){
//                    //ex.printStackTrace(); //エラーは無視する
//                }
//            }
//            if (_oneSsl != null) {
//                _oneSsl.Close();
//            }

            try{
                this._socket.Shutdown(SocketShutdown.Both);
            } catch{
                //TCPのサーバソケットをシャットダウンするとエラーになる（無視する）
            }
            _socket.Close();
            if (_oneSsl != null){
                _oneSsl.Close();
            }

            SetError("close()");
        }

        //【送信】(トレースなし)
        //リモートサーバがトレース内容を送信するときに更にトレースするとオーバーフローするため
        //RemoteObj.Send()では、こちらを使用する
        public int SendNoTrace(byte[] buffer) {
            try {
                if (_oneSsl != null) {
                    //return _oneSsl.Write(buffer, buffer.Length);
                }
                if (_socket.Connected)
                    return _socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            } catch (Exception ex) {
                SetError(string.Format("Length={0} {1}", buffer.Length, ex.Message));
                //Logger.Set(LogKind.Error, this, 9000046, string.Format("Length={0} {1}", buffer.Length, ex.Message));
            }
            return -1;
        }


        //【送信】テキスト（バイナリかテキストかが不明な場合もこちら）
        public int SendUseEncode(byte[] buf) {
            //テキストである可能性があるのでエンコード処理は省略できない
            Trace(TraceKind.Send, buf, false);//noEncode = false テキストである可能性があるのでエンコード処理は省略できない
            //実際の送信処理にテキストとバイナリの区別はない
            return SendNoTrace(buf);
        }
        
        
        /*******************************************************************/
        //以下、C#のコードを通すために設置（最終的に削除の予定）
        /*******************************************************************/
        private String _lastLineSend = "";

        public string LastLineSend {
            get { return _lastLineSend; }
        }


        //内部でASCIIコードとしてエンコードする１行送信  (\r\nが付加される)
        //LineSend()のオーバーライドバージョン
        //public int AsciiSend(string str, OperateCrlf operateCrlf) {
        public int AsciiSend(string str) {
            _lastLineSend = str;
            var buf = Encoding.ASCII.GetBytes(str);
            //return LineSend(buf, operateCrlf);
            //とりあえずCrLfの設定を無視している
            return LineSend(buf);
        }
        //AsciiSendを使用したいが、文字コードがASCII以外の可能性がある場合、こちらを使用する  (\r\nが付加される)
        //public int SjisSend(string str, OperateCrlf operateCrlf) {
        public int SjisSend(string str) {
            _lastLineSend = str;
            var buf = Encoding.GetEncoding("shift-jis").GetBytes(str);
            //return LineSend(buf, operateCrlf);
            //とりあえずCrLfの設定を無視している
            return LineSend(buf);
        }

        // 【１行受信】
        //切断されている場合、nullが返される
        //public string AsciiRecv(int timeout, OperateCrlf operateCrlf, ILife iLife) {
        public string AsciiRecv(int timeout, ILife iLife) {
            var buf = LineRecv(timeout, iLife);
            return buf == null ? null : Encoding.ASCII.GetString(buf);
        }

        //【送信】バイナリ
        public int SendNoEncode(byte[] buf)
        {
            //バイナリであるのでエンコード処理は省略される
            Trace(TraceKind.Send, buf, true);//noEncode = true バイナリであるのでエンコード処理は省略される
            //実際の送信処理にテキストとバイナリの区別はない
            return SendNoTrace(buf);
        }
    }
}



/*
 using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Bjd.log;
using Bjd.sock;
using Bjd.util;

namespace Bjd.net {
    //TCP接続用
    public class sockTcp : SockObj {
        //受信用バッファ（接続完了後に[BeginReceive()の中で]確保される）
        protected TcpQueue TcpQueue;
        byte[] _tcpBuffer;//１行処理のためのテンポラリバッファ

        readonly Ssl _ssl;
        OneSsl _oneSsl;

        //最後にLineSendで送信した文字列
        string _lastLineSend = "";
        public string LastLineSend {
            get {
                return _lastLineSend;
            }
        }

        //【コンストラクタ（クライアント用）】
        //public sockTcp(Kernel kernel, Logger logger, Ip ip, Int32 port, float fff, Ssl ssl)
        public sockTcp(Kernel kernel, Logger logger, Ip ip, Int32 port, Ssl ssl)
            : base(kernel, logger, ip.InetKind) {
            //SSL通信を使用する場合は、このオブジェクトがセットされる
            //通常の場合は、null
            _ssl = ssl;

            //socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket = new Socket((ip.InetKind == InetKind.V4) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            try {
                //socket.Connect(ip.IPAddress, port);
                Socket.BeginConnect(ip.IPAddress, port, CallbackConnect, this);
            } catch {
                state = SocketObjState.Disconnect;
            }
            //ここまでくると接続が完了している
            //BeginReceive();//接続完了処理（受信待機開始）
        }

        ////通常のサーバでは、このファンクションを外部で作成する
        //void callbackConnect(IAsyncResult ar) {
        //    if (socket.Connected) {
        //        socket.EndConnect(ar);
        //        //ここまでくると接続が完了している
        //        if (ssl != null) {//SSL通信の場合は、SSLのネゴシエーションが行われる
        //            oneSsl = ssl.CreateClientStream(socket);
        //            if (oneSsl == null) {
        //                state = SOCKET_OBJ_STATE.ERROR;
        //                return;
        //            }
        //        }
        //        BeginReceive();//接続完了処理（受信待機開始）
        //    } else {
        //        state = SOCKET_OBJ_STATE.ERROR;
        //    }
        //}
        //通常のサーバでは、このファンクションを外部で作成する
        void CallbackConnect(IAsyncResult ar) {
            if (Socket.Connected) {
                Socket.EndConnect(ar);
                //ここまでくると接続が完了している
                if (_ssl != null) { //SSL通信の場合は、SSLのネゴシエーションが行われる
                    //Ver5.3.6 ssl.CreateClientStream()の例外をトラップ
                    try {
                        _oneSsl = _ssl.CreateClientStream(Socket);
                    } catch (Exception ex) {
                        Logger.Set(LogKind.Error, this, 9000057, ex.Message);//Ver5.3.6 例外表示
                        _oneSsl = null;
                    }
                    if (_oneSsl == null) {
                        state = SocketObjState.Error;
                        return;
                    }
                }
                BeginReceive();//接続完了処理（受信待機開始）
            } else {
                state = SocketObjState.Error;
            }
        }

        //【コンストラクタ（サーバ用）】bind/listen
        public sockTcp(Kernel kernel, Logger logger, Ip ip, Int32 port, int listenMax, Ssl ssl)
            : base(kernel, logger, ip.InetKind) {
            //SSL通信を使用する場合は、このオブジェクトがセットされる
            //通常の場合は、null
            _ssl = ssl;
            if (ssl != null && !ssl.Status) { //SSLの初期化に失敗している
                state = SocketObjState.Error;
                logger.Set(LogKind.Error, null, 9000028, "");
                return;
            }
            try {
                //socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket = new Socket((ip.InetKind == InetKind.V4) ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            } catch (Exception e) {
                state = SocketObjState.Error;
                //Ver5.0.0-a8
                string detailInfomation = Util.SwapStr("\n", "", Util.SwapStr("\r", "", e.Message));
                logger.Set(LogKind.Error, null, 9000035, detailInfomation);//Socket生成でエラーが発生しました。[TCP]
                return;
            }

            try {
                Socket.Bind(new IPEndPoint(ip.IPAddress, port));
            } catch (Exception e) {
                state = SocketObjState.Error;
                //Ver5.0.0-a8
                var detailInfomation = Util.SwapStr("\n", "", Util.SwapStr("\r", "", e.Message));
                logger.Set(LogKind.Error, null, 9000009, detailInfomation);//Socket.Bind()でエラーが発生しました。[TCP]
                return;
            }
            try {
                Socket.Listen(listenMax);
            } catch (Exception e) {
                state = SocketObjState.Error;
                //Ver5.0.0-a8
                var detailInfomation = Util.SwapStr("\n", "", Util.SwapStr("\r", "", e.Message));
                logger.Set(LogKind.Error, null, 9000010, detailInfomation);//Socket.Listen()でエラーが発生しました。[TCP]
                return;
            }
            LocalEndPoint = (IPEndPoint)Socket.LocalEndPoint;
            //準備完了
            //StartServerを実行すると待ち受け状態になる
        }

        //サーバでAcceptしたsocketから初期化される、子ソケット
        public sockTcp(Kernel kernel, Logger logger, InetKind inetKind, Socket socket, Ssl ssl)
            : base(kernel, logger, inetKind) {
            _ssl = ssl;

            Socket = socket;

            //既に接続を完了している
            if (ssl != null) { //SSL通信の場合は、SSLのネゴシエーションが行われる
                _oneSsl = ssl.CreateServerStream(socket);
                if (_oneSsl == null) {
                    state = SocketObjState.Error;
                    return;
                }
            }
            BeginReceive();//接続完了処理（受信待機開始）
        }

        override public void StartServer(AsyncCallback callBack){
            //待機開始
            Socket.BeginAccept(callBack ?? AcceptFunc, this);
        }

        override public SockObj CreateChildObj(IAsyncResult ar) {
            try {
                return new sockTcp(Kernel, Logger, InetKind, Socket.EndAccept(ar), _ssl);
            } catch {
                //ソケットがクローズされてからもここへ到達する可能性が有るため
                return null;
            }
        }

        //通常のサーバでは、このファンクションを外部で作成する
        void AcceptFunc(IAsyncResult ar) {
            try { //Ver5.1.3-b5
                //自分自身を複製するため、いったん別のSocketで受け取る必要がある
                Socket newSocket = Socket.EndAccept(ar);
                //socket.Shutdown(SocketShutdown.Both);これはエラーになる
                Socket.Close();

                //新しいソケットで置き換える
                Socket = newSocket;

                if (_ssl != null) { //SSL通信の場合は、SSLのネゴシエーションが行われる
                    _oneSsl = _ssl.CreateClientStream(Socket);
                    if (_oneSsl == null) {
                        state = SocketObjState.Error;
                        return;
                    }
                }
                BeginReceive();//接続完了処理（受信待機開始）
            } catch {
                //Ver5.1.3-b5
                state = SocketObjState.Error;
            }
        }

        //接続完了処理（受信待機開始）
        private void BeginReceive() {
            //受信バッファは接続完了後に確保される
            TcpQueue = new TcpQueue();
            _tcpBuffer = new byte[TcpQueue.Space];//キューが空なので、Spaceはバッファの最大サイズになっている

            try {//Ver5.6.0
                LocalEndPoint = (IPEndPoint)Socket.LocalEndPoint;
                RemoteEndPoint = (IPEndPoint)Socket.RemoteEndPoint;
            } catch {
                state = SocketObjState.Error;
                return;
            }

            //if (ssl != null) {//SSL通信の場合は、SSLのネゴシエーションが行われる
            //    oneSsl = new OneSsl(socket,ssl.TargetServer);
            //    if (oneSsl == null) {
            //        state = SOCKET_OBJ_STATE.ERROR;
            //        return;
            //    }
            //}
            state = SocketObjState.Connect;

            //Log(LogKind.Detail, -1, string.Format("connected Local={0} Remote={1}", LocalEndPoint.ToString(), RemoteEndPoint.ToString()));

            //受信待機の開始(oneSsl!=nullの場合、受信バイト数は0に設定する)
            //socket.BeginReceive(tcpBuffer, 0, (oneSsl != null) ? 0 : tcpQueue.Space, SocketFlags.None, new AsyncCallback(EndReceive), this);
            try {
                if (_ssl != null) {
                    _oneSsl.BeginRead(_tcpBuffer, 0, TcpQueue.Space, EndReceive, this);
                } else {
                    Socket.BeginReceive(_tcpBuffer, 0, TcpQueue.Space, SocketFlags.None, EndReceive, this);
                }
            } catch {
                state = SocketObjState.Error;
            }
        }

        //受信処理・受信待機
        public void EndReceive(IAsyncResult ar) {
            if (ar == null) { //受信待機
                while ((TcpQueue.Space) == 0) {
                    Thread.Sleep(10);//他のスレッドに制御を譲る  
                    if (state != SocketObjState.Connect)
                        goto err;
                }
            } else { //受信完了
                lock (this) { //ポインタを移動する場合は、排他制御が必要
                    try{
                        int bytesRead = _oneSsl != null ? _oneSsl.EndRead(ar) : Socket.EndReceive(ar);
                        if (bytesRead == 0){
                            //  切断されている場合は、0が返される?
                            if (_ssl == null)
                                goto err;//エラー発生
                            Thread.Sleep(10);//Ver5.0.0-a19
                        }
                        else if (bytesRead < 0) {
                            goto err;//エラー発生
                        } else {
                            TcpQueue.Enqueue(_tcpBuffer, bytesRead);//キューへの格納
                        }
                    }
                    catch {
                        //受信待機のままソケットがクローズされた場合は、ここにくる
                        goto err;//エラー発生
                    }
                }
            }

            if (TcpQueue.Space == 0)
                //バッファがいっぱい 空の受信待機をかける
                EndReceive(null);
            else
                //受信待機の開始
                try {
                    if (_oneSsl != null) {
                        _oneSsl.BeginRead(_tcpBuffer, 0, TcpQueue.Space, EndReceive, this);
                    } else {
                        Socket.BeginReceive(_tcpBuffer, 0, TcpQueue.Space, SocketFlags.None, EndReceive, this);
                    }
                } catch {
                    goto err;//切断されている
                }
            return;
        err://エラー発生

            //【2009.01.12 追加】相手が存在しなくなっている
            state = SocketObjState.Disconnect;

            //Close();クローズは外部から明示的に行う
        }

        //内部でASCIIコードとしてエンコードする１行送信
        //LineSend()のオーバーライドバージョン
        public int AsciiSend(string str, OperateCrlf operateCrlf) {
            _lastLineSend = str;
            var buf = Encoding.ASCII.GetBytes(str);
            return LineSend(buf, operateCrlf);
        }

        //AsciiSendを使用したいが、文字コードがASCII以外の可能性がある場合、こちらを使用する
        public int SjisSend(string str, OperateCrlf operateCrlf) {
            _lastLineSend = str;
            var buf = Encoding.GetEncoding("shift-jis").GetBytes(str);
            return LineSend(buf, operateCrlf);
        }

        public int LineSend(byte[] buf, OperateCrlf operateCrlf) {
            if (operateCrlf == OperateCrlf.Yes) {
                buf = Bytes.Create(buf, new byte[] { 0x0d, 0x0a });
            }

            //noEncode = false;//テキストである事が分かっている
            Trace(TraceKind.Send, buf, false);//トレース表示

            //int offset = 0;
            try {
                return _oneSsl != null ? _oneSsl.Write(buf, buf.Length) : Socket.Send(buf, SocketFlags.None);
            } catch {
                return -1;
            }
        }

        public int Length() {
            return TcpQueue.Length;
        }

        // 【１行受信】
        //切断されている場合、nullが返される
        public string AsciiRecv(int timeout, OperateCrlf operateCrlf, ref bool life) {
            var buf = LineRecv(timeout, operateCrlf, ref life);
            return buf == null ? null : Encoding.ASCII.GetString(buf);
        }

        // 【１行受信】
        //切断されている場合、nullが返される
        public byte[] LineRecv(int timeout, OperateCrlf operateCrlf, ref bool life) {
            Socket.ReceiveTimeout = timeout * 1000;

            var breakTime = DateTime.Now.AddSeconds(timeout);

            while (life) {
                //Ver5.1.6
                if (TcpQueue.Length == 0)
                    Thread.Sleep(100);
                var buf = TcpQueue.DequeueLine();
                //noEncode = false;//テキストである事が分かっている
                Trace(TraceKind.Recv, buf, false);//トレース表示
                if (buf.Length != 0) {
                    if (operateCrlf == OperateCrlf.Yes) {
                        buf = Inet.TrimCrlf(buf);
                    }
                    return buf;
                }
                //【2009.01.12 追加】
                if (!Socket.Connected) {
                    state = SocketObjState.Disconnect;
                    return null;
                }
                if (state != SocketObjState.Connect)
                    return null;
                if (DateTime.Now > breakTime)
                    return null;
                //Thread.Sleep(100);//<=これ待ちすぎ？Ver5.0.0-b22
                Thread.Sleep(1);//
            }
            return null;
        }


        //【バイナリ受信】
        public bool RecvBinary(string fileName, ref bool life) {
            const int max = 65535; //処理するブロックサイズ
            var result = false;

            //トレース表示
            var sb = new StringBuilder();
            sb.Append(string.Format("RecvBinaryFile({0}) ", fileName));

            var fs = new FileStream(fileName, FileMode.Create);
            var bw = new BinaryWriter(fs);
            fs.Seek(0, SeekOrigin.Begin);

            try {
                while (life) {
                    Thread.Sleep(0);
                    // キューから取得する
                    var buffer = TcpQueue.Dequeue(max);
                    if (buffer == null) {
                        //logger.Set(LogKind.Debug, 9000011,"");//"tcpQueue().Dequeue()=null"
                        if (state != SocketObjState.Connect) {
                            //logger.Set(LogKind.Debug,9000012,"");//"tcpQueue().Dequeue() SocektObjState != SOCKET_OBJ_STATE.CONNECT break"
                            result = true;
                            break;
                        }
                        //Thread.Sleep(100);
                        Thread.Sleep(1);
                    } else {
                        //logger.Set(LogKind.Debug, 9000013 , string.Format("buffer.Length={0}byte", buffer.Length));//"tcpQueue().Dequeue()";
                        bw.Write(buffer, 0, buffer.Length);

                        //トレース表示
                        sb.Append(string.Format("Binary={0}byte ", buffer.Length));
                    }
                }
            } catch {
            }
            bw.Flush();
            bw.Close();
            fs.Close();

            //noEncode = true バイナリである事が分かっている
            Trace(TraceKind.Recv, Encoding.ASCII.GetBytes(sb.ToString()), true);//トレース表示
            return result;
        }

        ////【バイナリ送信】
        public bool SendBinaryFile(string fileName, ref bool life) {
            //トレース表示
            var sb = new StringBuilder();
            sb.Append(string.Format("SendBinaryFile({0}) ", fileName));

            var buffer = new byte[3000000];
            var result = false;
            if (File.Exists(fileName)) {
                try {
                    using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        using (var br = new BinaryReader(fs)) {
                            fs.Seek(0, SeekOrigin.Begin);
                            var offset = 0L;
                            while (life) {
                                var len = fs.Length - offset;
                                if (len == 0) {
                                    result = true;
                                    break;
                                }
                                if (len > buffer.Length) {
                                    len = buffer.Length;
                                }
                                len = br.Read(buffer, 0, (int)len);

                                //トレース表示
                                sb.Append(string.Format("Binary={0}byte ", len));

                                try {
                                    if (_oneSsl != null) {
                                        _oneSsl.Write(buffer, (int)len);
                                    } else {
                                        Socket.Send(buffer, 0, (int)len, SocketFlags.None);
                                    }
                                } catch (Exception e) {
                                    //Ver5.0.0-a8
                                    string detailInfomation = Util.SwapStr("\n", "", Util.SwapStr("\r", "", e.Message));
                                    Logger.Set(LogKind.Error, null, 9000014, detailInfomation);//"SendBinaryFile(string fileName) socket.Send()"
                                    break;
                                }

                                offset += len;
                                fs.Seek(offset, SeekOrigin.Begin);
                            }
                            br.Close();
                        }
                        fs.Close();
                    }
                } catch (Exception ex) {
                    Logger.Set(LogKind.Error, null, 9000050, ex.Message);
                }
            }
            //noEncode = true;//バイナリである事が分かっている
            Trace(TraceKind.Send, Encoding.ASCII.GetBytes(sb.ToString()), true);//トレース表示
            return result;
        }

        //【送信】(トレースなし)
        //リモートサーバがトレース内容を送信するときに更にトレースするとオーバーフローするため
        //RemoteObj.Send()では、こちらを使用する
        public int SendNoTrace(byte[] buffer) {
            try {
                if (_oneSsl != null) {
                    return _oneSsl.Write(buffer, buffer.Length);
                }
                if (Socket.Connected)
                    return Socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            } catch (Exception ex) {
                Logger.Set(LogKind.Error, this, 9000046, string.Format("Length={0} {1}", buffer.Length, ex.Message));
            }
            return -1;
        }

        //【送信】バイナリ
        public int SendNoEncode(byte[] buf) {
            //バイナリであるのでエンコード処理は省略される
            Trace(TraceKind.Send, buf, true);//noEncode = true バイナリであるのでエンコード処理は省略される
            //実際の送信処理にテキストとバイナリの区別はない
            return SendNoTrace(buf);
        }

        //【送信】テキスト（バイナリかテキストかが不明な場合もこちら）
        public int SendUseEncode(byte[] buf) {
            //テキストである可能性があるのでエンコード処理は省略できない
            Trace(TraceKind.Send, buf, false);//noEncode = false テキストである可能性があるのでエンコード処理は省略できない
            //実際の送信処理にテキストとバイナリの区別はない
            return SendNoTrace(buf);
        }

        public byte[] Recv(int len, int timeout) {
            var dt = DateTime.Now.AddSeconds(timeout);
            var buffer = new byte[0];
            try {
                if (len <= TcpQueue.Length) {
                    // キューから取得する
                    buffer = TcpQueue.Dequeue(len);
                } else {
                    while (true) {
                        Thread.Sleep(0);
                        if (0 < TcpQueue.Length) {
                            //size=受信が必要なバイト数
                            var size = len - buffer.Length;
                            //受信に必要なバイト数がバッファにない場合
                            if (size > TcpQueue.Length)
                                size = TcpQueue.Length;//とりあえずバッファサイズ分だけ受信する
                            byte[] tmp = TcpQueue.Dequeue(size);
                            buffer = Bytes.Create(buffer, tmp);
                            if (len <= buffer.Length) {
                                break;
                            }
                        } else {
                            if (state != SocketObjState.Connect) {
                                return null;
                            }
                            //Thread.Sleep(300);
                            Thread.Sleep(10);//Ver5.0.0-a19
                        }
                        if (dt < DateTime.Now) {
                            buffer = TcpQueue.Dequeue(len);//タイムアウト
                            break;
                        }
                    }
                }
            } catch {
                return null;
            }
            Trace(TraceKind.Recv, buffer, false);//noEncode = false;テキストかバイナリかは不明
            return buffer;
        }

        //【ソケットクローズ】
        override public void Close() {
            if (_oneSsl != null) {
                _oneSsl.Close();
            }
            base.Close();
        }
    }
}*/