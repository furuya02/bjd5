using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Bjd.log;
using Bjd.net;
using Bjd.option;

namespace Bjd.acl{
    //複数のACLを保持して、範囲に該当するかどうかをチェックする
    //ACLの初期化に失敗した場合は、Loggerにエラーを表示し、その行は無効になる
    public class AclList{
        private readonly List<Acl> _arV4 = new List<Acl>();
        private readonly List<Acl> _arV6 = new List<Acl>();
        private readonly bool _enable; //許可:0 不許可:1
        private readonly Logger _logger;


        //Ver6.0.2
        // *.example.com のようにFQDNでも指定できるように仕様変更
        private readonly  List<FqdnAcl> _arFqdnAcls = new List<FqdnAcl>();

        //Dat dat==null　で初期化された場合、全てenableNumで指定したものになる
        //dat=null enableNum=0(不許可) => All Deny
        //dat=null enableNum=1(許可) => All Allow
        public AclList(IEnumerable<OneDat> dat, int enableNum, Logger logger){
            _enable = (enableNum == 1);
            _logger = logger;
            if (dat == null){
                return;
            }
            foreach (var o in dat){
                if (!o.Enable){
                    continue;
                }
                //有効なデータだけを対象にする
                var name = o.StrList[0];
                var ipStr = o.StrList[1];

                if (ipStr == "*"){
                    //全部
                    try{
                        var acl = new AclV4(name, ipStr);
                        _arV4.Add(acl);
                    } catch (ValidObjException){
                        logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ipStr));
                    }
                    try{
                        var acl = new AclV6(name, ipStr);
                        _arV6.Add(acl);
                    } catch (ValidObjException){
                        logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ipStr));
                    }
                } else{
                    //IPv4指定かどうかの判断
                    var isV4 = true;
                    foreach (var c in ipStr){
                        if (char.IsNumber(c)){
                            continue;
                        }
                        if (c=='*' || c=='.' || c=='/' || c=='-'){
                            continue;
                        }
                        isV4 = false;
                        break;
                    }
                    if (isV4){
                        //IPv4ルール
                        try{
                            var acl = new AclV4(name, ipStr);
                            _arV4.Add(acl);
                        } catch (ValidObjException){
                            logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ipStr));
                        }

                    } else{
                        //IPv6指定かどうかの判断
                        var isV6 = true;
                        foreach (var c in ipStr) {
                            if (char.IsNumber(c)) {
                                continue;
                            }
                            if (c == '*' || c == ':' || c == '[' || c == ']' || c == '-') {
                                continue;
                            }
                            isV6 = false;
                            break;
                        }
                        if (isV6){
                            //IPv6ルール
                            try{
                                var acl = new AclV6(name, ipStr);
                                _arV6.Add(acl);
                            } catch (ValidObjException){
                                logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ipStr));
                            }

                        }else{ //FQDN指定と判断する
                            _arFqdnAcls.Add(new FqdnAcl(name,ipStr));
                        }
                        
                    }
                }
            }
        }

//Ver6.0.1以前
//        public AclList(IEnumerable<OneDat> dat, int enableNum, Logger logger) {
//            _enable = (enableNum == 1);
//            _logger = logger;
//            if (dat == null) {
//                return;
//            }
//            foreach (var o in dat) {
//                if (!o.Enable) {
//                    continue;
//                }
//                //有効なデータだけを対象にする
//                var name = o.StrList[0];
//                var ipStr = o.StrList[1];
//
//                if (ipStr == "*") {
//                    //全部
//                    try {
//                        var acl = new AclV4(name, ipStr);
//                        _arV4.Add(acl);
//                    } catch (ValidObjException) {
//                        logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ipStr));
//                    }
//                    try {
//                        var acl = new AclV6(name, ipStr);
//                        _arV6.Add(acl);
//                    } catch (ValidObjException) {
//                        logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ipStr));
//                    }
//                } else if (ipStr.IndexOf('.') != -1) {
//                    //IPv4ルール
//                    try {
//                        var acl = new AclV4(name, ipStr);
//                        _arV4.Add(acl);
//                    } catch (ValidObjException) {
//                        logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ipStr));
//                    }
//                } else {
//                    //IPv6ルール
//                    try {
//                        var acl = new AclV6(name, ipStr);
//                        _arV6.Add(acl);
//                    } catch (ValidObjException) {
//                        logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ipStr));
//                    }
//                }
//            }
//        }



        //ACLリストへのIP追加 ダイナミックにアドレスをDenyリストに加えるためのメソッド<br>
        //追加に失敗した場合、Loggerにはエラー表示されるが、追加は無効（追加されない） 
        public bool Append(Ip ip){
            if (!_enable){
                return false;
            }

            if (ip.InetKind == InetKind.V4){
                if (_arV4.Any(a => a.IsHit(ip))){
                    return false;
                }
            } else{
                if (_arV6.Any(a => a.IsHit(ip))){
                    return false;
                }
            }

            var dt = DateTime.Now;
            var name = string.Format("AutoDeny-{0}", dt.ToString());

            if (ip.InetKind == InetKind.V4){
                try{
                    var acl = new AclV4(name, ip.ToString());
                    _arV4.Add(acl);
                } catch (ValidObjException){
                    _logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ip));
                }
            } else{
                try{
                    var acl = new AclV6(string.Format("AutoDeny-{0}", dt.ToString()), ip.ToString());
                    _arV6.Add(acl);
                } catch (ValidObjException){
                    _logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address {1}", name, ip));
                }
            }
            return true;
        }

        //ACLリストにヒットするかどうかのチェック
        //範囲にヒットしたものが有効/無効のフラグ（enable）も内部で評価されている
        public AclKind Check(Ip ip){

            //ユーザリストの照合
            Acl acl = null;
            var hostName = ""; //Ver6.0.2

            //Ver6.0.2 最初にFQDNでのヒットを確認する
            if (_arFqdnAcls.Count != 0){
                var hostInfo = Dns.GetHostEntry(ip.IPAddress);
                hostName = hostInfo.HostName;

                foreach (FqdnAcl p in _arFqdnAcls) {
                    if (p.IsHit(ip,hostName)) {
                        acl = p;
                        break;
                    }
                }
            }

            if (ip.InetKind == InetKind.V4){
                foreach (Acl p in _arV4){
                    if (p.IsHit(ip)){
                        acl = p;
                        break;
                    }
                }
            } else{
                foreach (Acl p in _arV6){
                    if (p.IsHit(ip)){
                        acl = p;
                        break;
                    }
                }
            }

            if (!_enable && acl == null){
                if (hostName != ""){
                    _logger.Set(LogKind.Secure, null, 9000017, string.Format("address:{0} hostname:{1}", ip,hostName)); //このアドレスからのリクエストは許可されていません
                } else{
                    _logger.Set(LogKind.Secure, null, 9000017, string.Format("address:{0}", ip)); //このアドレスからのリクエストは許可されていません
                }
                return AclKind.Deny;
            }
            if (_enable && acl != null){
                if (hostName != ""){
                    _logger.Set(LogKind.Secure, null, 9000018, string.Format("aclName:{0} address:{1} hostName:{2}", acl.Name, ip,hostName)); //この利用者のアクセスは許可されていません
                } else{
                    _logger.Set(LogKind.Secure, null, 9000018, string.Format("aclName:{0} address:{1}", acl.Name, ip)); //この利用者のアクセスは許可されていません
                }
                return AclKind.Deny;
            }
            return AclKind.Allow;
        }
    }
}

/*
    public class AclList {
        readonly List<Acl> _arV4 = new List<Acl>();
        readonly List<Acl> _arV6 = new List<Acl>();

        readonly bool _enable;//許可:0 不許可:1
        readonly Logger _logger;

        public AclList(Dat dat,int enableNum,Logger logger) {
            _enable = (enableNum == 1);
            _logger = logger;
            foreach (var o in dat) {
                if (o.Enable) {//有効なデータだけを対象にする
                    var name = o.StrList[0];
                    var ipStr= o.StrList[1];

                    if (ipStr == "*") {//全部
                        Acl acl = new AclV4(name, ipStr);
                        if (!acl.Status) {
                            logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address{1}", name, ipStr));
                        } else {
                            _arV4.Add(acl);
                        }
                        acl = new AclV6(name, ipStr);
                        if (!acl.Status) {
                            logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address{1}", name, ipStr));
                        } else {
                            _arV6.Add(acl);
                        }
                    } else if (ipStr.IndexOf('.') != -1) {//IPv4ルール
                        var acl = new AclV4(name, ipStr);
                        if (!acl.Status) {
                            logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address{1}", name, ipStr));
                        } else {
                            _arV4.Add(acl);
                        }
                    } else {//IPv6ルール
                        var acl = new AclV6(name, ipStr);
                        if (!acl.Status) {
                            logger.Set(LogKind.Error, null, 9000034, string.Format("Name:{0} Address{1}", name, ipStr));
                        } else {
                            _arV6.Add(acl);
                        }
                    }
                }
            }
        }
        //リストへの追加
        public bool Append(Ip ip) {
            if (!_enable){
                return false;
            }

            if (ip.InetKind == InetKind.V4){
                if (_arV4.Any(p => p.IsHit(ip))){
                    return false;
                }
//                var acl = new AclV4(string.Format("AutoDeny-{0}", DateTime.Now), ip.ToString());
                _arV4.Add(new AclV4(string.Format("AutoDeny-{0}", DateTime.Now), ip.ToString()));
            }
            else{
                if (_arV6.Any(p => p.IsHit(ip))){
                    return false;
                }
                //var acl = new AclV6(string.Format("AutoDeny-{0}", DateTime.Now), ip.ToString());
                _arV6.Add(new AclV6(string.Format("AutoDeny-{0}", DateTime.Now), ip.ToString()));
            }
            return true;
        }


       //ACLリストにヒットするかどうかのチェック<br>
       //範囲にヒットしたものが有効/無効のフラグ（enable）も内部で評価されている
        public AclKind Check(Ip ip) {

            //ユーザリストの照合
            Acl acl = null;
            if (ip.InetKind == InetKind.V4){
                foreach (var p in _arV4.Where(p => p.IsHit(ip))){
                    acl = p;
                    break;
                }
            } else{
                foreach (var p in _arV6.Where(p => p.IsHit(ip))){
                    acl = p;
                    break;
                }
            }

            if (!_enable && acl == null){
                _logger.Set(LogKind.Secure, null, 9000017, string.Format("address:{0}", ip)); //このアドレスからのリクエストは許可されていません
                return AclKind.Deny;
            }
            if (_enable && acl != null){
                _logger.Set(LogKind.Secure, null, 9000018, string.Format("aclName:{0} address:{1}", acl.Name, ip)); //この利用者のアクセスは許可されていません
                return AclKind.Deny;
            }
            return AclKind.Allow;

        }

    }

}
*/