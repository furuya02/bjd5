using System.IO;
using Bjd.packet;
using Bjd.util;

namespace DnsServer{

    public class PacketRr : Packet{
        // Name 不定幅のため、このクラスでは取り扱わない
        // short type
        // short class
        // int ttl
        // byte DataLength
        // byte[] RData

        private const int PType = 0;
        private const int PCls = 2;
        private const int PTtl = 4;
        private const int PDlen = 8;
        private const int PData = 10;
        private readonly bool isQD;

        // デフォルトコンストラクラの隠蔽
        //        private PacketRr(){
        //            //base(new byte[0], 0);
        //        }


        //パケットを生成する場合のコンストラクタ
        public PacketRr(int dlen) : base((dlen == 0) ? (new byte[4]) : (new byte[10 + dlen]), 0){
            //dlenが0の時、QDを表す（TTL,dLen,dataが存在しない）
            if (dlen == 0){
                isQD = true;
            }
        }

        //パケットを解析するためのコンストラクタ
        public PacketRr(byte[] data, int offset) : base(data, offset){
            if (data.Length - offset < 4){
                throw new IOException("A lack of data");
            }
        }

        public DnsType DnsType{
            get{
                var d = GetUShort(PType);
                return DnsUtil.Short2DnsType((short) d);
            }
            set{
                var n = DnsUtil.DnsType2Short(value);
                SetUShort((ushort) n, PType);
            }
        }

        public ushort Cls{
            get{
                return GetUShort(PCls);
            }
            set{
                SetUShort(value, PCls);
            }
        }

        public uint Ttl{
            get{
                return GetUInt(PTtl);
            }
            set{
                if (isQD){
                    //QueryにはData領域は無い
                    return;
                }
                SetUInt(value, PTtl);
            }
        }

        public ushort DLen{
            get{
                if (isQD){
                    return 0;
                }
                return GetUShort(PDlen);
            }
        }

        public byte[] Data{
            get{
                if (isQD){
                    return new byte[0];
                }
                return GetBytes(PData, DLen);
            }
            set{
                if (isQD){
                    //QueryにはData領域は無い
                    return;
                }
                SetUShort((ushort) (value.Length), PDlen);
                SetBytes(value, PData);

            }
        }

        public override int Length(){
            if (isQD){
                return 4; //TTL,dlen及びdataが存在しない
            }
            try{
                int dataLen = DLen;
                return 2 + 2 + 4 + dataLen + 2;
            } catch (IOException e){
                Util.RuntimeException(e.Message);
            }
            return 0;
        }


        public override byte[] GetBytes(){
            try{
                return GetBytes(0, Length());
            } catch (IOException e){
                Util.RuntimeException(e.Message); //設計上の問題
            }
            return null; //これが返されることはない
        }
    }
}