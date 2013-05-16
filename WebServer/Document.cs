using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.sock;
using Bjd.util;

namespace WebServer {
    //********************************************************
    //ドキュメント生成クラス
    //********************************************************
    class Document {
        //readonly Kernel kernel;
        readonly Logger _logger;
        //readonly OneOption _oneOption;
        readonly Conf _conf;
        readonly SockTcp _sockTcp;
        readonly ContentType _contentType;

        //byte[] doc = new byte[0];
        readonly Body _body;
        
        //送信ヘッダ
        readonly Header _sendHeader;

        public bool SetRangeTo{get;set;}//Rangeヘッダで範囲（終わり）が指定された場合True

        public Document(Kernel kernel, Logger logger, Conf conf, SockTcp tcpObj, ContentType contentType) {
            //this.kernel = kernel;
            _logger = logger;
            //_oneOption = oneOption;
            _conf = conf;
            _sockTcp = tcpObj;
            _contentType = contentType;

            SetRangeTo = false;

            //送信ヘッダ初期化
            _sendHeader = new Header();
            _sendHeader.Replace("Server", Util.SwapStr("$v", kernel.Ver.Version(), (string)_conf.Get("serverHeader")));
            _sendHeader.Replace("MIME-Version","1.0");
            _sendHeader.Replace("Date",Util.UtcTime2Str(DateTime.UtcNow));

            _body = new Body();
        }
        //Location:ヘッダを含むかどうか
        public bool SearchLocation(){
            return null != _sendHeader.GetVal("Location");
        }

        public void Clear() {
            _body.Clear();
            _sendHeader.Replace("Content-Length",_body.Length.ToString());
            //_sendHeader.Replace("Content-Length",string.Format("{0}",_body.Length));
        }

        //*********************************************************************
        // 送信
        //*********************************************************************
        //public void Send(bool keepAlive,ref bool life) {
        public void Send(bool keepAlive,ILife iLife) {
            _sendHeader.Replace("Connection", keepAlive ? "Keep-Alive" : "close");

            //ヘッダ送信
            _sockTcp.SendUseEncode(_sendHeader.GetBytes());//ヘッダ送信
            //本文送信
            if(_body.Length>0) {
                //Ver5.0.0-b12
                //if(sendHeader.GetVal("Content-Type").ToLower().IndexOf("text")!=-1){
                var contentType = _sendHeader.GetVal("Content-Type");
                if(contentType!=null && contentType.ToLower().IndexOf("text")!=-1){
                    _body.Send(_sockTcp,true,iLife);
                    //tcpObj.SendUseEncode(body.Get());   
                }else{
                    _body.Send(_sockTcp,false,iLife);
                    //tcpObj.SendNoEncode(body.Get());   
                }
            }
        }

        public void AddHeader(string key,string val) {
            _sendHeader.Append(key,Encoding.ASCII.GetBytes(val));
        }
        //Encoding.ASCII以外でエンコードしたい場合、こちらを使用する
        public void AddHeader(string key,byte [] val) {
            _sendHeader.Append(key,val);
        }

        //*********************************************************************
        // ドキュメント生成
        //*********************************************************************
        public bool CreateFromFile(string fileName,long rangeFrom,long rangeTo) {
            if (File.Exists(fileName)) {

                _body.Set(fileName,rangeFrom,rangeTo);
                
                //Ver5.4.0
                var l = _body.Length;
                if (SetRangeTo && rangeFrom==0)
                    l++;
                _sendHeader.Replace("Content-Length", l.ToString());
                _sendHeader.Replace("Content-Type",_contentType.Get(fileName));

                return true;
            }
            return false;
        }
        //public void SetDoc(byte [] buf){
        //    doc = new byte[buf.Length];
        //    Buffer.BlockCopy(buf,0,
        //    sendHeader.Replace("Content-Length",doc.Length.ToString());
        //}

        public void CreateFromXml(string str) {

            _body.Set(Encoding.UTF8.GetBytes(str));
            _sendHeader.Replace("Content-Length",_body.Length.ToString());
            
            _sendHeader.Replace("Content-Type","text/xml; charset=\"utf-8\"");
        }

        public void CreateFromSsi(byte[] output,string fileName) {
            _body.Set(output);
            _sendHeader.Replace("Content-Length",_body.Length.ToString());
            _sendHeader.Replace("Content-Type",_contentType.Get(fileName));
        }
        // CGIで得られた出力から、SendHeader及びdocを生成する
        public bool CreateFromCgi(byte[] output) {
            while (true) {
                var tmp = new byte[output.Length];
                Buffer.BlockCopy(output,0,tmp,0,output.Length);
                for (var i = 0;;i++) {
                    if (tmp.Length <= i)
                        return false;
                    if (tmp[i] != 0x0a)
                        continue; //'\n'
                    var buf = new byte[i];
                    Buffer.BlockCopy(tmp,0,buf,0,i);
                    var line = Encoding.ASCII.GetString(buf);
                    line = line.TrimEnd('\r');
                        
                    if (line.Length > 0) {
                        var n = line.IndexOf(':');
                        if (0 <= n) {
                            var tag = line.Substring(0,n);
                            var val = line.Substring(n + 1).Trim();
                            _sendHeader.Append(tag.Trim(),Encoding.ASCII.GetBytes(val));
                        } else {
                            goto end;
                        }
                    }
                    var len = output.Length - i - 1;
                    output = new byte[len];
                    Buffer.BlockCopy(tmp,i + 1,output,0,len);

                    if (line.Length == 0) {
                        _body.Set(output);
                        _sendHeader.Replace("Content-Length",_body.Length.ToString());
                        return true;
                    }
                    break;
                end:
                    _body.Set(output);
                    _sendHeader.Replace("Content-Length",_body.Length.ToString());
                    return true;
                }
            }
        }
        //Ver5.0.0-a20 エンコードはオプション設定に従う
        bool GetEncodeOption(out Encoding encoding,out string charset) {
            charset = "utf-8";
            encoding = Encoding.UTF8;
            switch ((int)_conf.Get("encode")) {
                case 0://UTF-8
                    return true;
                case 1://shift-jis
                    charset = "Shift-JIS";
                    encoding = Encoding.GetEncoding("shift-jis");
                    return true;
                case 2://eyc
                    charset = "euc-jp";
                    encoding = Encoding.GetEncoding("euc-jp");
                    return true;
            }
            return false;
        }


        public bool CreateFromErrorCode(Request request,int responseCode) {

            //Ver5.0.0-a20 エンコードはオプション設定に従う
            Encoding encoding;
            string charset;
            if (!GetEncodeOption(out encoding,out charset)) {
                return false;
            }
            
            //レスポンス用の雛形取得
            var lines = Inet.GetLines((string)_conf.Get("errorDocument"));
            if (lines.Count == 0) {
                _logger.Set(LogKind.Error,null,25,"");
                return false;
            }

            //バッファの初期化
            var sb = new StringBuilder();

            //文字列uriを出力用にサイタイズする（クロスサイトスクリプティング対応）
            var uri = Inet.Sanitize(request.Uri);

            //雛形を１行づつ読み込んでキーワード変換したのち出力用バッファに蓄積する
            foreach(string line in lines){
                string str = line;
                str = Util.SwapStr("$MSG", request.StatusMessage(responseCode), str);
                str = Util.SwapStr("$CODE", responseCode.ToString(), str);
                str = Util.SwapStr("$SERVER", Define.ApplicationName(), str);
                str = Util.SwapStr("$VER", request.Ver, str);
                str = Util.SwapStr("$URI", uri, str);
                sb.Append(str + "\r\n");
            }
            _body.Set(encoding.GetBytes(sb.ToString()));
            _sendHeader.Replace("Content-Length",_body.Length.ToString());
            _sendHeader.Replace("Content-Type",string.Format("text/html;charset={0}",charset));
            return true;

        }
        public bool CreateFromIndex(Request request,string path) {

            //Ver5.0.0-a20 エンコードはオプション設定に従う
            Encoding encoding;
            string charset;
            if (!GetEncodeOption(out encoding,out charset)) {
                return false;
            }

            //レスポンス用の雛形取得
            var lines = Inet.GetLines((string)_conf.Get("indexDocument"));
            if (lines.Count == 0) {
                _logger.Set(LogKind.Error,null, 26, "");
                return false;
            }

            //バッファの初期化
            var sb = new StringBuilder();

            //文字列uriを出力用にサイタイズする（クロスサイトスクリプティング対応）
            var uri = Inet.Sanitize(request.Uri);


            //雛形を１行づつ読み込んでキーワード変換したのち出力用バッファに蓄積する
            foreach(string line in lines){
                var str = line;
                if (str.IndexOf("<!--$LOOP-->") == 0) {
                    str = str.Substring(12);//１行の雛型

                    //一覧情報の取得(１行分はLineData)
                    var lineDataList = new List<LineData>();
                    var dir = request.Uri;
                    if (1 < dir.Length) {
                        if (dir[dir.Length - 1] != '/')
                            dir = dir + '/';
                    }
                    //string dirStr = dir.Substring(0,dir.LastIndexOf('/'));
                    if (dir != "/") {
                        //string parentDirStr = dirStr.Substring(0,dirStr.LastIndexOf('/') + 1);
                        //lineDataList.Add(new LineData(parentDirStr,"Parent Directory","&lt;DIR&gt;","-"));
                        lineDataList.Add(new LineData("../","Parent Directory","&lt;DIR&gt;","-"));
                    }

                    var di = new DirectoryInfo(path);
                    foreach (var info in di.GetDirectories("*.*")) {
                        var href = Uri.EscapeDataString(info.Name) + '/';
                        lineDataList.Add(new LineData(href, info.Name, "&lt;DIR&gt;", "-"));
                    }
                    foreach (var info in di.GetFiles("*.*")) {
                        string href = Uri.EscapeDataString(info.Name);
                        lineDataList.Add(new LineData(href, info.Name, info.LastWriteTime.ToString(), info.Length.ToString()));
                    }

                    //位置情報を雛形で整形してStringBuilderに追加する
                    foreach (var lineData in lineDataList)
                        sb.Append(lineData.Get(str) + "\r\n");

                } else {//一覧行以外の処理
                    str = Util.SwapStr("$URI",uri,str);
                    str = Util.SwapStr("$SERVER",Define.ApplicationName(),str);
                    str = Util.SwapStr("$VER", request.Ver, str);
                    sb.Append(str + "\r\n");
                }
            }
            _body.Set(encoding.GetBytes(sb.ToString()));
            _sendHeader.Replace("Content-Length",_body.Length.ToString());
            _sendHeader.Replace("Content-Type",string.Format("text/html;charset={0}",charset));
            return true;
        }
        //CreateIndexDocument()で使用される
        private class LineData {
            readonly string _href;
            readonly string _name;
            readonly string _date;
            readonly string _size;
            public LineData(string href, string name, string date, string size) {
                _href = Util.SwapStr(" ", "%20", href);
                _name = name;
                _date = date;
                _size = size;
            }
            //雛型(str)のキーワードを置き変えて１行のデータを取得する
            public string Get(string str) {
                var tmp = str;
                tmp = Util.SwapStr("$HREF", _href, tmp);
                tmp = Util.SwapStr("$NAME", _name, tmp);
                tmp = Util.SwapStr("$DATE", _date, tmp);
                tmp = Util.SwapStr("$SIZE", _size, tmp);
                return tmp;
            }
        }


        class Body {

            enum KindBuf {
                Memory=0,
                Disk=1,
            }

            KindBuf _kindBuf;
            byte[] _doc;
            
            string _fileName;
            long _rangeFrom;
            long _rangeTo;

            public Body() {
                _kindBuf = KindBuf.Memory;
                _doc = new byte[0];
            }
            public void Clear(){
                _kindBuf = KindBuf.Memory;
                _doc = new byte[0];
            }

            public void Set(string fileName,long rangeFrom,long rangeTo) {
                _kindBuf = KindBuf.Disk;
                _fileName = fileName;
                _rangeFrom = rangeFrom;
                _rangeTo = rangeTo;
            }
            public void Set(byte [] buf) {
                _kindBuf = KindBuf.Memory;
                _doc = new byte[buf.Length];
                Buffer.BlockCopy(buf,0,_doc,0,buf.Length);
            }
            
            public long Length {
                get {
                    if(_kindBuf == KindBuf.Memory) {
                        return _doc.Length;
                    }
                    return _rangeTo - _rangeFrom + ((_rangeFrom == 0) ? 0 : 1);
                }
            }
            //public bool Send(SockTcp tcpObj,bool encode,ref bool life){
            public bool Send(SockTcp tcpObj,bool encode,ILife iLife){
                if(_kindBuf == KindBuf.Memory) {
                    if(encode) {
                        if(-1 == tcpObj.SendUseEncode(_doc))
                            return false;
                    } else {
                        if(-1 == tcpObj.SendNoEncode(_doc))
                            return false;
                    }
                } else {
                    using(var fs = new FileStream(_fileName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) {
                        using(var br = new BinaryReader(fs)) {
                            fs.Seek(_rangeFrom,SeekOrigin.Begin);
                            var start = _rangeFrom;
                            while(iLife.IsLife()) {
                                long size = _rangeTo - start + 1;
                                if(size > 1048560)
                                    size = 1048560;
                                if(size <= 0)
                                    break;
                                _doc = new byte[size];
                                int len = br.Read(_doc,0,(int)size);
                                if(len <= 0)
                                    break;

                                if(len != size) {
                                    var tmp = new byte[len];
                                    Buffer.BlockCopy(_doc,0,tmp,0,len);
                                    _doc = tmp;
                                }
                                
                                if(encode) {
                                    if(-1 == tcpObj.SendUseEncode(_doc)) {
                                        return false;
                                    }
                                } else {
                                    if(-1 == tcpObj.SendNoEncode(_doc)) {
                                        return false;
                                    }
                                }
                                start += _doc.Length;
                                if(_rangeTo - start <= 0) 
                                    break;
                                Thread.Sleep(1);
                            }
                            br.Close();
                        }
                        fs.Close();
                    }
                }
                return true;
            }
        }
    }
    
}
