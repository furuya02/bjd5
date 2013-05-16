using System;
using System.Net;
using Bjd;
using Bjd.ctrl;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;

namespace DhcpServer {

    public partial class Server : OneServer {
        readonly Lease _lease;//データベース
        readonly Object _lockObj = new object();//排他制御オブジェクト

        readonly string _serverAddress;//サーバアドレス

        readonly Ip _maskIp; //マスク
        readonly Ip _gwIp;   //ゲートウエイ
        readonly Ip _dnsIp0; //ＤＮＳ（プライマリ）
        readonly Ip _dnsIp1; //ＤＮＳ（セカンダリ）
        readonly int _leaseTime;//リース時間
        readonly string _wpadUrl;//WPAD

        //コンストラクタ
        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel, conf, oneBind) {

                //オプションの読み込み
                _maskIp = (Ip)Conf.Get("maskIp");
                _gwIp = (Ip)Conf.Get("gwIp");
                _dnsIp0 = (Ip)Conf.Get("dnsIp0");
                _dnsIp1 = (Ip)Conf.Get("dnsIp1");
                _leaseTime = (int)Conf.Get("leaseTime");
            if (_leaseTime <= 0) 
                _leaseTime = 86400;
            if ((bool)Conf.Get("useWpad")) {
                _wpadUrl = (string)Conf.Get("wpadUrl");
            }

            
            //DB生成
            string fileName = string.Format("{0}\\lease.db", kernel.ProgDir());
            var startIp = (Ip)Conf.Get("startIp");
            var endIp = (Ip)Conf.Get("endIp");
            _macAcl = (Dat)Conf.Get("macAcl");
            //設定が無い場合は、空のDatを生成する
            if (_macAcl == null){
                _macAcl = new Dat(new CtrlType[]{CtrlType.TextBox,CtrlType.AddressV4, CtrlType.TextBox});
            }

            //Ver5.6.8
            //カラム数「名前（表示名)」を増やしたことによる互換性保持
            if (_macAcl.Count > 0) {
                foreach (OneDat t in _macAcl){
                    if (t.StrList.Count == 2) {
                        t.StrList.Add(string.Format("host_{0}",t.StrList[1]));
                    }
                }
            }
            _lease = new Lease(fileName, startIp, endIp, _leaseTime, _macAcl);
            
            //サーバアドレスの初期化
            _serverAddress = Define.ServerAddress();
        }


        //リモート操作（データの取得）
        public override String Cmd(string cmdStr) {
            if (cmdStr == "Refresh-Lease") {
                return _lease.GetInfo();
            }
            return "";
        }



        new public void Dispose() {
            _lease.Dispose();
            
            base.Dispose();
        }
        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }
        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {

            var sockUdp = (SockUdp)sockObj;
            if (sockUdp.RemoteAddress.Port != 68) {// 接続元ポート番号が68以外は、DHCPパケットではないので破棄する
                return;
            }

            //パケットの読込(受信パケットrp)            
            var rp = new PacketDhcp();
            if (!rp.Read(sockUdp.RecvBuf)) 
                return; //データ解釈に失敗した場合は、処理なし

            if (rp.Opcode != 1) 
                return;//OpCodeが「要求」で無い場合は、無視する
            
            //送信先をブロードキャストに設定する
            var ep = new IPEndPoint(IPAddress.Broadcast, 68);
            sockUdp.RemoteAddress = ep;

            //********************************************************
            // MAC制御
            //********************************************************
            if ((bool)Conf.Get("useMacAcl")) {// MAC制御が有効な場合
                if (!_lease.SearchMac(rp.Mac)) {
                    Logger.Set(LogKind.Secure,sockUdp,1,rp.Mac.ToString());
                    return;
                }
            }

            // 排他制御 (データベース整合のため)
            lock (_lockObj) {

                //サーバアドレス
                Ip serverIp = rp.ServerIp;
                if (serverIp.AddrV4 == 0) {
                    serverIp = new Ip(_serverAddress);
                }
                //リクエストアドレス
                Ip requestIp = rp.RequestIp;

                //this.Logger.Set(LogKind.Detail,sockUdp,3,string.Format("{0} {1} {2}",rp.Mac,requestIp.ToString(),rp.Type.ToString()));
                Log(sockUdp, 3, rp.Mac, requestIp, rp.Type);

                if (rp.Type == DhcpType.Discover) {// 検出

                    requestIp = _lease.Discover(requestIp, rp.Id, rp.Mac);
                    if(requestIp!=null){
                        // OFFER送信
                        var sp = new PacketDhcp(rp.Id,requestIp,serverIp,rp.Mac,DhcpType.Offer,_leaseTime,_maskIp,_gwIp,_dnsIp0,_dnsIp1,_wpadUrl);
                        Send(sockUdp,sp);
                    }
                } else if (rp.Type == DhcpType.Request) {// 要求

                    requestIp = _lease.Request(requestIp, rp.Id, rp.Mac);
                    if (requestIp != null) {

                        if (serverIp.ToString() == _serverAddress) {// 自サーバ宛て
                            // ACK送信
                            var sp = new PacketDhcp(rp.Id,requestIp,serverIp,rp.Mac,DhcpType.Ack,_leaseTime,_maskIp,_gwIp,_dnsIp0,_dnsIp1,_wpadUrl);
                            Send(sockUdp,sp);

                            //this.Logger.Set(LogKind.Normal,sockUdp,5,string.Format("{0} {1} {2}",rp.Mac,requestIp.ToString(),rp.Type.ToString()));
                            Log(sockUdp, 5, rp.Mac, requestIp, rp.Type);
                        } else {
                            _lease.Release(rp.Mac);//無効化する
                        }

                    } else {
                        // NACK送信
                        var sp = new PacketDhcp(rp.Id,requestIp,serverIp,rp.Mac,DhcpType.Nak,_leaseTime,_maskIp,_gwIp,_dnsIp0,_dnsIp1,_wpadUrl);
                        Send(sockUdp,sp);
                    }
                } else if (rp.Type == DhcpType.Release) {// 開放
                    requestIp = _lease.Release(rp.Mac);//開放
                    if(requestIp!=null)
                        //this.Logger.Set(LogKind.Normal,sockUdp,6,string.Format("{0} {1} {2}",rp.Mac,requestIp.ToString(),rp.Type.ToString()));
                        Log(sockUdp, 6, rp.Mac, requestIp, rp.Type);
                } else if (rp.Type == DhcpType.Infrm) {// 情報
                    // ACK送信
                    //Send(sockUdp,sp);
                }
            }// 排他制御
        }
        //レスポンスパケットの送信
        void Send(SockUdp sockUdp,PacketDhcp sp) {
            
            //送信
            sockUdp.Send(sp.GetBuffer());
            //this.Logger.Set(LogKind.Detail,sockUdp,4,string.Format("{0} {1} {2}",sp.Mac,(sp.RequestIp == null) ? "0.0.0.0" : sp.RequestIp.ToString(),sp.Type.ToString()));
            Log(sockUdp, 4, sp.Mac,sp.RequestIp,sp.Type);
        }

        void Log(SockUdp sockUdp,int messageNo, Mac mac,Ip ip,DhcpType type) {
            string macStr = mac.ToString();
            foreach (var m in _macAcl) {
                if (m.StrList[0].ToUpper() == mac.ToString()) {
                    macStr = string.Format("{0}({1})",mac,m.StrList[2]);
                    break;
                }
            }
            Logger.Set(LogKind.Detail, sockUdp, messageNo, string.Format("{0} {1} {2}", macStr, (ip == null) ? "0.0.0.0" : ip.ToString(), type.ToString()));
        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }
}
