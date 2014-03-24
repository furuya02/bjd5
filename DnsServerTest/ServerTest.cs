using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;
using BjdTest.test;
using DnsServer;
using NUnit.Framework;

namespace DnsServerTest{


    //このテストを成功させるには、c:\dev\bjd5\BJD\outにDnsServer.dllが必要
    public class ServerTest{

        private static TmpOption _op; //設定ファイルの上書きと退避
        private static Server _sv; //サーバ

        [TestFixtureSetUp]
        public static void BeforeClass(){

            //named.caのコピー
            var src = string.Format("{0}\\DnsServerTest\\named.ca", TestUtil.ProjectDirectory());
            var dst = string.Format("{0}\\BJD\\out\\named.ca", TestUtil.ProjectDirectory());
            File.Copy(src, dst, true);

            //設定ファイルの退避と上書き
            _op = new TmpOption("DnsServerTest","DnsServerTest.ini");
            OneBind oneBind = new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Udp);
            Kernel kernel = new Kernel();
            var option = kernel.ListOption.Get("Dns");
            Conf conf = new Conf(option);

            //サーバ起動
            _sv = new Server(kernel, conf, oneBind);
            _sv.Start();


        }

        [TestFixtureTearDown]
        public static void AfterClass(){

            //サーバ停止
            _sv.Stop();
            _sv.Dispose();

            //設定ファイルのリストア
            _op.Dispose();

        }

        // 共通メソッド
	    // リクエスト送信して、サーバから返ったデータをDNSパケットとしてデコードする
	    // レスポンスが無い場合は、1秒でタイムアウトしてnullを返す
        // rd = 再帰要求
        private PacketDns lookup(DnsType dnsType, string name,bool rd=false){
            //乱数で識別子生成
            var id = (ushort) (new Random()).Next(100);
            //送信パケット生成
            var sp = new PacketDns(id, false, false, rd, false);
            //質問フィールド追加
            sp.AddRr(RrKind.QD, new RrQuery(name, dnsType));
            //クライアントソケット生成、及び送信
            var cl = new SockUdp(new Kernel(), new Ip(IpKind.V4Localhost), 53, null, sp.GetBytes());
            //受信
            //byte[] recvBuf = cl.Recv(1000);
            var recvBuf = cl.Recv(3);
            
            if (recvBuf.Length == 0){
                //受信データが無い場合
                return null;
            }
            //System.out.println(string.Format("lookup(%s,\"%s\") recv().Length=%d", dnsType, name, recvBuf.Length));
            //デコード
            var p = new PacketDns(recvBuf);
            //System.out.println(print(p));
            return p;
        }

        //共通メソッド
	    //リソースレコードの数を表示する
        private static string Print(PacketDns p){
            return string.Format("QD={0} AN={1} NS={2} AR={3}", p.GetCount(RrKind.QD), p.GetCount(RrKind.AN), p.GetCount(RrKind.NS), p.GetCount(RrKind.AR));
        }

	    // 共通メソッド
	    //リソースレコードのToString()
        private string Print(PacketDns p, RrKind rrKind, int n){
            var o = p.GetRr(rrKind, n);
            if (rrKind == RrKind.QD){
                return o.ToString();
            }
            return Print(o);
        }

	    // 共通メソッド
	    // リソースレコードのToString()
        private string Print(OneRr o){
            switch (o.DnsType){
                case DnsType.A:
                    return o.ToString();
                case DnsType.Aaaa:
                    return o.ToString();
                case DnsType.Ns:
                    return o.ToString();
                case DnsType.Mx:
                    return o.ToString();
                case DnsType.Ptr:
                    return o.ToString();
                case DnsType.Soa:
                    return o.ToString();
                case DnsType.Cname:
                    return o.ToString();
                default:
                    Util.RuntimeException("not implement.");
                    break;
            }
            return "";
        }

        [Test]
        public void ステータス情報_ToString_の出力確認(){

            var expected = "+ サービス中 \t                 Dns\t[127.0.0.1\t:UDP 53]\tThread";

            //exercise
            var actual = _sv.ToString().Substring(0, 56);
            //verify
            Assert.That(actual, Is.EqualTo(expected));

        }

        [Test]
        public void localhostの検索_タイプA(){

            //exercise
            var p = lookup(DnsType.A, "localhost");
            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=1 AR=0"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query A localhost."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("A localhost. TTL=2400 127.0.0.1"));
            Assert.That(Print(p, RrKind.NS, 0), Is.EqualTo("Ns localhost. TTL=2400 localhost."));
        }

        [Test]
        public void localhostの検索_タイプAAAA(){
            //exercise
            var p = lookup(DnsType.Aaaa, "localhost");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=1 AR=0"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Aaaa localhost."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Aaaa localhost. TTL=2400 ::1"));
            Assert.That(Print(p, RrKind.NS, 0), Is.EqualTo("Ns localhost. TTL=2400 localhost."));
        }

        [Test]
        public void localhostの検索_タイプPTR(){
            //exercise
            var p = lookup(DnsType.Ptr, "localhost");
            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=0 NS=0 AR=0"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Ptr localhost."));
        }

        [Test]
        public void localhost_V4の検索_タイプPTR(){
            //exercise
            var p = lookup(DnsType.Ptr, "1.0.0.127.in-addr.arpa");
            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=0 AR=0"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Ptr 1.0.0.127.in-addr.arpa."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Ptr 1.0.0.127.in-addr.arpa. TTL=2400 localhost."));

        }

        [Test]
        public void localhost_V6の検索_タイプPTR(){
            //exercise
            var p = lookup(DnsType.Ptr, "1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa");
            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=0 AR=0"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Ptr 1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Ptr 1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa. TTL=2400 localhost."));
        }

        [Test]
        public void 自ドメインの検索_タイプA_www_aaa_com(){
            //exercise
            var p = lookup(DnsType.A, "www.aaa.com");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=1 AR=1"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query A www.aaa.com."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("A www.aaa.com. TTL=2400 192.168.0.10"));
            Assert.That(Print(p, RrKind.NS, 0), Is.EqualTo("Ns aaa.com. TTL=2400 ns.aaa.com."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A ns.aaa.com. TTL=2400 192.168.0.1"));
        }

        [Test]
        public void 自ドメインの検索_タイプA_xxx_aaa_com_存在しない(){
            //exercise
            var p = lookup(DnsType.A, "xxx.aaa.com");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=0 NS=1 AR=1"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query A xxx.aaa.com."));
            Assert.That(Print(p, RrKind.NS, 0), Is.EqualTo("Ns aaa.com. TTL=2400 ns.aaa.com."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A ns.aaa.com. TTL=2400 192.168.0.1"));
        }

        [Test]
        public void 自ドメインの検索_タイプNS_aaa_com(){
            //exercise
            var p = lookup(DnsType.Ns, "aaa.com");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=0 AR=1"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Ns aaa.com."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Ns aaa.com. TTL=2400 ns.aaa.com."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A ns.aaa.com. TTL=2400 192.168.0.1"));
        }

        [Test]
        public void 自ドメインの検索_タイプMX_aaa_com(){
            //exercise
            var p = lookup(DnsType.Mx, "aaa.com");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=0 AR=1"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Mx aaa.com."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Mx aaa.com. TTL=2400 15 smtp.aaa.com."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A smtp.aaa.com. TTL=2400 192.168.0.2"));
        }

        [Test]
        public void 自ドメインの検索_タイプAAAA_www_aaa_com(){
            //exercise
            var p = lookup(DnsType.Aaaa, "www.aaa.com");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=1 AR=1"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Aaaa www.aaa.com."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Aaaa www.aaa.com. TTL=2400 fe80::3882:6dac:af18:cba6"));
            Assert.That(Print(p, RrKind.NS, 0), Is.EqualTo("Ns aaa.com. TTL=2400 ns.aaa.com."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A ns.aaa.com. TTL=2400 192.168.0.1"));
        }

        [Test]
        public void 自ドメインの検索_タイプAAAA_xxx_aaa_com_存在しない(){
            //exercise
            var p = lookup(DnsType.Aaaa, "xxx.aaa.com");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=0 NS=1 AR=1"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Aaaa xxx.aaa.com."));
            Assert.That(Print(p, RrKind.NS, 0), Is.EqualTo("Ns aaa.com. TTL=2400 ns.aaa.com."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A ns.aaa.com. TTL=2400 192.168.0.1"));
        }

        [Test]
        public void 自ドメインの検索_タイプCNAME_www2_aaa_com(){
            //exercise
            var p = lookup(DnsType.Cname, "www2.aaa.com");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=1 AR=3"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Cname www2.aaa.com."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Cname www2.aaa.com. TTL=2400 www.aaa.com."));
            Assert.That(Print(p, RrKind.NS, 0), Is.EqualTo("Ns aaa.com. TTL=2400 ns.aaa.com."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A www.aaa.com. TTL=2400 192.168.0.10"));
            Assert.That(Print(p, RrKind.AR, 1), Is.EqualTo("Aaaa www.aaa.com. TTL=2400 fe80::3882:6dac:af18:cba6"));
            Assert.That(Print(p, RrKind.AR, 2), Is.EqualTo("A ns.aaa.com. TTL=2400 192.168.0.1"));
        }

        [Test]
        public void 自ドメインの検索_タイプCNAME_www_aaa_com_逆検索(){
            //exercise
            var p = lookup(DnsType.Cname, "www.aaa.com");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=0 NS=1 AR=1"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Cname www.aaa.com."));
            Assert.That(Print(p, RrKind.NS, 0), Is.EqualTo("Ns aaa.com. TTL=2400 ns.aaa.com."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A ns.aaa.com. TTL=2400 192.168.0.1"));
        }

        [Test]
        public void 自ドメインの検索_タイプPTR_192_168_0_1(){
            //exercise
            var p = lookup(DnsType.Ptr, "1.0.168.192.in-addr.arpa");

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=2 NS=0 AR=0"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Ptr 1.0.168.192.in-addr.arpa."));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Ptr 1.0.168.192.in-addr.arpa. TTL=2400 ns.aaa.com."));
            Assert.That(Print(p, RrKind.AN, 1), Is.EqualTo("Ptr 1.0.168.192.in-addr.arpa. TTL=2400 ws0.aaa.com."));
        }

        [Test]
        public void 自ドメインの検索_タイプPTR_192_168_0_222_存在しない(){
            //exercise
            var p = lookup(DnsType.Ptr, "222.0.168.192.in-addr.arpa");

            //verify
            //Assert.Is.assertNull(p); //レスポンスが無いことを確認する
            Assert.IsNull(p); //レスポンスが無いことを確認する

        }

        [Test]
        public void 他ドメインの検索_タイプA() {
            //exercise
            var p = lookup(DnsType.A, "www.sapporoworks.ne.jp",true);

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=3 AR=3"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query A www.sapporoworks.ne.jp."));
            
            var ar = new List<string>();
            for (int i = 0; i < 1;i++ )
                ar.Add(Print(p, RrKind.AN, i));
            ar.Sort();
            Assert.That(ar[0], Is.EqualTo("A www.sapporoworks.ne.jp. TTL=86400 59.106.27.208"));
 

            ar.Clear();
            for (int i = 0; i < 3; i++)
                ar.Add(Print(p, RrKind.NS, i));
            ar.Sort();
            Assert.That(ar[0], Is.EqualTo("Ns sapporoworks.ne.jp. TTL=3600 ns1.dns.ne.jp."));
            Assert.That(ar[1], Is.EqualTo("Ns sapporoworks.ne.jp. TTL=3600 ns2.dns.ne.jp."));
            Assert.That(ar[2], Is.EqualTo("Ns sapporoworks.ne.jp. TTL=86400 www.sapporoworks.ne.jp."));

            ar.Clear();
            for (int i = 0; i < 3; i++)
                ar.Add(Print(p, RrKind.AR, i));
            ar.Sort();
            Assert.That(ar[0], Is.EqualTo("A ns1.dns.ne.jp. TTL=86400 210.188.224.9"));
            Assert.That(ar[1], Is.EqualTo("A ns2.dns.ne.jp. TTL=86400 210.224.172.13"));
            Assert.That(ar[2], Is.EqualTo("A www.sapporoworks.ne.jp. TTL=86400 59.106.27.208"));


        }

        [Test]
        public void 他ドメインの検索_タイプMX() {
            //exercise
            var p = lookup(DnsType.Mx, "sapporoworks.ne.jp", true);

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=0 AR=1"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query Mx sapporoworks.ne.jp."));

            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Mx sapporoworks.ne.jp. TTL=3600 10 sapporoworks.ne.jp."));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A sapporoworks.ne.jp. TTL=3600 59.106.27.208"));


        }

        [Test]
        public void 他ドメインの検索_タイプCNAME() {
            //exercise
            var p = lookup(DnsType.A, "www.yahoo.com", true);

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=5 NS=5 AR=5"));
            Assert.That(Print(p, RrKind.QD, 0), Is.EqualTo("Query A www.yahoo.com."));

            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Cname www.yahoo.com. TTL=300 fd-fp3.wg1.b.yahoo.com."));
            Assert.That(Print(p, RrKind.AN, 1), Is.EqualTo("Cname fd-fp3.wg1.b.yahoo.com. TTL=300 ds-fp3.wg1.b.yahoo.com."));
            Assert.That(Print(p, RrKind.AN, 2), Is.EqualTo("Cname ds-fp3.wg1.b.yahoo.com. TTL=60 ds-kr-fp3-lfb.wg1.b.yahoo.com."));
            Assert.That(Print(p, RrKind.AN, 3), Is.EqualTo("Cname ds-kr-fp3-lfb.wg1.b.yahoo.com. TTL=300 ds-kr-fp3.wg1.b.yahoo.com."));
            Assert.That(Print(p, RrKind.AN, 4), Is.EqualTo("A ds-kr-fp3.wg1.b.yahoo.com. TTL=60 106.10.139.246"));
            Assert.That(Print(p, RrKind.AR, 0), Is.EqualTo("A ns1.yahoo.com. TTL=172800 68.180.131.16"));
            Assert.That(Print(p, RrKind.AR, 1), Is.EqualTo("A ns5.yahoo.com. TTL=172800 119.160.247.124"));
            Assert.That(Print(p, RrKind.AR, 2), Is.EqualTo("A ns2.yahoo.com. TTL=172800 68.142.255.16"));
            Assert.That(Print(p, RrKind.AR, 3), Is.EqualTo("A ns3.yahoo.com. TTL=172800 203.84.221.53"));
            Assert.That(Print(p, RrKind.AR, 4), Is.EqualTo("A ns4.yahoo.com. TTL=172800 98.138.11.157"));


        }

        [Test]
        public void 他ドメインの検索_yahooo_co_jp() {
            //exercise
            var p = lookup(DnsType.A, "www.yahoo.co.jp", true);

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=2 NS=4 AR=4"));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("Cname www.yahoo.co.jp. TTL=900 www.g.yahoo.co.jp."));
            //AN.1のAレコードは、ダイナミックにIPが変わるので、Testの対象外とする
            //Assert.That(Print(p, RrKind.AN, 1), Is.EqualTo("A www.g.yahoo.co.jp. TTL=60 203.216.235.189"));
        }

        [Test]
        public void 他ドメインの検索_www_asahi_co_jp() {
            //exercise
            var p = lookup(DnsType.A, "www.asahi.co.jp", true);

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=1 NS=2 AR=2"));
            Assert.That(Print(p, RrKind.AN, 0), Is.EqualTo("A www.asahi.co.jp. TTL=16400 202.242.245.10"));
        }

        [Test]
        public void 他ドメインの検索_www_ip_com() {
            //exercise
            var p = lookup(DnsType.A, "www.ip.com", true);

            //verify
            Assert.That(Print(p), Is.EqualTo("QD=1 AN=4 NS=5 AR=7"));
            var ar = new List<String>();
            ar.Add("A www.ip.com. TTL=1800 96.45.82.133");
            ar.Add("A www.ip.com. TTL=1800 96.45.82.69");
            ar.Add("A www.ip.com. TTL=1800 96.45.82.5");
            ar.Add("A www.ip.com. TTL=1800 96.45.82.197");

            for (int i=0;i<ar.Count;i++){
                var str = Print(p, RrKind.AN, i);
                if (ar.IndexOf(str) < 0){
                    Assert.Fail(str);                    
                }
            }
        }
    }
}