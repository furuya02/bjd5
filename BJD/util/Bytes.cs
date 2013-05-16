using System;
using System.Linq;
using System.Text;

namespace Bjd.util {
    //byte[]配列の操作クラス
    static public class Bytes {

        //*********************************************************
        // byte[] の生成
        //*********************************************************
        //複数のオブジェクトを並べて、byte[]に変換する
        //null指定可能 / stringは、Encoding.ASCCでバイト化される
        public static byte[] Create(params object[] list) {
            var len = 0;
            foreach (var o in list) {
                if (o == null)
                    continue;
                switch (o.GetType().Name) {
                    case "Byte[]":
                        len += ((byte[])o).Length;
                        break;
                    case "String":
                    case "string":
                        len += ((string)o).Length;
                        break;
                    case "Int32":
                    case "UInt32":
                        len += 4;
                        break;
                    case "Int16":
                    case "UInt16":
                        len += 2;
                        break;
                    case "Int64":
                    case "UInt64":
                        len += 8;
                        break;
                    case "Byte":
                        len += 1;
                        break;
                    default:
                        Msg.Show(MsgKind.Error, "ERROR Bytes.Create() " + o.GetType().Name);
                        return new byte[0];
                }
            }

            var data = new byte[len];
            var offset = 0;

            foreach (var o in list) {

                if (o == null)
                    continue;
                
                switch (o.GetType().Name) {
                    case "Byte[]":
                        Buffer.BlockCopy(((byte[])o), 0, data, offset, ((byte[])o).Length);
                        offset += ((byte[])o).Length;
                        break;
                    case "String":
                    case "string":
                        Buffer.BlockCopy(Encoding.ASCII.GetBytes((string)o), 0, data, offset, ((string)o).Length);
                        offset += ((string)o).Length;
                        break;
                    case "Int32":
                        Buffer.BlockCopy(BitConverter.GetBytes((Int32)o), 0, data, offset, 4);
                        offset += 4;
                        break;
                    case "UInt32":
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt32)o), 0, data, offset, 4);
                        offset += 4;
                        break;
                    case "Int16":
                        Buffer.BlockCopy(BitConverter.GetBytes((Int16)o), 0, data, offset, 2);
                        offset += 2;
                        break;
                    case "UInt16":
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt16)o), 0, data, offset, 2);
                        offset += 2;
                        break;
                    case "Int64":
                        Buffer.BlockCopy(BitConverter.GetBytes((Int64)o), 0, data, offset, 8);
                        offset += 8;
                        break;
                    case "UInt64":
                        Buffer.BlockCopy(BitConverter.GetBytes((UInt64)o), 0, data, offset, 8);
                        offset += 8;
                        break;
                    case "Byte":
                        data[offset] = (byte)o;
                        offset += 1;
                        break;
                }
            }
            return data;
        }

        //*********************************************************
        // 検索
        //*********************************************************
        //bufferの中でtargetが始まる位置を検索する
        //int off 検索開始位置
        public static int IndexOf(byte[] buffer,int off,byte[] target) {
            for (var i = off;i + target.Length < buffer.Length;i++) {
                var match = !target.Where((t1, t) => buffer[i + t] != t1).Any();
                if (match)
                    return i;
            }
            return -1;
        }
    }
}