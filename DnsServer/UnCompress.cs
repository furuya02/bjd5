using System;
using Bjd.packet;
using Bjd.util;

namespace DnsServer{

    // 開始位置 buffer[offSet]から、ホスト名(string)を取り出す
    // 結果は、getHostName()及びgetOffSet()で取得する
    public class UnCompress{
        private readonly string _hostname = "";
        private readonly int _offSet;

        public UnCompress(byte[] buffer, int offset){
            _offSet = offset;
            var compressOffSet = 0; // 圧縮ラベルを使用した場合のリターン用ポインタ
            var compress = false; // 圧縮ラベルを使用したかどうかのフラグ
            var tmp = new char[buffer.Length];
            var d = 0;
            while (true){
                var c = buffer[_offSet];
                _offSet++;
                if (c == 0){
                    //最後の.は残す
                    if (d == 0){
                        tmp[d] = '.';
                        d++;
                    }
                    _hostname = new string(tmp, 0, d);
                    if (compress){
                        // 圧縮ラベルを使用した場合は、２バイトだけ進めたポインタを返す
                        _offSet = compressOffSet;
                    }
                    return;
                }
                if ((c & 0xC0) == 0xC0){
                    // 圧縮ラベル
                    ushort off1 = Util.htons(BitConverter.ToUInt16(buffer, _offSet - 1));
                    // 圧縮ラベルを使用した直後は、s+2を返す
                    // 圧縮ラベルの再帰の場合は、ポインタを保存しない
                    if (!compress){
                        compressOffSet = _offSet + 1;
                        compress = true;
                    }
                    var off = (short) (off1 & 0x3FFF);
                    _offSet = off;
                } else{
                    if (c >= 255){
                        _hostname = "";
                        return;
                    }
                    for (var i = 0; i < c; i++){
                        tmp[d++] = (char) buffer[_offSet++];
                    }
                    tmp[d++] = '.';
                }
            }
        }

        public string HostName{
            get{
                return _hostname;
            }
        }

        public int OffSet{
            get{
                return _offSet;
            }
        }
    }
}