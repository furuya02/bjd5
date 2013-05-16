using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Bjd.log;
using Bjd.net;
using Bjd.trace;
using Bjd.util;

namespace Bjd.sock{
    //SockTcp 及び SockUdp の基底クラス
    public abstract class SockObj{

        //****************************************************************
        // アドレス関連
        //****************************************************************
        public IPEndPoint RemoteAddress { get; set; }
        public IPEndPoint LocalAddress { get; set; }
        public String RemoteHostname { get; private set; }

        //このKernelはTrace()のためだけに使用されているので、Traceしない場合は削除することができる
        protected Kernel Kernel;

        public Ip LocalIp{
            get{
                var strIp = "0.0.0.0";
                if (LocalAddress != null){
                    strIp = LocalAddress.Address.ToString();
                }
                return new Ip(strIp);
            }
        }

        public Ip RemoteIp{
            get{
                var strIp = "0.0.0.0";
                if (RemoteAddress != null){
                    strIp = RemoteAddress.Address.ToString();
                }
                return new Ip(strIp);
            }
        }

        protected SockObj(Kernel kernel){
            Kernel = kernel;
            SockState = SockState.Idle;
            LocalAddress = null;
            RemoteAddress = null;
        }

        //****************************************************************
        // LastError関連
        //****************************************************************
        private String _lastError = "";

        //LastErrorの取得
        public String GetLastEror(){
            return _lastError;
        }

        //****************************************************************
        // SockState関連
        //****************************************************************
        public SockState SockState { get; private set; }


        //ステータスの設定
        //Connect/bindで使用する
        protected void Set(SockState sockState, IPEndPoint localAddress, IPEndPoint remoteAddress){
            SockState = sockState;
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
        }

        //****************************************************************
        // エラー（切断）発生時にステータスの変更とLastErrorを設定するメソッド
        //****************************************************************
        protected void SetException(Exception ex){
            _lastError = string.Format("[{0}] {1}", ex.Source, ex.Message);
            SockState = SockState.Error;
        }

        protected void SetError(String msg){
            _lastError = msg;
            SockState = SockState.Error;
        }



        //TODO メソッドの配置はここでよいか？
        public void Resolve(bool useResolve, Logger logger){
            if (useResolve){
                RemoteHostname = "resolve error!";
                try{
                    RemoteHostname = Kernel.DnsCache.GetHostName(RemoteAddress.Address, Kernel.CreateLogger("SockObj", true, null));
                } catch (Exception ex){
                    logger.Set(LogKind.Error, null, 9000053, ex.Message);
                }
            } else{
                String ipStr = RemoteAddress.Address.ToString();
                if (ipStr[0] == '/'){
                    ipStr = ipStr.Substring(1);
                }
                RemoteHostname = ipStr;
            }

        }

        public abstract void Close();

        
        //バイナリデータであることが判明している場合は、noEncodeをtrueに設定する
        //これによりテキスト判断ロジックを省略できる
        protected void Trace(TraceKind traceKind, byte[] buf, bool noEncode){

            if (buf == null || buf.Length == 0){
                return;
            }

            if (Kernel.RunMode == RunMode.Remote){
                return; //リモートクライアントの場合は、ここから追加されることはない
            }

            //Ver5.0.0-a22 サービス起動の場合は、このインスタンスは生成されていない
            bool enableDlg = Kernel.TraceDlg != null && Kernel.TraceDlg.Visible;
            if (!enableDlg && Kernel.RemoteConnect == null){
                //どちらも必要ない場合は処置なし
                return;
            }

            bool isText = false; //対象がテキストかどうかの判断
            Encoding encoding = null;

            if (!noEncode){
                //エンコード試験が必要な場合
                try{
                    encoding = MLang.GetEncoding(buf);
                } catch{
                    encoding = null;
                }
                if (encoding != null){
                    //int codePage = encoding.CodePage;
                    if (encoding.CodePage == 20127 || encoding.CodePage == 65001 || encoding.CodePage == 51932 || encoding.CodePage == 1200 || encoding.CodePage == 932 || encoding.CodePage == 50220){
                        //"US-ASCII" 20127
                        //"Unicode (UTF-8)" 65001
                        //"日本語(EUC)" 51932
                        //"Unicode" 1200
                        //"日本語(シフトJIS)" 932
                        //日本語(JIS) 50220
                        isText = true;
                    }
                }
            }

            var ar = new List<String>();
            if (isText){
                var lines = Inet.GetLines(buf);
                ar.AddRange(lines.Select(line => encoding.GetString(Inet.TrimCrlf(line))));
            } else{
                ar.Add(noEncode ? string.Format("binary {0} byte", buf.Length) : string.Format("Binary {0} byte", buf.Length));
            }
            foreach (var str in ar){
                Ip ip = RemoteIp;

                if (enableDlg){
                    //トレースダイアログが表示されてい場合、データを送る
                    Kernel.TraceDlg.AddTrace(traceKind, str, ip);
                }
                if (Kernel.RemoteConnect != null){
                    //リモートサーバへもデータを送る（クライアントが接続中の場合は、クライアントへ送信される）
                    Kernel.RemoteConnect.AddTrace(traceKind, str, ip);
                }
            }

        }
    }
}



    /*
    　  //Socketその他を保持するクラス(１つの接続を表現している)
    public abstract class SockObj {
        public Socket Socket;
        protected Logger Logger;
        protected Kernel Kernel;
        protected SocketObjState state = SocketObjState.Idle;

        //****************************************************************
        //プロパティ
        //****************************************************************
        //Ver5.4.1 名称変更
        //public string RemoteHostName { get; private set; }
        public string RemoteHost { get; private set; }
        //Ver5.4.1新設
        public Ip RemoteAddr {get;set;}
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public SocketObjState State {
            get {
                if (state == SocketObjState.Connect && !Socket.Connected) {
                    state = SocketObjState.Disconnect;
                }
                return state;
            }
        }
        public InetKind InetKind{get;private set;}

        //クローン
        //UDPサーバオブジェクトからコピーされた場合は、clone=trueとなり、closeは無視される
        protected bool Clone;

        public int SendTimeout {
            set {
                Socket.SendTimeout = 1000 * value;
            }
        }

        //****************************************************************
        //コンストラクタ
        //****************************************************************
        protected SockObj(Kernel kernel,Logger logger,InetKind inetKind) {
            Kernel = kernel;
            Logger = logger;
            InetKind = inetKind;


            //Verr5.4.1
            //RemoteHostName = "";//接続先のホスト名
            RemoteHost = "";//接続先のホスト名
            //Ver5.4.1
            RemoteAddr = new Ip(IpKind.V4_0);
        }

        //TCPの場合 EndAccept() UDPの場合 EndReceiveFrom()
        abstract public SockObj CreateChildObj(IAsyncResult ar);
        
        //TCPの場合 BeginAccept() UDPの場合BeginReceiveFrom()
        abstract public void StartServer(AsyncCallback callBack);


        //【ソケットクローズ】
        virtual public void Close() {
            if (Clone) {//クローンの場合は破棄しない
                return;
            }
            state = SocketObjState.Disconnect;
            try {
                Socket.Shutdown(SocketShutdown.Both);
            } catch {
                //TCPのサーバソケットをシャットダウンするとエラーになる（無視する）
            }
            if(Socket!=null)
                Socket.Close();
        }
        //【2009.01.13 追加】IPアドレスからホスト名を取得する
        public void Resolve(bool useResolve,Logger logger) {
            if (useResolve) {
                RemoteHost = "resolve error!";
                try {
                    RemoteHost = Kernel.DnsCache.Get(RemoteEndPoint.Address,logger);
                } catch(Exception ex) {
                    logger.Set(LogKind.Error, null, 9000053, ex.Message);
                }
            } else {
                RemoteHost = RemoteEndPoint.Address.ToString();
            }
        }
        //バイナリデータであることが判明している場合は、noEncodeをtrueに設定する
        //これによりテキスト判断ロジックを省略できる
        protected void Trace(TraceKind traceKind,byte [] buf,bool noEncode) {

            if (buf == null || buf.Length == 0)
                return;

            if (Kernel.RunMode == RunMode.Remote)
                return;//リモートクライアントの場合は、ここから追加されることはない

            //Ver5.0.0-a22 サービス起動の場合は、このインスタンスは生成されていない
            bool enableDlg = Kernel.TraceDlg != null && Kernel.TraceDlg.Visible;
            if (!enableDlg && Kernel.RemoteServer==null) {
                //どちらも必要ない場合は処置なし
                return;
            }

            var isText = false;//対象がテキストかどうかの判断
            Encoding encoding = null;

            if(!noEncode) {//エンコード試験が必要な場合
                try {
                    encoding = MLang.GetEncoding(buf);
                } catch {
                    encoding = null;
                }
                if(encoding != null) {
                    //int codePage = encoding.CodePage;
                    if(encoding.CodePage == 20127 || encoding.CodePage == 65001 || encoding.CodePage == 51932 || encoding.CodePage == 1200 || encoding.CodePage == 932 || encoding.CodePage == 50220) {
                        //"US-ASCII" 20127
                        //"Unicode (UTF-8)" 65001
                        //"日本語(EUC)" 51932
                        //"Unicode" 1200
                        //"日本語(シフトJIS)" 932
                        //日本語(JIS) 50220
                        isText = true;
                    }
                }
            }

            var ar = new List<string>();
            if (isText){
                var lines = Inet.GetLines(buf);
                ar.AddRange(lines.Select(line => encoding.GetString(Inet.TrimCrlf(line))));
            }
            else {
                ar.Add(noEncode
                           ? string.Format("binary {0} byte", buf.Length)
                           : string.Format("Binary {0} byte", buf.Length));
            }
            foreach (string str in ar) {
                var ip = new Ip(RemoteEndPoint.Address.ToString());

                if(enableDlg) {//トレースダイアログが表示されてい場合、データを送る
                    Kernel.TraceDlg.AddTrace(traceKind,str,ip);
                }
                if(Kernel.RemoteServer!=null) {//リモートサーバへもデータを送る（クライアントが接続中の場合は、クライアントへ送信される）
                    Kernel.RemoteServer.AddTrace(traceKind,str,ip);
                }
            }

        }

    }
}
      */                                                                                     