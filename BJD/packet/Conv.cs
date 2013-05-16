using System;
using Bjd.util;

namespace Bjd.packet{
    public class Conv{

        //デフォルトコンストラクタの隠蔽
        private Conv(){

        }

        public static byte[] GetBytes(short val){
            //[C#]
            byte[] buf = { (byte)(val >> 8), (byte)val };
            //byte[] buf = { (byte)(val >> 0), (byte)(val>>8) };
            return buf;
        }

        public static byte[] GetBytes(ushort val) {
            //[C#]
            byte[] buf = { (byte)(val >> 8), (byte)val };
            //byte[] buf = { (byte)(val >> 0), (byte)(val >> 8) };
            return buf;
        }

        public static byte[] GetBytes(int val){
            byte[] buf = {(byte) (val >> 24), (byte) (val >> 16), (byte) (val >> 8), (byte) val};
            //byte[] buf = { (byte)(val >> 0), (byte)(val >> 8), (byte)(val >> 16), (byte)(val >>24)};
            return buf;
        }

        public static byte[] GetBytes(uint val) {
            //[C#]
            byte[] buf = {(byte) (val >> 24), (byte) (val >> 16), (byte) (val >> 8), (byte) val};
            //byte[] buf = { (byte)(val >> 0), (byte)(val >> 8), (byte)(val >> 16), (byte)(val >> 24) };
            return buf;
        }

        public static byte[] GetBytes(long val){
            //[C#]
            byte[] buf ={
                (byte) (val >> 56), (byte) (val >> 48), (byte) (val >> 40), (byte) (val >> 32),
                (byte) (val >> 24), (byte) (val >> 16), (byte) (val >> 8), (byte) val
            };
            //byte[] buf ={
            //    (byte) (val >> 0), (byte) (val >> 8), (byte) (val >> 16), (byte) (val >> 24),
            //    (byte) (val >> 32), (byte) (val >> 40), (byte) (val >> 48), (byte) (val >> 56)
            //};
            return buf;
        }

        public static byte[] GetBytes(ulong val) {
            //[C#]
            byte[] buf ={
               (byte) (val >> 56), (byte) (val >> 48), (byte) (val >> 40), (byte) (val >> 32),
               (byte) (val >> 24), (byte) (val >> 16), (byte) (val >> 8), (byte) val
            };
            //byte[] buf ={
            //    (byte) (val >> 0), (byte) (val >> 8), (byte) (val >> 16), (byte) (val >> 24),
            //    (byte) (val >> 32), (byte) (val >> 40), (byte) (val >> 48), (byte) (val >> 56)
            //};
            return buf;
        }

        public static ushort GetUShort(byte[] buf){
            return GetUShort(buf, 0);
        }

        public static ushort GetUShort(byte[] buf, int offset){
            ushort n = BitConverter.ToUInt16(buf, offset);
            return Util.htons(n);
        }
        
        //        public static int GetInt(byte[] buf){
//            return GetInt(buf, 0);
//        }
//
//        public static int GetInt(byte[] buf, int offset){
//            uint n = (uint) BitConverter.ToInt32(buf, offset);
//            return (int)Util.htonl(n);
//        }
        public static uint GetUInt(byte[] buf){
            return GetUInt(buf, 0);
        }

        public static uint GetUInt(byte[] buf, int offset){
            uint n = BitConverter.ToUInt32(buf, offset);
            return Util.htonl(n);
            //return BitConverter.ToUInt32(buf, offset);
        }

//        public static long GetLong(byte[] buf){
//            return GetLong(buf, 0);
//        }
//
//        public static long GetLong(byte[] buf, int offset){
//            return BitConverter.ToInt64(buf, offset);
//        }

        public static ulong GetULong(byte[] buf){
            return GetULong(buf, 0);
        }

        public static ulong GetULong(byte[] buf, int offset) {
            UInt64 n = BitConverter.ToUInt64(buf, offset);
            return Util.htonl(n);

            //return BitConverter.ToUInt64(buf, offset);
        }

    }
}