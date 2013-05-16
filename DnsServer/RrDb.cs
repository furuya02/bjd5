using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;

namespace DnsServer{


    public class RrDb{

        private readonly List<OneRr> _ar = new List<OneRr>();
        private string _domainName = "ERROR";
        private readonly uint _expire;

        public string GetDomainName(){
            return _domainName;
        }

	    //プロダクトでは使用しないが、テストのためにあえて公開している
        public RrDb(){
            //ドメイン名の初期化
            SetDomainName("example.com."); //テスト用ドメイン名
            _expire = 2400; //テスト用は2400で固定
        }

	    //コンストラクタ
	    //リソース定義（Dat)で初期化する場合
        public RrDb(Logger logger, Conf conf, IEnumerable<OneDat> dat, string dname){
            //ドメイン名の初期化
            SetDomainName(dname);

            //Datの読み込み
            if (dat != null){
                foreach (var o in dat){
                    if (o.Enable){
                        try{
                            AddOneDat(_domainName, o);
                        } catch (ValidObjException e){
                            logger.Set(LogKind.Error, null, 19, string.Format("domain={0} {1}", _domainName, e.Message));
                        }
                    }
                }
            }
            if (conf != null){
                //SOAレコードの追加
                var mail = (string) conf.Get("soaMail");
                var serial = (uint)((int) conf.Get("soaSerial"));
                var refresh = (uint)((int) conf.Get("soaRefresh"));
                var retry = (uint)((int) conf.Get("soaRetry"));
                _expire = (uint)((int) conf.Get("soaExpire")); //expireは、TTL=0のリソースが検索されたとき、TTLに使用するため、クラス変数に保存する
                var minimum = (uint)((int) conf.Get("soaMinimum"));
                if (!InitSoa(_domainName, mail, serial, refresh, retry, _expire, minimum)){
                    logger.Set(LogKind.Error, null, 20, string.Format("domain={0}", _domainName));
                }
            }
        }

	    //コンストラクタ
	    //named.caで初期化する場合
        public RrDb(string namedCaPath, uint expire){
            //ドメイン名の初期化
            SetDomainName(".");
            _expire = expire;

            //named.caの読み込み
            if (namedCaPath != null){
                if (File.Exists(namedCaPath)){
                    var lines = File.ReadAllLines(namedCaPath);
                    //全行のNAMEを保持する　NAMEは前行と同じ場合省略が可能
                    var tmpName = "";
                    foreach (var str in lines){
                        tmpName = AddNamedCaLine(tmpName, str);
                    }
                } else{
                    throw new IOException(string.Format("file not found [{0}]", namedCaPath));
                }
            }
            //locaohostレコードの追加
            InitLocalHost();
        }

        private string AddNamedCaLine(string tmpName, string str){
            //rootCacheは有効期限なし
            const int ttl = 0;

            var name = "";
            //string Class = "IN";
            var dnsType = DnsType.Unknown;
            //;以降はコメントとして削除する
            var i = str.IndexOf(";");
            if (i != -1){
                str = str.Substring(0, i);
            }

            //空の行は処理しない
            if (str.Length == 0){
                return tmpName;
            }

            //空白・タブを削除して、パラメータをtmp2へ取得する
            var tmp = str.Split(new[]{' ', '\t'});
            var tmp2 = tmp.Where(s => s != "").ToList();

            //************************************************
            //タイプだけは省略することができないので、それを基準にサーチする
            //************************************************
            var typeCol = 0;
            for (; typeCol < tmp2.Count; typeCol++){
                foreach (DnsType t in Enum.GetValues(typeof (DnsType))){
                    if (tmp2[typeCol] != t.ToString().ToUpper()){
                        continue;
                    }
                    dnsType = t;
                    break;
                }
                if (dnsType != DnsType.Unknown){
                    break;
                }
            }
            if (dnsType == DnsType.Unknown){
                throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました (タイプ名に矛盾があります [str={0}])", str));
            }

            //タイプの次がDATAとなる
            if (typeCol + 1 >= tmp2.Count){
                throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました  (タイプの次にカラム（DATA）が存在しない [str={0}])", str));
            }
            var dataStr = tmp2[typeCol + 1];

            //************************************************
            //クラス(IN)が含まれているかどうかをサーチする
            //************************************************
            var classCol = 0;
            var find = false;
            for (; classCol < tmp2.Count; classCol++){
                if (tmp2[classCol] != "IN"){
                    continue;
                }
                find = true;
                break;
            }
            if (!find){
                classCol = -1;
            }
            //クラスが含まれた場合、そのカラムはclassColに保存されている
            //含まれていない場合 classCol=-1

            if (typeCol == 1){
                if (classCol == -1){
                    //INが無い場合
                    //０番目はNAME若しくはTTLとなる
                    if (str.Substring(0, 1) == " " || str.Substring(0, 1) == "\t"){
                        //名前は省略されているので
                        //TTLは0で固定 ttl = Integer.valueOf(tmp2.get(0));
                    } else{
                        name = tmp2[0];
                    }
                } else{
                    //INが有る場合
                    //0番目はINであるので、名前もTTLも省略されている
                    if (classCol != 0){
                        throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました (INの位置に矛盾がありま [str={0}])", str));
                    }
                }
            } else if (typeCol == 2){
                if (classCol == -1){
                    //INが無い場合
                    //０番目はNAME、1番目はTTLとなる
                    name = tmp2[0];
                    //TTLは0で固定 ttl = Integer.valueOf(tmp2.get(1));
                } else{
                    //INが有る場合
                    if (classCol != 1){
                        throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました  (INの位置に矛盾がありま [str={0}])", str));
                    }
                    //０番目はNAME若しくはTTLとなる
                    if (str.Substring(0, 1) == " " || str.Substring(0, 1) == "\t"){
                        //名前は省略されているので
                        //TTLは0で固定  ttl = Integer.valueOf(tmp2.get(0));
                    } else{
                        name = tmp2[0];
                    }
                }
            } else if (typeCol == 3){
                if (classCol == -1){
                    //INが無い場合
                    throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました  (カラムが不足している [str={0}])", str));
                }
                //INが有る場合
                if (classCol != 2){
                    throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました  (INの位置に矛盾がありま [str={0}])", str));
                }
                //０番目はNAME、1番目はTTLとなる
                name = tmp2[0];
                //TTLは0で固定 ttl = Integer.valueOf(tmp2.get(1));
            }

            //*********************************************
            //nameの補完
            //*********************************************
            if (name == "@"){
                //@の場合、ドメイン名に置き換えられる
                name = _domainName;
            } else if (name.LastIndexOf(".") != name.Length - 1){
                //最後に.がついていない場合、ドメイン名を追加する
                name = name + "." + _domainName;
            } else if (name == ""){
                name = tmpName; //前行と同じ
            }
            tmpName = name; //前行分として記憶する

            //*********************************************
            //string sataStr を変換してデータベースに追加
            //*********************************************
            if (dnsType == DnsType.A){
                try{
                    var ipV4 = new Ip(dataStr);
                    if (ipV4.InetKind != InetKind.V4){
                        throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました (AレコードにIPv4でないアドレスが指定されました [ip={0} str={1}])", dataStr, str));
                    }
                    Add(new RrA(name, ttl, ipV4));
                } catch (ValidObjException){
                    throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました (Ipアドレスに矛盾があります [ip={0} str={1}])", dataStr, str));
                }
            } else if (dnsType == DnsType.Aaaa){
                try{
                    var ipV6 = new Ip(dataStr);
                    if (ipV6.InetKind != InetKind.V6){
                        throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました (AAAAレコードにIPv6でないアドレスが指定されました [ip={0} str={1}])", dataStr, str));
                    }
                    Add(new RrAaaa(name, ttl, ipV6));
                } catch (ValidObjException){
                    throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました (Ipアドレスに矛盾があります [ip={0} str={1}])", dataStr, str));
                }
            } else if (dnsType == DnsType.Ns){
                Add(new RrNs(name, ttl, dataStr));
            } else{
                throw new IOException(string.Format("ルートサーバ情報の読み込みに失敗しました (タイプA,AAAA及びNS以外は使用できません [str={0}])", str));
            }
            return tmpName;
        }

        private void InitLocalHost(){
            const int ttl = 0; //rootCacheは有効期限なし
            var ip = new Ip(IpKind.V4Localhost);
            Add(new RrA("localhost.", ttl, ip));
            Add(new RrPtr("1.0.0.127.in-addr.arpa.", ttl, "localhost"));

            ip = new Ip(IpKind.V6Localhost);
            Add(new RrAaaa("localhost.", ttl, ip));
            Add(new RrPtr("1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.IP6.ARPA.", ttl, "localhost"));

            //ドメイン名が""空の時にNSレコードを返すため
            Add(new RrNs("localhost.", ttl, "localhost"));
        }

        //ドメイン名の設定
	    //必ず、最後がドットになるように補完される
	    private void SetDomainName(string str){
            //最後に.がついていない場合、追加する
            if (str.LastIndexOf('.') != str.Length - 1){
                str = str + ".";
            }
            _domainName = str;
        }

        //	public void ttlClear() {
        //		long now = Calendar.getInstance().getTimeInMillis();
        //		// 排他制御
        //		synchronized (lock) {
        //			for (int i = ar.Count - 1; i > 0; i--) {
        //				if (!ar.get(i).isEffective(now)) {
        //					ar.Remove(i);
        //				}
        //			}
        //		} // 排他制御
        //	}

        //データが存在するかどうかだけの確認
        public bool Find(string name, DnsType dnsType){
            var list = GetList(name, dnsType);
            if (list.Count != 0){
                return true;
            }
            return false;
        }

	    //リソースの検索
	    //指定したname及びDNS_TYPEにヒットするデータを取得する
	    //クライアントへの送信に使用する場合は、TTLをexpireで書き換える必要がある
        // return 検索で見つからなかった場合は、空の配列を返す
        public List<OneRr> GetList(string name, DnsType dnsType){

            var list = new List<OneRr>();
            //検索中に期限の過ぎたリソースがあった場合は、このリストに追加しておいて最後に削除する
            var removeList = new List<OneRr>();

            var now = DateTime.Now.Ticks/10000000;//秒単位

            // 排他制御
            lock (this){
                foreach (var o in _ar){
                    if (o.DnsType != dnsType){
                        continue;
                    }
                    if (!o.IsEffective(now)){
                        removeList.Add(o);
                        continue; //生存時間超過データは使用しない
                    }
                    if (o.Name.ToUpper() != name.ToUpper()){
                        continue; //大文字で比較される
                    }

                    //bool find = rrList.Any(o => o.Data == oneRR.Data);//データが重複していない場合だけ、リストに追加する
                    //データが重複していない場合だけ、リストに追加する
                    //bool find = false;
                    //foreach(OneRr o in rrList) {
                    //	if (o.Data == o.Data) {
                    //		find = true;
                    //		break;
                    //	}
                    //}
                    //if (find) {
                    //	continue;
                    //}
                    //int ttl = Util.htonl(soaExpire);
                    //	

                    //TTLだけを修正したクローンを作成してresultリストに追加する
                    var ttl = o.Ttl;
                    //TTLが0の場合、「基本設定」の「最少時間」を使用する
                    if (ttl == 0){
                        ttl = (uint)_expire;
                    }
                    list.Add(o.Clone(ttl));
                }
                //期限の過ぎたリソースの削除
                foreach (var o in removeList){
                    _ar.Remove(o);
                }
            } // 排他制御
            return list;
        }

	    //リソースの追加
	    //同一のリソース（TTL以外）は上書きされる
	    //ただしTTL=0のデータは上書きされない
        public bool Add(OneRr oneRr){
            // 排他制御
            lock (this){
                OneRr target = null; //書き換え対象のリソース
                //TTL以外が全て同じのソースを検索する
                foreach (var o in _ar){
                    if (o.DnsType == oneRr.DnsType && o.Name == oneRr.Name){
                        //データ内容の確認	
                        var isSame = !o.Data.Where((t, n) => t != oneRr.Data[n]).Any();
                        if (isSame){
                            if (o.Ttl == 0){
                                //TTL=0のデータは普遍であるため、書き換えはしない
                                return false;
                            }
                            target = o;
                            break;
                        }
                    }
                }
                //書き換えの対象が見つかっている場合は、削除する
                if (target != null){
                    _ar.Remove(target);
                }
                _ar.Add(oneRr);
            }
            return true;
        }
        public int Count{
            get{
                return _ar.Count;
            }
        }

        //プロダクトでは使用しないが、テストのためにあえてメソッドにしている
        private OneRr Get(int index){
            return _ar[index];
        }

        //プロダクトでは使用しないが、テストのためにあえてメソッドにしている
        private int Size(){
            return _ar.Count;
        }

        //OneDatを追加する 
        private void AddOneDat(string domainName, OneDat o){
            if (!o.Enable){
                throw new ValidObjException("isEnable=false");
            }

            var type = Int32.Parse(o.StrList[0]);
            var name = o.StrList[1];
            var alias = o.StrList[2];
            Ip ip = null;
            if (type != 3){
                //Cnameの時、Ipアドレスが入っていないので、例外が発生する
                ip = new Ip(o.StrList[3]);
            }
            var priority = ushort.Parse(o.StrList[4]);
            const int ttl = 0; //有効期限なし

            //最後に.がついていない場合、ドメイン名を追加する
            if (name.LastIndexOf('.') != name.Length - 1){
                name = name + "." + domainName;
            }
            if (alias.LastIndexOf('.') != alias.Length - 1){
                alias = alias + "." + domainName;
            }

            DnsType dnsType;
            switch (type){
                case 0:
                    dnsType = DnsType.A;
                    if (ip != null && ip.InetKind != InetKind.V4){
                        throw new ValidObjException("IPv6 cannot address it in an A(PTR) record");
                    }
                    Add(new RrA(name, ttl, ip));
                    break;
                case 1:
                    dnsType = DnsType.Ns;
                    Add(new RrNs(domainName, ttl, name));
                    break;
                case 2:
                    dnsType = DnsType.Mx;
                    Add(new RrMx(domainName, ttl, priority, name));
                    break;
                case 3:
                    dnsType = DnsType.Cname;
                    Add(new RrCname(alias, ttl, name));
                    break;
                case 4:
                    dnsType = DnsType.Aaaa;
                    if (ip != null && ip.InetKind != InetKind.V6){
                        throw new ValidObjException("IPv4 cannot address it in an AAAA record");
                    }
                    Add(new RrAaaa(name, ttl, ip));
                    break;
                default:
                    throw new ValidObjException(string.Format("unknown type ({0})", type));
            }

            //MX及びNSの場合は、A or AAAAも追加する
            if (dnsType == DnsType.Mx || dnsType == DnsType.Ns){
                if (ip != null && ip.InetKind == InetKind.V4){
                    Add(new RrA(name, ttl, ip));
                } else{
                    Add(new RrAaaa(name, ttl, ip));
                }
            }
            //CNAME以外は、PTRレコードを自動的に生成する
            if (dnsType != DnsType.Cname){
                //PTR名を作成 [例] 192.168.0.1 -> 1.0.168.192.in-addr.arpa;
                if (ip != null && ip.InetKind == InetKind.V4){
                    //IPv4
                    var ptrName = string.Format("{0}.{1}.{2}.{3}.in-addr.arpa.", (ip.IpV4[3] & 0xff), (ip.IpV4[2] & 0xff), (ip.IpV4[1] & 0xff), (ip.IpV4[0] & 0xff));
                    Add(new RrPtr(ptrName, ttl, name));
                } else{
                    //IPv6
                    var sb = new StringBuilder();
                    if (ip != null)
                        foreach (var a in ip.IpV6){
                            sb.Append(string.Format("{0:x2}", a));
                        }
                    var ipStr = sb.ToString();
                    if (ipStr.Length == 32){
                        sb = new StringBuilder();
                        for (var e = 31; e >= 0; e--){
                            sb.Append(ipStr[e]);
                            sb.Append('.');
                        }
                        Add(new RrPtr(sb + "ip6.arpa.", ttl, name));
                    }
                }
            }
        }

        //SOAレコードの追加
        //OneDatでデータを読みこんだ後、このメソッドでSOAレコードを追加する
        //既に対象ドメインのSOAレコードが有る場合は、TTL=0で処理なし TTL!=0で置換
        //対象ドメインのNSレコードが無い場合、処理なし（NSサーバの情報が無いため）
        private bool InitSoa(string domainName, string mail, uint serial, uint refresh, uint retry, uint expire, uint minimum){

            //NSサーバ
            string nsName = null;

            //DB上の対象ドメインのNSレコードを検索
            foreach (var o in _ar){
                //DB上に対象ドメインのNSレコードが有る場合
                if (o.DnsType == DnsType.Ns && o.Name == domainName){
                    nsName = ((RrNs) o).NsName;
                    break;
                }
            }
            if (nsName == null){
                //NSサーバの情報が無い場合は、SOAの追加はできない
                return false;
            }

            //DB上の対象ドメインのSOAレコードを検索
            for (var i = 0; i < _ar.Count; i++){
                if (_ar[i].DnsType == DnsType.Soa){
                    var soa = (RrSoa) _ar[i];
                    if (soa.Name == domainName){
                        if (soa.Ttl == 0){
                            //既存情報のTTLが0の場合、処置できない
                            return false;
                        }
                        _ar.RemoveAt(i); //削除
                        break;
                    }
                }
            }
            //SOAレコードの追加
            const int ttl = 0; //有効期限なし
            var soaMail = mail.Replace('@', '.'); //@を.に置き換える
            soaMail = soaMail + "."; //最後に.を追加する
            _ar.Add(new RrSoa(domainName, ttl, nsName, soaMail, serial, refresh, retry, expire, minimum));
            return true;
        }
    }
}
