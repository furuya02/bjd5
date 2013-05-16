using System;
using System.IO;
using Bjd.util;

namespace Bjd.packet{
    public abstract class Packet{

        //保持するパケット
        //private ByteBuffer packets;
        readonly byte [] _packets;
        //保持しているパケットサイズ
        //プロトコル上の有効無効には関係ない
        readonly int _size;

        //最大サイズが不明な場合のコンストラクタ
        protected Packet(byte[] data, int offset){
            _size = data.Length - offset;
            _packets = new byte[_size];
            Buffer.BlockCopy(data,offset,_packets,0,_size);
        }

        //最大サイズが判明している場合のコンストラクタ
        protected Packet(byte[] data, int offset, int length){
            _size = length;
            _packets = new byte[_size];
            Buffer.BlockCopy(data, offset, _packets, 0, _size);
        }

        //指定したオフセットへのbyte値の設定
        public void SetByte(byte val, int offset){
           ComformeSize(offset, 1);
            _packets[offset] = val;
        }

        //指定したオフセットからのbyte値の取得
        public byte GetByte(int offset, int max){
            ComformeSize(offset,1);
            return _packets[offset];
        }

        void Set(byte [] b,int len,int offset){
            ComformeSize(offset, len);
            Buffer.BlockCopy(b, 0, _packets, offset, len);
        }

        //指定したオフセットへのShot値の設定
        public void SetUShort(ushort val, int offset){
            //ushort n = Util.htons(val);
            //Set(Conv.GetBytes(n),2,offset);
            Set(Conv.GetBytes(val), 2, offset);
        }

        //指定したオフセットからのShot値の取得
//        public short GetShort(int offset){
//            ComformeSize(offset, 2);
//            return Conv.GetShort(_packets, offset);
//        }
        //[C#]
        public ushort GetUShort(int offset) {
            ComformeSize(offset, 2);
            return Conv.GetUShort(_packets, offset);
        }

        //指定したオフセットへのint値の設定
        public void SetUInt(uint val, int offset){
            //uint n = Util.htonl(val);
            //Set(Conv.GetBytes(n), 4, offset);
            Set(Conv.GetBytes(val), 4, offset);
        }

        //指定したオフセットからのint値の取得
//        public int GetInt(int offset){
//            ComformeSize(offset, 4);
//            return Conv.GetInt(_packets, offset);
//        }
        //[C#]
        public uint GetUInt(int offset) {
            ComformeSize(offset, 4);
            return Conv.GetUInt(_packets, offset);
        }

        public void SetULong(ulong val, int offset){
            //ulong n = Util.htonl(val);
            //Set(Conv.GetBytes((long)n), 8, offset);
            Set(Conv.GetBytes(val), 8, offset);
        }

        public ulong GetULong(int offset){
            ComformeSize(offset, 8);
            return Conv.GetULong(_packets, offset);
        }

        public byte[] GetBytes(int offset, int len){
            ComformeSize(offset, len);
            var dst = new byte[len];
            Buffer.BlockCopy(_packets,offset,dst,0,len);
            return dst;
        }

        public void SetBytes(byte[] val, int offset){
            var len = val.Length;
            ComformeSize(offset, len);
            Buffer.BlockCopy(val,0,_packets,offset,len);
        }

        public abstract byte[] GetBytes();

        //パケットサイズのオーバーラン確認
        //offset 開始位置 
        //len 取得サイズ
        //IOException オーバしている場合、この例外が発生する
        private void ComformeSize(int offset, int len){
            if (offset + len > _size){
                throw new IOException();
            }
        }

        public abstract int Length();
    }
}