using System;
using System.Collections.Generic;
using System.Text;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Bjd.util;

namespace Bjd.net {
    public class LocalAddress : ValidObj{

        private List<Ip> _v4 = new List<Ip>();
        private List<Ip> _v6 = new List<Ip>();
    	public Ip[] V4 {
            get{
                CheckInitialise();
                return _v4.ToArray();
            }
    	}
    	public Ip[] V6 {
            get{
                CheckInitialise();
                return _v6.ToArray();
            }
    	}

	    //プログラムで唯一のインスタンスを返す
    	private static LocalAddress _localAddress = null;
	    public static LocalAddress GetInstance(){
	        return _localAddress ?? (_localAddress = new LocalAddress());
	    }
        //リモートから生成する
        public static void SetInstance(String str){
            _localAddress = new LocalAddress(str);
        }


        // (隠蔽)コンストラクタ
	    //インターフェース状態を読み込んでリストを初期化する
	    private LocalAddress() {
		    Init(); //初期化
		    _v4.Add(new Ip(IpKind.InAddrAny));
		    _v6.Add(new Ip(IpKind.In6AddrAnyInit));

            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in nics) {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;
                var props = nic.GetIPProperties();
                foreach (var info in props.UnicastAddresses) {
                    if (info.Address.AddressFamily == AddressFamily.InterNetwork) {
                        _v4.Add(new Ip(info.Address.ToString()));
                    } else if (info.Address.AddressFamily == AddressFamily.InterNetworkV6) {
                        if (info.Address.IsIPv6LinkLocal == false && info.Address.IsIPv6Multicast == false && info.Address.IsIPv6SiteLocal == false){
                            var s = info.Address.ToString();
                            try{
                                var ip = new Ip(s);
                                _v6.Add(ip);
                            } catch (ValidObjException) {
                                //システムから返された文字列でIpを初期化して例外が出るという事は、実行時例外とするしかない
                                Util.RuntimeException(String.Format("inetAddress={0}", s)); //実行時例外
                            }
                        }
                    }
                }
            }
	    }


        //コンストラクタ(リモートから受信した文字列による初期化)<br>
	    //remoteStr()で作成された文字列以外が挿入された場合、リスク回避のため、初期化失敗としてオブジェクトを利用を禁止する<br>
        //Remoteから取得した文字列でオブジェクトを生成する
        public LocalAddress(string str){
            Init(); //初期化

            var tmp = str.Split('\t');
            if (tmp.Length != 2){
                ThrowException(str); //例外終了
            }


            foreach (var s in tmp[0].Split(new[]{'\b'}, StringSplitOptions.RemoveEmptyEntries)){
                try{
                    var ip = new Ip(s);
                    _v4.Add(ip);
                } catch (ValidObjException) {
                    ThrowException(str); //例外終了
                }
            }
            foreach (var s in tmp[1].Split(new[]{'\b'}, StringSplitOptions.RemoveEmptyEntries)){
                try{
                    var ip = new Ip(s);
                    _v6.Add(ip);
                } catch (ValidObjException) {
                    ThrowException(str); //例外終了
                }
            }
        }

        //Remoteへの送信文字列
        public string RemoteStr() {
            var sb = new StringBuilder();
            foreach (var ip in _v4) {
                sb.Append(ip + "\b");
            }
            sb.Append("\t");
            foreach (var ip in _v6) {
                sb.Append(ip + "\b");
            }
            return sb.ToString();
        }

        protected override sealed void Init(){
    		_v4 = new List<Ip>();
	    	_v6 = new List<Ip>();
        }
    }
 
  
}

