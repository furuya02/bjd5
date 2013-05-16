using System;
using System.Collections.Generic;
using Bjd.sock;

namespace Bjd.log{
    public class TmpLogger : Logger{

        private readonly List<LogTemporary> _ar = new List<LogTemporary>();

        public new void Set(LogKind logKind, SockObj sockObj, int messageNo, String detailInfomation){
            _ar.Add(new LogTemporary(logKind, sockObj, messageNo, detailInfomation));
        }

        private class LogTemporary{
            public LogKind LogKind { get; private set; }
            public SockObj SockObj { get; private set; }
            public int MessageNo { get; private set; }
            public string DetailInfomation { get; private set; }

            public LogTemporary(LogKind logKind, SockObj sockObj, int messageNo, String detailInfomation){
                LogKind = logKind;
                SockObj = sockObj;
                MessageNo = messageNo;
                DetailInfomation = detailInfomation;
            }
        }

        /**
         * 溜まったログをloggerに送る
         * @param logger
         */

        public void Release(Logger logger){
            foreach (var a in _ar){
                logger.Set(a.LogKind, a.SockObj, a.MessageNo, a.DetailInfomation);
            }
            _ar.Clear();

        }
    }
}
