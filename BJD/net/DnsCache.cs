using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Bjd.log;
using Bjd.util;

namespace Bjd.net{
    //DNSのキャッシュ
    public class DnsCache{

        private List<OneDnsCache> ar = new List<OneDnsCache>();

        //IPアドレスからホスト名を検索する（逆引き）
        //return 取得したIPアドレスの配列 検索に失敗した場合、検索した文字列がそのまま返される
        public String GetHostName(IPAddress ipaddress, Logger logger){
            lock (this){

                String ipStr = ipaddress.ToString();
                if (ipStr[0] == '/'){
                    ipStr = ipStr.Substring(1);
                }

                foreach (OneDnsCache oneDnsCache in ar){
                    foreach (Ip ip in oneDnsCache.IpList){
                        if (ip.ToString() == ipStr){
                            return oneDnsCache.Name;
                        }
                    }
                }

                RemoveOldCache(); //古いものを整理する

                //DNSに問い合わせる
                //String hostName = ipaddress.ToString();//.getHostName();
                var hostName = "";
                try{
                    var hostInfo = Dns.GetHostByAddress(ipaddress.ToString());
                    hostName = hostInfo.HostName;
                }
                catch{
                    hostName = ipaddress.ToString();
                }


                if (hostName == ipStr){
                    if (logger != null){
                        logger.Set(LogKind.Normal, null, 9000052, string.Format("IP={0}", ipStr));
                    }
                }

                //データベースへの追加
                Ip[] ipList = new Ip[1];
                try{
                    ipList[0] = new Ip(ipStr);
                } catch (ValidObjException e){
                    //ここで失敗するのはおかしい
                    Util.RuntimeException(string.Format("new Ip({0}) => ValidObjException ({1})", ipStr,e.Message));
                }
                ar.Add(new OneDnsCache(hostName, ipList));
                return hostName;
            }
        }

        //キャッシュの件数取得(デバッグ用)
        public int size(){
            return ar.Count;
        }

        //キャッシュの容量制限
        private void RemoveOldCache(){
            if (ar.Count > 200){
                for (int i = 0; i < 50; i++){
                    //古いものから50件削除
                    ar.RemoveAt(0);
                }
            }
        }

        //ホスト名からIPアドレスを検索する(正引き)
        //return 取得したIPアドレスの配列 検索に失敗した場合、0件の配列が返される
        public Ip[] GetAddress(String hostName){
            lock (this){
                //データベースから検索して取得する
                foreach (var oneDnsCache in ar){
                    if (oneDnsCache.Name.ToUpper() == hostName.ToUpper() ){
                        return oneDnsCache.IpList;
                    }
                }
                RemoveOldCache(); //古いものを整理する


                //DNSに問い合わせる
                IPHostEntry list = null;
                try{
                    list = Dns.GetHostEntry(hostName);
                    if (list.AddressList.Length == 0){
                        return new Ip[0]; //名前が見つからない場合
                    }
                }catch (Exception){
                    return new Ip[0];
                    
                }

                List<Ip> tmp = new List<Ip>();
                foreach (IPAddress addr in list.AddressList) {
                    //IPv4及びIPv6以外は処理しない
                    if (addr.AddressFamily != AddressFamily.InterNetwork && addr.AddressFamily != AddressFamily.InterNetworkV6){
                        continue;
                    }
                    //Ipv6の場合　リンクローカル・マルチキャスト・サイトローカルは対象外とする
                    if (addr.AddressFamily == AddressFamily.InterNetworkV6){
                        if (addr.IsIPv6LinkLocal || addr.IsIPv6Multicast || addr.IsIPv6SiteLocal){
                            continue;
                        }
                    }
                    String ipStr = addr.ToString();//.getHostAddress();
                    try{
                        tmp.Add(new Ip(ipStr));
                    } catch (ValidObjException e){
                        //ここで失敗するのはおかしい
                        Util.RuntimeException(string.Format("new Ip({0}) => ValidObjException ({1})", ipStr,e.Message));
                    }
                }
                Ip[] ipList = tmp.ToArray();
                //データベースへの追加
                ar.Add(new OneDnsCache(hostName, ipList));
                return ipList;
            }

        }
    }
}
        /*
        readonly List<OneDnsCache> _ar = new List<OneDnsCache>();

        public List<Ip> Get(string hostName){
            lock (this) {

                foreach (OneDnsCache oneDnsCache in _ar) {
                    if (oneDnsCache.Name.ToUpper() == hostName.ToUpper()) {
                        return oneDnsCache.IpList;
                    }
                }

                var ipList = new List<Ip>();
                try {
                    var iphe = Dns.GetHostEntry(hostName);
                    if (iphe.AddressList.Length == 0)
                        return null;
                    ipList.AddRange(iphe.AddressList.Select(ipAddress => new Ip(ipAddress.ToString())));
                    _ar.Add(new OneDnsCache(hostName,ipList));
                } catch {
                    return null;
                }
                if (_ar.Count > 500) {
                    _ar.RemoveRange(0,50);
                }
                return ipList;
            }

        }
        public string Get(IPAddress ipaddress,Logger logger) {
            lock (this) {

                foreach (OneDnsCache oneDnsCache in _ar) {
                    if (oneDnsCache.IpList.Any(ip => ip.IPAddress.ToString() == ipaddress.ToString())){
                        return oneDnsCache.Name;
                    }
                }

                string hostName;
                try {
                    var hostInfo = Dns.GetHostEntry(ipaddress);


                    hostName = hostInfo.HostName;
                    if (hostName != null) {
                        var ipList = new List<Ip>{new Ip(ipaddress.ToString())};
                        _ar.Add(new OneDnsCache(hostName,ipList));
                    }
                } catch(Exception ex) {
                    logger.Set(LogKind.Normal, null, 9000052, string.Format("IP={0} {1}",ipaddress,ex.Message));
                    return "";
                }
                if (_ar.Count > 500) {
                    _ar.RemoveRange(0,50);
                }
                return hostName;
            }
        }
    }
}
        */