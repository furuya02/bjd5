using System;
using Bjd.net;

namespace Bjd.acl {
    public class AclV4 : Acl {
        //指定の要領
        //192.168.0.1
        //192.168.0.1-200
        //192.168.0.1-192.168.10.254
        //192.168.10.254-192.168.0.1（開始と終了が逆転してもＯＫ）
        //192.168.0.1/24
        //192.168.*.* 
        //*.*.*,*
        //*

        //****************************************************************
        //オーバーライド
        //****************************************************************
        override public bool IsHit(Ip ip) {
            if (ip.AddrV4 < Start.AddrV4)
                return false;
            if (End.AddrV4 < ip.AddrV4)
                return false;
            return true;
        }

        //コンストラクタ
        public AclV4(string name, string ipStr)
            : base(name) {

            //「*」によるALL指定
            if (ipStr == "*" || ipStr == "*.*.*.*") {
                Start = new Ip(IpKind.V4_0);
                End = new Ip(IpKind.V4_255);
                Status = true;
                return;//初期化成功
            }

            //「*」表現を正規化する
            string[] tmp = ipStr.Split('.');
            if (tmp.Length == 4) {
                if (tmp[1] == "*" && tmp[2] == "*" && tmp[3] == "*") { //192.*.*.*
                    ipStr = string.Format("{0}.0.0.0/8", tmp[0]);
                } else if (tmp[2] == "*" && tmp[3] == "*") {//192.168.*.*
                    ipStr = string.Format("{0}.{1}.0.0/16", tmp[0], tmp[1]);
                } else if (tmp[3] == "*") {//192.168.0.*
                    ipStr = string.Format("{0}.{1}.{2}.0/24", tmp[0], tmp[1], tmp[2]);
                }
            }

            if (ipStr.IndexOf('-') != -1) {
                //************************************************************
                // 「-」による範囲指定
                //************************************************************
                tmp = ipStr.Split('-');
                if (tmp.Length != 2)
                    ThrowException(ipStr); //初期化失敗
                try{
                    Start = new Ip(tmp[0]);
                }catch(ValidObjException){
                    ThrowException(ipStr); //初期化失敗
                }
                var strTo = tmp[1];
                //to（終了アドレス）が192.168.2.254のように４オクテットで表現されているかどうかの確認
                tmp = strTo.Split('.');
                if (tmp.Length == 4) {//192.168.0.100
                    try{
                        End = new Ip(strTo);
                    }catch(ValidObjException){
                        ThrowException(ipStr); //初期化失敗
                    }
                } else if (tmp.Length == 1) {//100
                    try {
                        var n = Convert.ToUInt32(strTo);
                        //if(n < 0 || 255 < n)
                        //    return;//初期化失敗
                        strTo = string.Format("{0}.{1}.{2}.{3}", Start.IpV4[0], Start.IpV4[1], Start.IpV4[2], n);
                        End = new Ip(strTo);
                    } catch {
                        ThrowException(ipStr); //初期化失敗
                    }
                } else {
                    ThrowException(ipStr); //初期化失敗
                }

                //開始アドレスが終了アドレスより大きい場合、入れ替える
                if (Start.AddrV4 > End.AddrV4) {
                    Swap(); // startとendの入れ替え
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

                uint mask = 0;
                uint xor=0;
                try {
                    int m = Convert.ToInt32(strMask);
                    if (m < 0 || 32 < m) {
                        //マスクは32ビットが最大
                        ThrowException(ipStr); //初期化失敗
                    }
                    for (int i = 0; i < 32; i++) {
                        if (i != 0)
                            mask = mask << 1;
                        if (i < m)
                            mask = (mask | 1);
                    }
                    xor = (0xffffffff ^ mask);
                } catch {
                    ThrowException(ipStr); //初期化失敗
                }
                try{
                    var ip = new Ip(strIp);
                    Start = new Ip(ip.AddrV4 & mask);
                    End = new Ip(ip.AddrV4 | xor);
                } catch (ValidObjException) {
                    ThrowException(ipStr); //初期化失敗
                }
            } else {
                //************************************************************
                // 通常指定
                //************************************************************
                try{
                    Start = new Ip(ipStr);
                    End = new Ip(ipStr);
                } catch (ValidObjException) {
                    ThrowException(ipStr); //初期化失敗
                }
            }
            if (Start.InetKind != InetKind.V4) {
                ThrowException(ipStr); //初期化失敗
            }
            if (End.InetKind != InetKind.V4) {
                ThrowException(ipStr); //初期化失敗
            }


            //最終チェック
            if (Start.AddrV4 != 0 || End.AddrV4 != 0) {
                if (Start.AddrV4 <= End.AddrV4)
                    Status = true;//初期化成功
            }
        }

        protected override void Init(){
            Start = new Ip(IpKind.V4_0);
            End = new Ip(IpKind.V4_255);
        }
    }
}
