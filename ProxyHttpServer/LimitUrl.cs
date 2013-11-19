using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using Bjd.option;

namespace ProxyHttpServer {
    //URL制限
    internal class LimitUrl {
        readonly List<OneLimit> _allowList = new List<OneLimit>();
        readonly List<OneLimit> _denyList = new List<OneLimit>();
        public LimitUrl(IEnumerable<OneDat> allow, IEnumerable<OneDat> deny) {
            foreach (var o in allow) {
                if (!o.Enable)
                    continue; //有効なデータだけを対象にする
                var str = o.StrList[0];
                int n;
                try {
                    n = Convert.ToInt32(o.StrList[1]);
                } catch{
                    var s = o.StrList[1];
                    switch (s){
                        case "Front agreement":
                        case "前方一致":
                            n = 0;
                            break;
                        case "Rear agreement":
                        case "後方一致":
                            n = 1;
                            break;
                        case "Part agreement":
                        case "部分一致":
                            n = 2;
                            break;
                        case "Regular expression":
                        case "正規表現":
                            n = 3;
                            break;
                        default:
                            continue;
                    }
                }
                _allowList.Add(new OneLimit(str, n));
            }
            foreach (var o in deny) {
                if (o.Enable) {//有効なデータだけを対象にする
                    var str = o.StrList[0];

                    int n;
                    try {
                        n = Convert.ToInt32(o.StrList[1]);
                    } catch{
                        var s = o.StrList[1];
                        switch (s){
                            case "Front agreement":
                            case "前方一致":
                                n = 0;
                                break;
                            case "Rear agreement":
                            case "後方一致":
                                n = 1;
                                break;
                            case "Part agreement":
                            case "部分一致":
                                o.StrList[1] = "2";
                                n = 2;
                                break;
                            case "Regular expression":
                            case "正規表現":
                                n = 3;
                                break;
                            default:
                                continue;
                        }
                    }

                    _denyList.Add(new OneLimit(str, n));
                }
            }
        }
        public bool IsAllow(string url,ref string error) {

            //Ver5.9.8 https://のリクエストは、「ホスト名:443」として入ってくる
            var i = url.IndexOf(":443");
            if (i!=-1){
                if (i == url.Length - 4){
                    url = "https://" + url.Substring(0, url.Length - 4) + "/";
                }
            }

            foreach (var o in _allowList) {
                var str = o.IsHit(url);
                if (str == null)
                    continue;
                //allowでヒットした場合は、常にALLOW
                error = string.Format("Allow={0} url={1}", str, url);
                return true;
            }
            foreach (var o in _denyList) {
                var str = o.IsHit(url);
                if (str == null)
                    continue;
                //denyでヒットした場合は、常にDENY
                error = string.Format("Deny={0} url={1}",str,url);
                return false;
            }
            if (_denyList.Count==0 && _allowList.Count > 0) {
                //Allowだけ設定されてい場合
                error = "don't agree in an ALLOW list"; 
                return false;//DENY 
            }
            //Denyだけ設定されてい場合
            //両方設定されている場合
            return true;//ALLOW
        }


        class OneLimit {
            readonly string _str;//URL文字列
            readonly int _n;//0:先頭一致 1:後方一致　2:部分一致 3:正規表現
            public OneLimit(string str, int n) {
                _str = str;
                _n = n;
            }
            //ヒットした場合、ヒットした文字列
            //ヒットしない場合、null
            public string IsHit(string url){
                switch (_n){
                    case 2:
                    case 1:
                    case 0:{
                        int index = url.IndexOf(_str);
                        if (_n == 0) {
                            if (index == 0)//先頭一致
                                return _str;
                        } else if (_n == 1) {//後方一致
                            if (index == url.Length - _str.Length)
                                return _str;
                        } else if (_n == 2) {//部分一致
                            //if (index < 0)
                            if (0 <= index)
                                return _str;
                        }
                    }
                        break;
                    case 3:
                        try {
                            var regex = new Regex(_str);
                            if (regex.Match(url).Success)
                                return _str;
                        } catch{
                            return null;
                        }
                        break;
                }
                return null;
            }
        }

    }

}
