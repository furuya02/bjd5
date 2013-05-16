using System;
using System.Collections.Generic;

namespace Bjd.net {

    public class BindAddr : ValidObj {

        public Ip IpV4 { get; private set; }
        public Ip IpV6 { get; private set; }
        public BindStyle BindStyle { get; private set; }

        //デフォルト値の初期化
        protected override sealed void Init(){
            BindStyle = BindStyle.V4Only;
            IpV4 = new Ip(IpKind.InAddrAny);
            IpV6 = new Ip(IpKind.In6AddrAnyInit);
        }

        //デフォルトコンストラクタ
        public BindAddr(){
            Init(); //デフォルト値での初期化
        }

        //コンストラクタ
        public BindAddr(BindStyle bindStyle, Ip ipV4, Ip ipV6){
            BindStyle = bindStyle;
            IpV4 = ipV4;
            IpV6 = ipV6;
        }

        //コンストラクタ
        public BindAddr(string str){
            if (str == null){
                ThrowException("BindAddr(null)"); //初期化失敗
                return;
            }

            var tmp = str.Split(',');
            if (tmp.Length != 3){
                ThrowException(str); //初期化失敗
            }


            if (tmp[0] == "V4_ONLY" || tmp[0] == "V4Only" || tmp[0] == "V4ONLY"){
                tmp[0] = "V4Only";
            }else if (tmp[0] == "V6_ONLY" || tmp[0] == "V6Only" || tmp[0] == "V6ONLY"){
                tmp[0] = "V6Only";
            }else if (tmp[0] == "V46_DUAL" || tmp[0] == "V46Dual" || tmp[0] == "V46DUAL"){
                tmp[0] = "V46Dual";
            }else{
                ThrowException(str); //初期化失敗
            }

            try{
                BindStyle = (BindStyle) Enum.Parse(typeof (BindStyle), tmp[0]);
                IpV4 = new Ip(tmp[1]);
                //Ver5.7.x以前のコンバート
                if (tmp[2] == "0.0.0.0"){
                    tmp[2] = "::0";
                }
                IpV6 = new Ip(tmp[2]);
            }
            catch (Exception){
                ThrowException(str); //初期化失敗
            }
            if (IpV4.InetKind != InetKind.V4){
                ThrowException(str); //初期化失敗
            }
            if (IpV6.InetKind != InetKind.V6){
                ThrowException(str); //初期化失敗
            }
        }

        public override string ToString() {
            CheckInitialise();
            return string.Format("{0},{1},{2}", BindStyle, IpV4, IpV6);
        }


        public override bool Equals(object o){
            CheckInitialise(); 
            // 非NULL及び型の確認
		    if (o == null || !(o is BindAddr)) {
			    return false;
		    }

            var b = (BindAddr)o;
            if (BindStyle == b.BindStyle){
                if (IpV4 == b.IpV4){
                    if (IpV6 == b.IpV6){
                        return true;
                    }

                }
            }
            return false;
		}

        public override int GetHashCode(){
            return base.GetHashCode();
        }

        //競合があるかどうかの確認
        public bool CheckCompetition(BindAddr b){
            CheckInitialise(); 
            bool v4Competition = false; //V4競合の可能性
            bool v6Competition = false; //V6競合の可能性
            switch (BindStyle){
                case BindStyle.V46Dual:
                    if (b.BindStyle == BindStyle.V46Dual){
                        v4Competition = true;
                        v6Competition = true;
                    }
                    else if (b.BindStyle == BindStyle.V4Only){
                        v4Competition = true;
                    }
                    else{
                        v6Competition = true;
                    }
                    break;
                case BindStyle.V4Only:
                    if (b.BindStyle != BindStyle.V6Only){
                        v4Competition = true;
                    }
                    break;
                case BindStyle.V6Only:
                    if (b.BindStyle != BindStyle.V4Only){
                        v6Competition = true;
                    }
                    break;
            }

            //V4競合の可能性がある場合
            if (v4Competition){
                //どちらかがANYの場合は、競合している
                if (IpV4.Any || b.IpV4.Any)
                    return true;
                if (IpV4 == b.IpV4)
                    return true;
            }
            //V6競合の可能性がある場合
            if (v6Competition){
                //どちらかがANYの場合は、競合している
                if (IpV6.Any || b.IpV6.Any)
                    return true;
                if (IpV6 == b.IpV6)
                    return true;
            }
            return false;
        }


        //プロトコルを指定してOneBindの配列を取得
        //取得した配列分だけインターフェースへのbindが必要となる
        public OneBind[] CreateOneBind(ProtocolKind protocolKind){
            CheckInitialise();
            var ar = new List<OneBind>();
            if (BindStyle != BindStyle.V4Only){
                ar.Add(new OneBind(IpV6, protocolKind));
            }
            if (BindStyle != BindStyle.V6Only){
                ar.Add(new OneBind(IpV4, protocolKind));
            }
            return ar.ToArray();
        }

    }
}
