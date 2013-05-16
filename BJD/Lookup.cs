using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using System.Drawing;
//using System.Collections;
//using System.Windows.Forms;
//using System.Data;
//追加した名前空間
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Management;
using Bjd.util;


namespace Bjd {
    public class Lookup {

        private Lookup(){}//デフォルトコンストラクタの隠蔽
        
        // DNSサーバアドレスを取得する(設定値取得)
        static public List<string> DnsServer() {
            var list = new List<string>();    
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var moc = mc.GetInstances();
            foreach (var mo in moc) {
                if ((bool)mo["IPEnabled"]){
                    var dnsSet = (string[])mo["DNSServerSearchOrder"];
                    if (dnsSet != null) {
                        list.AddRange(dnsSet);
                    }
                }
            }
            return list;
        }
        static public List<string> QueryA(string hostName){
            var addrList = new List<string>();
            try{
                var hostEntry = Dns.GetHostEntry(hostName);
                addrList.AddRange(hostEntry.AddressList.Select(ipAddress => ipAddress.ToString()));
            }
            catch {
            }
            return addrList;
        }
        
        static public List<string> QueryMx(string domainName,string dnsServer) {
            var hostList = new List<string>();
            var noList = new List<int>();

            var s = domainName.Split('.');

            //送信バッファの長さ
            var len = 16;
            foreach (var ss in s) {
                len += ss.Length;
                len++;
            }
            len++;
            //送信用バッファを用意する
            var buffer = new byte[len];

    
            //識別子の生成
            var id= new byte[2];
            var rnd = new RNGCryptoServiceProvider();
            rnd.GetNonZeroBytes(id);

            Array.Copy(id, buffer, 2);
            buffer[2] = 0x01;
            //buffer[3] = 0x00;
            //buffer[4] = 0x00;
            buffer[5] = 0x01;
            //buffer[6] = 0x00;
            //buffer[7] = 0x00;
            //buffer[8] = 0x00;
            //buffer[9] = 0x00;
            //buffer[10] = 0x00;
            //buffer[11] = 0x00;

            //質問セクションの初期化
            var p = 12;
            foreach (var tmp in s) {
                buffer[p++] = (byte)tmp.Length;
                Encoding.ASCII.GetBytes(tmp, 0, tmp.Length, buffer, p);
                p += tmp.Length;
            }
            buffer[p++] = 0x00;
            buffer[p++] = 0x00;
            buffer[p++] = 0x0F;
            buffer[p++] = 0x00;
            buffer[p++] = 0x01;

            //クエリーの送信
            
            
            //UdpClient udpClient = new UdpClient();
            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(dnsServer), 53);
            //udpClient.Connect(endPoint);//connect
            //udpClient.Send(buffer, p);//send
            //buffer = new byte[512];//バッファを受信用に初期化
            //buffer = udpClient.Receive(ref endPoint);//deceive
            //udpClient.Close();


            var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var endPoint = new IPEndPoint(IPAddress.Parse(dnsServer), 53);
            //3秒でタイムアウト
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);

            //byte[] q = Encoding.ASCII.GetBytes(query);
            client.SendTo(buffer,p,SocketFlags.None, endPoint);//送信
            //IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            
            var senderEP = (EndPoint)endPoint;
            try {
                var data = new byte[1024];
                var recv = client.ReceiveFrom(data, ref senderEP);//受信
                buffer = new byte[recv];
                Buffer.BlockCopy(data, 0, buffer, 0, recv);
                client.Close();
            } catch {//タイムアウト
                client.Close();
                return hostList;
            }


            //識別子の確認
            if (buffer[0]!=id[0] || buffer[1]!=id[1])
                return hostList;

            //Qcount
            p = 4;
            var qcount = (short)Util.htons(BitConverter.ToUInt16(buffer,p));
            p += 2;
            if (qcount == 0)
                return hostList;
            
            //Acount
            var acount = (short)Util.htons(BitConverter.ToUInt16(buffer,p));
            if (acount == 0)
                return hostList;
            

            p = 12;
            //Questionをジャンプする
            while (buffer[p] != 0x00) 
                p++;
            p += 5;

            for (int i = 0; i< acount; i++) {
                //NAMEをスキップ
                while (true) {
                    if (buffer[p] >= 0xC0) {
                        p += 2;
                        break;
                    }
                    if (buffer[p] == 0x00) {
                        p++;
                        break;
                    }
                    p++;
                }

                p += 8; //TYPE(2),CLASS(2),TTL(4) 合計8バイト

                //リソースの長さ
                var rlen = (short)Util.htons(BitConverter.ToUInt16(buffer,p));
                p += 2;
                int offset = p;//リソースの先頭位置
                //リファレンス数取得
                var preference = (short)Util.htons(BitConverter.ToUInt16(buffer,offset));
                offset += 2;
                //ホスト名取得
                var host = "";
                while (true) {
                    if (buffer[offset] == 0x00)
                        break;
                    if (buffer[offset] >= 0xC0) {//圧縮形式
                        //offset = (int)Util.htons(Bytes.ReadUInt16(buffer,offset));
                        offset = Util.htons(BitConverter.ToUInt16(buffer, offset));
                        offset = offset & 0x3FFF;
                    } else {
                        int nlen = buffer[offset++];
                        host += Encoding.ASCII.GetString(buffer, offset, nlen);
                        host += ".";
                        offset += nlen;
                    }
                }
                //リファレンス数の小さいものをリストの最初に入れる
                var set = false;
                for (int n = 0; n < noList.Count; n++) {
                    if (preference < noList[n]) {
                        noList.Insert(n, preference);
                        hostList.Insert(n, host);
                        set = true;
                        break;
                    }
                }
                if (!set) {
                    hostList.Add(host);
                    noList.Add(preference);
                }
                p += rlen; //次のレコード位置へジャンプ
            }
            return hostList;
        }
    }
}
