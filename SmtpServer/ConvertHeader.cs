﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bjd.log;
using Bjd.mail;
using Bjd.option;

namespace SmtpServer {
    class ConvertHeader {

        //ヘッダ置換
        readonly Dictionary<String, String> _replace = new Dictionary<string, string>();
        //ヘッダ追加
        readonly Dictionary<String, String> _append = new Dictionary<string, string>();

        //(Dat)Conf.Get("patternList")
        //(Dat)Conf.Get("appendList")
        public ConvertHeader(Dat replace,Dat append){
            if (replace != null){
                foreach (var d in replace){
                    if (d.Enable){
                        _replace.Add(d.StrList[0],d.StrList[1]);
                    }
                }
            }
            if (append != null) {
                foreach (var d in append) {
                    if (d.Enable) {
                        _append.Add(d.StrList[0], d.StrList[1]);
                    }
                }
            }
            
        }

        //変換と追加
        public void Exec(Mail mail,Logger logger){
            //ヘッダ変換
            foreach (var a in _replace){
                if (mail.RegexHeader(a.Key,a.Value)){
                    if (logger != null) {
                        logger.Set(LogKind.Normal, null, 16, string.Format("{0} -> {1}", a.Key,a.Value));
                    }
                }
            }
            //ヘッダの追加
            foreach (var a in _append){
                mail.AddHeader(a.Key, a.Value);
                if (logger != null){
                    logger.Set(LogKind.Normal, null, 17, string.Format("{0}: {1}", a.Key, a.Value));
                }
            }
        }
    }
}
