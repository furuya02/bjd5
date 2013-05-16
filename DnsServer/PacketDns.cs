using System;
using System.Collections.Generic;
using System.IO;
using Bjd.packet;
using Bjd.util;

namespace DnsServer{


    // パケットを処理するクラス
    public class PacketDns{
        //	private Logger logger;

        // [受信に使用する場合]
        // PacketDns p = new PacketDns(Kernel kernel,string nameTag);
        // p.Read(byte[] buffer)
        // p.RequestName;
        // p.DnsType
        // p.Id （識別子）
        // p.Rd  (再帰問い合わせ)
        // p.GetRR(RR_KIND rrKind)

        // [送信に使用する場合]
        // PacketDns p = new PacketDns(Kernel kernel,string nameTag);
        // p.CreateHeader(uint id,bool qr,bool aa,bool rd,bool ra);//ヘッダの作成
        // p.AddRR(RR_KIND rrKind,OneRR oneRR);//フィールドの追加
        // byte [] p.Get();//内部バッファの取得（HeaderDnsとar(List<OneRR>)を結合(名前の圧縮及びHeaderのCountの修正)して返す）

        // 内部バッファ  DNSパケットの内容は、以下の２つのバッファで保持される
        private readonly PacketDnsHeader _dnsHeader;
        private readonly List<OneRr>[] _ar = new List<OneRr>[4];

        private void Init(){
            for (var i = 0; i < 4; i++){
                _ar[i] = new List<OneRr>();
            }
        }

        //パケット生成のためのコンストラクタ
        public PacketDns(ushort id, bool qr, bool aa, bool rd, bool ra){
            Init();

            //ヘッダの生成
            _dnsHeader = new PacketDnsHeader();
            try{
                _dnsHeader.Id = id;
            } catch (IOException){
                //DnsHeaderは12バイトのサイズで初期化されているはずなので、ここで例外が発生するのは設計上の問題
                Util.RuntimeException("PacketDns.createHeader() dnsHeader.serId()");
            }

            // 各種フラグ
            ushort flags = 0;
            if (qr){
                //要求(false)・応答(true)
                flags = (ushort) (flags | 0x8000);
            }
            if (aa){
                //権威応答 権威有り(true)
                flags = (ushort) (flags | 0x0400);
            }
            if (rd){
                //再帰要求 有り(true)
                flags = (ushort) (flags | 0x0100);
            }
            if (ra){
                //再帰有効 有効(true)
                flags = (ushort) (flags | 0x0080);
            }
            try{
                _dnsHeader.Flags = flags;
            } catch (IOException){
                //DnsHeaderは12バイトのサイズで初期化されているはずなので、ここで例外が発生するのは設計上の問題
                Util.RuntimeException("PacketDns.createHeader() dnsHeader.setFlags(flags)");
            }

            //byte rcode=0 戻りコード
            //ushort tmp = (ushort)(0x0F & rcode);
            //flags = (ushort)(flags | tmp);
        }

        //パケット解釈のためのコンストラクタ
        public PacketDns(byte[] buffer){
            Init();

            //ヘッダの解釈
            _dnsHeader = new PacketDnsHeader(buffer, 0);
            var offset = _dnsHeader.Length();

            // オペコード　0:標準 1:逆 2:サーバ状態
            //var c = (short)(Util.htons(_headerDns.Flags) & 0x7800);
            //var opcode = (ushort)(c >> 11);
            var flags = _dnsHeader.Flags;
            var opcode = flags & 0x7800;

            if (opcode != 0){
                // 標準問い合せ以外は対応していない
                throw new IOException(string.Format("OPCODE not 0 [CPCODE={0}]", opcode));
            }

            //****************************************
            //質問/回答/権限/追加フィールド取得
            //****************************************
            for (var rr = 0; rr < 4; rr++){
                //ushort max = Util.htons(_headerDns.Count[rr]);//対象フィールドのデータ数
                var max = _dnsHeader.GetCount(rr);
                if (rr == 0 && max != 1){
                    //質問エントリーが１でないパケットは処理できません。
                    throw new IOException(string.Format("QD Entry !=0  [count={0}]", max));
                }
                for (var n = 0; n < max; n++){
                    //名前の取得
                    //offsetの移動  名前のサイズが一定ではないので、そのサイズ分だけ進める
                    var u0 = new UnCompress(buffer, offset);
                    offset = u0.OffSet;
                    var name = u0.HostName;

                    //名前以降のリソースレコードを取得
                    var packetRr = new PacketRr(buffer, offset);

                    var dnsType = packetRr.DnsType;
                    if (rr == 0){
                        //質問フィールド[QD]の場合は、TTL, DLEN , DATAは無い
                        _ar[rr].Add(new RrQuery(name, dnsType));
                        //offsetの移動  名前以降の分だけ進める
                        offset += 4;
                        continue;
                    }
                    var ttl = packetRr.Ttl;
                    var dlen = packetRr.DLen;
                    var data = packetRr.Data;

                    //TypeによってはNameが含まれている場合があるが、Nameは圧縮されている可能性があるので、
                    //いったん、string 戻してから、改めてリソース用に組み直したDataを作成する
                    OneRr oneRr = null;
                    if (dnsType == DnsType.A){
                        oneRr = new RrA(name, ttl, data);
                    } else if (dnsType == DnsType.Aaaa){
                        oneRr = new RrAaaa(name, ttl, data);
                    } else if (dnsType == DnsType.Cname || dnsType == DnsType.Ptr || dnsType == DnsType.Ns){
                        var u1 = new UnCompress(buffer, offset + 10);
                        switch (dnsType){
                            case DnsType.Cname:
                                oneRr = new RrCname(name, ttl, u1.HostName);
                                break;
                            case DnsType.Ptr:
                                oneRr = new RrPtr(name, ttl, u1.HostName);
                                break;
                            case DnsType.Ns:
                                oneRr = new RrNs(name, ttl, u1.HostName);
                                break;
                            default:
                                Util.RuntimeException(string.Format("DnsPacket() not implement dnsType={0}", dnsType));
                                break;
                        }
                    } else if (dnsType == DnsType.Mx){
                        var preference = Conv.GetUShort(buffer, offset + 10);
                        var u2 = new UnCompress(buffer, offset + 12);
                        oneRr = new RrMx(name, ttl, preference, u2.HostName);
                    } else if (dnsType == DnsType.Soa){
                        var u3 = new UnCompress(buffer, offset + 10);
                        var u4 = new UnCompress(buffer, u3.OffSet);
                        var p = u4.OffSet;
                        var serial = Conv.GetUInt(buffer, p);
                        p += 4;
                        var refresh = Conv.GetUInt(buffer, p);
                        p += 4;
                        var retry = Conv.GetUInt(buffer, p);
                        p += 4;
                        var expire = Conv.GetUInt(buffer, p);
                        p += 4;
                        var minimum = Conv.GetUInt(buffer, p);
                        oneRr = new RrSoa(name, ttl, u3.HostName, u4.HostName, serial, refresh, retry, expire, minimum);
                    }
                    if (oneRr != null){
                        //A NS MX SOA PTR CNAMEの6種類以外は、処理(追加)しない
                        _ar[rr].Add(oneRr);
                    }
                    offset += 10 + dlen;
                }
                //ヘッダ内のRRレコードのエントリー数を設定する
                _dnsHeader.SetCount(rr, (ushort) _ar[rr].Count);
            }

        }

        public ushort GetCount(RrKind rrKind){
            try{
                return _dnsHeader.GetCount((int) rrKind);
            } catch (IOException e){
                //ここで例外が派生するのは、 設計上の問題
                Util.RuntimeException(e.Message);
                return 0;
            }
        }

        //    public ushort GetRcode() {
        public short GetRcode(){
            return (short) (_dnsHeader.Flags & 0x000F);
            //return (ushort)(Util.htons(_headerDns.Flags) & 0x000F);
        }

        public bool GetAa(){
            if ((_dnsHeader.Flags & 0x0400) != 0){
                return true;
            }
            return false;
        }

        //フィールドの読み込み
        //RR_TYPEフィールドのno番目のデータを取得する
        public OneRr GetRr(RrKind rrKind, int no){
            return _ar[(int) rrKind][no];
        }

        // 質問フィールドのDNSタイプ取得
        public DnsType GetDnsType(){
            //質問フィールドの１つめのリソース
            return _ar[0][0].DnsType;
        }

        //質問フィールドの名前取得
        public string GetRequestName(){
            //質問フィールドの１つめのリソース
            return _ar[0][0].Name;
        }

        //識別子取得
        public ushort GetId(){
            //ネットワークバイトオーダのまま
            try{
                return _dnsHeader.Id;
            } catch (IOException e){
                Util.RuntimeException(e.Message);
            }
            return 0; // これが返されることは無い
        }

        public bool GetRd(){
            //再帰要求(RD)取得
            //var c = (short)(Util.htons(_headerDns.Flags) & 0x0100);
            try{
                var c = (short) (_dnsHeader.Flags & 0x0100);
                return ((c >> 8) != 0);
            } catch (IOException){
                return false; // これが実行されることは無い
            }
        }

        //回答フィールドへの追加
        //これを下記のように変更し、OneRRのコンストラクタを使用するようにする
        public void AddRr(RrKind rrKind, OneRr oneRr){
            //名前の圧縮は、最後のgetBytes()で処理する
            var i = (int) rrKind;
            _ar[i].Add(oneRr);
            try{
                var count = _dnsHeader.GetCount(i);
                _dnsHeader.SetCount(i, ++count);
            } catch (IOException){
                Util.RuntimeException("PacketDns.addRR()");
            }
        }

	    //バイトイメージの取得
        public byte[] GetBytes(){
            var buffer = _dnsHeader.GetBytes();
            for (var i = 0; i < 4; i++){
                var a = _ar[i];
                foreach (var o in a){

                    var dataName = (new Compress(buffer, DnsUtil.Str2DnsName(o.Name))).GetData();
                    var data = o.Data;
                    var dnsType = o.DnsType;

                    if (i != 0){
                        //QDでは、data部分は存在しない
                        if (dnsType == DnsType.Ns || dnsType == DnsType.Cname || dnsType == DnsType.Ptr){
                            data = (new Compress(buffer, o.Data)).GetData(); //圧縮
                        } else if (dnsType == DnsType.Mx){
                            var preference = Conv.GetUShort(o.Data, 0);
                            var mlServer = new byte[o.Data.Length - 2];
                            //System.arraycopy(o.Data, 2, mlServer, 0, o.Data.Length - 2);
                            Buffer.BlockCopy(o.Data, 2, mlServer, 0, o.Data.Length - 2);

                            mlServer = (new Compress(buffer, mlServer)).GetData(); //圧縮
                            data = Bytes.Create(Conv.GetBytes(preference), mlServer);
                        }
                    }
                    //PacketRrは、QD(i==0)の時、data.Length=0となり、内部でisQueryがセットされる
                    var packetRr = new PacketRr(data.Length);
                    try{
                        packetRr.Cls = 1;
                        packetRr.DnsType = dnsType;
                        packetRr.Ttl = o.Ttl; //PacketRr.isQueryがセットされているとき、処理なし
                        packetRr.Data = data; //PacketRr.isQueryがセットされているとき、処理なし

                    } catch (IOException e){
                        //設計上の問題
                        Util.RuntimeException(e.Message);
                    }
                    //PacketRr.isQueryがセットされているとき、getBytes()は4バイト(TTL,DLEN,DATAなし)になっている
                    buffer = Bytes.Create(buffer, dataName, packetRr.GetBytes());
                }
            }
            return buffer;
        }
    }
}