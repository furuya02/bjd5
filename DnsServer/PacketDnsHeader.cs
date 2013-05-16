using System.IO;
using Bjd.packet;
using Bjd.util;

namespace DnsServer{

    public class PacketDnsHeader : Packet{

        public PacketDnsHeader() : base(new byte[12], 0, 12){

        }

        public PacketDnsHeader(byte[] data, int offset)
            : base(data, offset, 12) {
            if (data.Length - offset < 12) {
                throw new IOException("A lack of data");
            }

        }

        public override int Length() {
            return 12;
        }


        private const int PId = 0;
        private const int PFlags = 2;
        private const int PQd = 4;
        private const int PAn = 6;
        private const int PNs = 8;
        private const int PAr = 10;


        //バイトイメージの取得
    	public override byte[] GetBytes(){
            try{
                return GetBytes(0, 12);
            } catch (IOException e){
                //設計上の問題
                Util.RuntimeException(e.Message);
            }
            return null; //これが返されることは無い
        }

	    // 識別子
        public ushort Id{
            get{
                return GetUShort(PId);
            }
            set{
                SetUShort(value, PId);
            }

        }

        //フラグ
        public ushort Flags{
            get{
                return GetUShort(PFlags);
            }
            set{
                SetUShort(value, PFlags);
            }
        }

        //RR数
        public ushort GetCount(int rr){
            return GetUShort(GetRrPos(rr));
        }
        public void SetCount(int rr,ushort count){
            SetUShort(count, GetRrPos(rr));
        }

	 
        private int GetRrPos(int rr){
            if (rr == 0){
                return PQd;
            }
            if (rr == 1){
                return PAn;
            }
            if (rr == 2){
                return PNs;
            }
            if (rr == 3){
                return PAr;
            }
            //設計上の問題
            Util.RuntimeException(string.Format("DnsHeader.getCountTag({0})", rr));
            return 0;
        }

    }
}