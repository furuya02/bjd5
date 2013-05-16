using System;
using Bjd.packet;
using Bjd.util;

namespace DnsServer{
    // DNS形式名前(byte[])を圧縮する
    // byte [] bufferは、現在、パケットの先頭からのバイト列
    // 結果は、GetData()で取得する
    public class Compress{
        private readonly byte[] _data = new byte[0];

        public Compress(byte[] buffer, byte[] dataName){

            //多い目にバッファを確保する
            var buf = new byte[dataName.Length];

            //コピー
            var dst = 0;
            for (var src = 0; src < dataName.Length;){
                var index = -1;
                if (dataName[src] != 0){
                    // 圧縮可能な同一パターンを検索する
                    var len = dataName.Length - src; //残りの文字列数
                    var target = new byte[len];
                    Buffer.BlockCopy(dataName, src, target, 0, len);

                    //パケットのヘッダ以降が検索対象になる（bufferは、ヘッダの後ろに位置しているので先頭は0となる）
                    const int off = 12; // 検索開始位置(ヘッダ以降)
                    index = Bytes.IndexOf(buffer, off, target);
                }
                if (0 <= index){
                    // 圧縮可能な場合
                    uint c = Util.htons((ushort)(0xC000 | (index)));//本当の位置はヘッダ分を追加したindex+12となる
                    var cc = BitConverter.GetBytes(c);
                    Buffer.BlockCopy(cc, 0, buf, dst, 2);
                    dst += 2;
                    break;
                }
                // 圧縮不可能な場合は、.までをコピーする
                var n = dataName[src] + 1;
                for (var i = 0; i < n; i++){
                    buf[dst + i] = dataName[src + i];
                }
                dst += n;
                src += n;
            }
            //有効文字数分のみコピーする
            _data = new byte[dst];
            Buffer.BlockCopy(buf, 0, _data, 0, dst);
        }

        public byte[] GetData(){
            return _data;
        }
    }
}