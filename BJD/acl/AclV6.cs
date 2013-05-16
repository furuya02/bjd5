using System;
using Bjd.net;

namespace Bjd.acl {
    public class AclV6 : Acl {

        //****************************************************************
        //オーバーライド
        //****************************************************************
        override public bool IsHit(Ip ip) {
            if (ip.AddrV6H < Start.AddrV6H) {
                return false;
            }
            if (ip.AddrV6H == Start.AddrV6H) {
                if (ip.AddrV6L < Start.AddrV6L)
                    return false;
            }

            if (End.AddrV6H < ip.AddrV6H) {
                return false;
            }
            if (End.AddrV6H == ip.AddrV6H) {
                if (End.AddrV6L < ip.AddrV6L)
                    return false;
            }
            return true;
        }

        //コンストラクタ
        public AclV6(string name, string ipStr)
            : base(name) {

            //「*」によるALL指定
            if (ipStr == "*" || ipStr == "*:*:*:*:*:*:*:*") {
                Start = new Ip(IpKind.V6_0);
                End = new Ip(IpKind.V6_FF);
                Status = true;
                return;//初期化成功
            }

            string[] tmp;
            if (ipStr.IndexOf('-') != -1) {
                //************************************************************
                // 「-」による範囲指定
                //************************************************************
                tmp = ipStr.Split('-');
                if (tmp.Length != 2)
                    ThrowException(ipStr); //初期化失敗
                try{
                    Start = new Ip(tmp[0]);
                    End = new Ip(tmp[1]);
                }catch(ValidObjException){
                    ThrowException(ipStr); //初期化失敗
                }

                //開始アドレスが終了アドレスより大きい場合、入れ替える
                if (Start.AddrV6H == End.AddrV6H) {
                    if (Start.AddrV6L > End.AddrV6L) {
                        Swap(); // startとendの入れ替え
                    }
                } else {
                    if (Start.AddrV6H > End.AddrV6H) {
                        Swap(); // startとendの入れ替え
                    }
                }

            } else if (ipStr.IndexOf('/') != -1) {
                //************************************************************
                // 「/」によるマスク指定
                //************************************************************
                tmp = ipStr.Split('/');
                if (tmp.Length != 2)
                    ThrowException(ipStr); //初期化失敗
                
                var strIp = tmp[0];
                var strMask = tmp[1];

                UInt64 maskH = 0;
                UInt64 maskL = 0;
                UInt64 xorH = 0;
                UInt64 xorL = 0;
                try {
                    UInt64 m = Convert.ToUInt64(strMask);
                    if ( 128 < m) {
                        //マスクは128ビットが最大
                        ThrowException(ipStr); //初期化失敗
                    }
                    for (UInt64 i = 0; i < 64; i++) {
                        if (i != 0)
                            maskH = maskH << 1;
                        if (i < m)
                            maskH = (maskH | 1);
                    }
                    xorH = (0xffffffffffffffff ^ maskH);

                    for (UInt64 i = 64; i < 128; i++) {
                        if (i != 0)
                            maskL = maskL << 1;
                        if (i < m)
                            maskL = (maskL | 1);
                    }
                    xorL = (0xffffffffffffffff ^ maskL);
                } catch {
                    ThrowException(ipStr); //初期化失敗
                }
                try{
                    var ip = new Ip(strIp);
                    Start = new Ip(ip.AddrV6H & maskH, ip.AddrV6L & maskL);
                    End = new Ip(ip.AddrV6H | xorH, ip.AddrV6L | xorL);
                } catch (ValidObjException) {
                    ThrowException(ipStr); //初期化失敗
                }
            } else {
                //************************************************************
                // 通常指定
                //************************************************************
                try {
                    Start = new Ip(ipStr);
                    End = new Ip(ipStr);
                } catch (ValidObjException) {
                    ThrowException(ipStr); //初期化失敗
                }
            }

            if (Start.InetKind != InetKind.V6) {
                ThrowException(ipStr); //初期化失敗
            }
            if (End.InetKind != InetKind.V6) {
                ThrowException(ipStr); //初期化失敗
            }
            Status = true;//初期化成功
        }

        protected override void Init(){
            Start = new Ip(IpKind.V6_0);
            End = new Ip(IpKind.V6_FF);
        }
    }
}
