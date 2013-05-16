using System;
using System.Collections.Generic;
using System.Linq;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyHttpServer {
    class Proxy:IDisposable {
        readonly Kernel _kernel;
        public Logger Logger { get; private set; }
        public int OptionTimeout { get; private set; }
        //ソケット
        Dictionary<CS,SockTcp> _sock = new Dictionary<CS,SockTcp>(2);
        //上位プロキシ情報
        public UpperProxy UpperProxy { get; private set; }
        //接続中のサーバ情報(URLから取得したホスト名)
        public string HostName="";
        public int Port = 0;//Ver5.0.1
        
        public Proxy(Kernel kernel,Logger logger,SockTcp clientSocket,int optionTimeout,UpperProxy upperProxy) {
            _kernel = kernel;
            Logger = logger;
            OptionTimeout = optionTimeout;
            UpperProxy = upperProxy;

            _sock[CS.Client] = clientSocket;
            _sock[CS.Server] = null;
            //sock[CS.CLIENT].SendTimeout = optionTimeout; //Ver5.0.2 送信タイムアウトは設定しない

            ProxyProtocol = ProxyProtocol.Unknown;
        }
        // 終了処理
        public void Dispose() {
            if(_sock[CS.Client] != null)
                _sock[CS.Client].Close();
            if(_sock[CS.Server] != null)
                _sock[CS.Server].Close();

            //Ver5.0.0-b3 使用終了を明示的に記述する
            foreach(CS cs in Enum.GetValues(typeof(CS))) {
                _sock[cs] = null;
            }
            _sock = null;
        }

        public ProxyProtocol ProxyProtocol { get; private set; }
        
        public SockTcp Sock(CS cs) {
            return _sock[cs];
        }

        //ソケットに到着しているデータ量
        public int Length(CS cs) {
            return _sock[cs].Length();
        }
        
        public void NoConnect(string host,int port) {//キャッシュにヒットした場合に、サーバ側のダミーソケットを作成する
            _sock[CS.Server] = new SockTcp(_kernel,new Ip(IpKind.V4_0),0,0,null);
            HostName = host;//Request.HostNameを保存して、現在接続中のホストを記憶する
            Port = port;
        }

        public bool Connect(ILife iLife, string host1, int port1, string requestStr, ProxyProtocol proxyProtocol) {

            ProxyProtocol = proxyProtocol;

            if(Sock(CS.Server) != null) {
                //Ver5.0.1
                //if(_host == HostName) {
                if(host1 == HostName && port1==Port) {
                    return true;
                }
                //Ver5.0.0-b21
                Sock(CS.Server).Close();
            }

            if(UpperProxy.Use) {//上位プロキシのチェック
                // 上位プロキシを経由しないサーバの確認
                foreach(string address in UpperProxy.DisableAdderssList) {
                    if (ProxyProtocol == ProxyProtocol.Ssl) {
                        if(host1.IndexOf(address) == 0) {
                            UpperProxy.Use = false;
                            break;
                        }
                    } else {
                        string str = requestStr.Substring(11);
                        if(str.IndexOf(address) == 0) {
                            UpperProxy.Use = false;
                            break;
                        }
                    }

                }
            }
            
            string host = host1;
            int port = port1;
            if(UpperProxy.Use) {
                host = UpperProxy.Server;
                port = UpperProxy.Port;
            }

            List<Ip> ipList = null;
            try{
                ipList = new List<Ip>();
                ipList.Add(new Ip(host));
            }catch (ValidObjException){
                ipList = _kernel.DnsCache.GetAddress(host).ToList();
                if(ipList == null || ipList.Count == 0) {
                    Logger.Set(LogKind.Error,null,11,host);
                    return false;
                }
            }

            Ssl ssl = null;
            foreach(Ip ip in ipList){
                int timeout = 3;
                _sock[CS.Server] = Inet.Connect(_kernel,ip,port,timeout,ssl);
                if(_sock[CS.Server] != null)
                    break;
            }
            if(_sock[CS.Server] == null) {
                Logger.Set(LogKind.Detail,_sock[CS.Client],26,string.Format("{0}:{1}",ipList[0],port));
                return false;
            }
            //sock[CS.SERVER].SendTimeout = OptionTimeout;//Ver5.0.2 送信タイムアウトは設定しない

            HostName = host1;//Request.HostNameを保存して、現在接続中のホストを記憶する
            //Ver5.6.1
            Port = port1;
            return true;

        }

    }
}
