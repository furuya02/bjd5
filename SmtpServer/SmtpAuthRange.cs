using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.acl;
using Bjd.log;
using Bjd.net;
using Bjd.option;

namespace SmtpServer {
    class SmtpAuthRange{

        private readonly List<Acl> _arV4 = new List<Acl>();
        private readonly List<Acl> _arV6 =  new List<Acl>();
        private readonly int _enableEsmtp;//0:適用しない　1:適用する
        private readonly Logger _logger = null;
        
        //(Dat)conf.Get("range")
        //(int)conf.Get("enableEsmtp")
        public SmtpAuthRange(IEnumerable<OneDat> range, int enableEsmtp,Logger logger){

            _logger = logger;
            
            _enableEsmtp = enableEsmtp;

            if (range != null){
                foreach (var o in range){
                    if (o.Enable){//有効なデータだけを対象にする
                        var name = o.StrList[0];
                        var ipStr = o.StrList[1];

                        if (ipStr.IndexOf('.') != -1){//IPv4ルール
                            var acl = new AclV4(name, ipStr);
                            if (!acl.Status){
                                if (_logger != null){
                                    _logger.Set(LogKind.Error, null, 9000040,
                                                string.Format("Name:{0} Address{1}", name, ipStr));
                                }
                            }
                            else{
                                _arV4.Add(acl);
                            }
                        }else{//IPv6ルール
                            var acl = new AclV6(name, ipStr);
                            if (!acl.Status){
                                if (_logger != null){
                                    _logger.Set(LogKind.Error, null, 9000040,
                                                string.Format("Name:{0} Address{1}", name, ipStr));
                                }
                            }
                            else{
                                _arV6.Add(acl);
                            }
                        }
                    }
                }
            }
        }

        public bool IsHit(Ip ip){
            Acl target = null;
            if (ip.InetKind == InetKind.V4) {//IPv4
                foreach (var p in _arV4.Where(p => p.IsHit(ip))) {
                    target = p;
                    break;
                }
            } else {//IPv6
                foreach (var p in _arV6.Where(p => p.IsHit(ip))) {
                    target = p;
                    break;
                }
            }
            if (_enableEsmtp == 0 && target != null) {
                if (_logger != null){
                    _logger.Set(LogKind.Detail, null, 26, string.Format("user:{0} address:{1}", target.Name, ip));
                }
                return false; //適用除外
            }
            if (_enableEsmtp == 1 && target == null) {
                if (_logger != null){
                    _logger.Set(LogKind.Detail, null, 26, string.Format("address:{0}", ip));
                }
                return false;//適用除外
            }
            return true;

        }
    }
}
