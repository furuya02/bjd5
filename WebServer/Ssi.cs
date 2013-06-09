using System;
using System.IO;
using System.Linq;
using System.Text;
using Bjd;
using Bjd.log;
using Bjd.option;
using Bjd.sock;
using Bjd.util;

namespace WebServer {
    class Ssi {
        readonly Kernel _kernel;
        readonly Logger _logger;
        //readonly OneOption _oneOption;
        readonly Conf _conf;

        string _timeFmt = "ddd M dd hh:mm:ss yyyy";//(SSI用)日付書式文字列
        string _sizeFmt = "bytes";

        //子プロセスでCGIを実行する場合に使用する
        Target _target;
        readonly SockTcp _sockTcp;
        readonly Request _request;
        readonly Header _recvHeader;

        public Ssi(Kernel kernel, Logger logger, Conf conf, SockTcp tcpObj, Request request, Header recvHeader) {
            _kernel = kernel;
            _logger = logger;
            //_oneOption = oneOption;
            _conf = conf;

            //子プロセスでCGIを実行する場合に使用する
            _sockTcp = tcpObj;
            _request = request;
            _recvHeader = recvHeader;
        }

        public bool Exec(Target target, Env env, WebStream output) {
            _target = target;

            //出力用バッファ
            var sb = new StringBuilder();

            var encoding = MLang.GetEncoding(target.FullPath);

            //***************************************************
            // 対象ファイルの読み込み
            //***************************************************
            using (var sr = new StreamReader(target.FullPath, encoding)) {
                while (true) {
                    var str = sr.ReadLine();
                    if (str == null)
                        break;
                    //SSIキーワードを検索して置き換える（再帰処理）
                    SsiConvert(ref str, encoding);
                    sb.Append(str + "\r\n");
                }
                sr.Close();
            }
            output.Add(encoding.GetBytes(sb.ToString()));
            return true;
        }

        enum SsiKind {
            Unknown = 0,
            Exec = 1,
            Config = 2,
            Echo = 3,
            Flastmod = 4,
            Fsize = 5,
            Include = 6
        }

        //***************************************************
        //SSIキーワードを検索して置き換える（再帰処理）
        //***************************************************
        bool SsiConvert(ref string str, Encoding encoding) {
            //分解
            var startIndex = str.IndexOf("<!--#");
            if (startIndex < 0) {
                return true;
            }
            var stopIndex = str.Substring(startIndex).IndexOf("-->");
            if (stopIndex < 0) {
                return false;
            }
            var before = str.Substring(0, startIndex);
            var target = str.Substring(startIndex + 5, stopIndex - 5);
            target = target.Trim();//前後の空白を取り除く
            var after = str.Substring(startIndex + stopIndex + 3);

            var ssiKind = SsiKind.Unknown;
            var param = "";

            if (target.IndexOf("exec") == 0) {
                if (!(bool)_conf.Get("useExec")) {
                    return true;
                }
                ssiKind = SsiKind.Exec;
                param = target.Substring(4);
            } else if (target.IndexOf("config") == 0) {
                ssiKind = SsiKind.Config;
                param = target.Substring(6);
            } else if (target.IndexOf("echo") == 0) {
                ssiKind = SsiKind.Echo;
                param = target.Substring(4);
            } else if (target.IndexOf("flastmod") == 0) {
                ssiKind = SsiKind.Flastmod;
                param = target.Substring(8);
            } else if (target.IndexOf("fsize") == 0) {
                ssiKind = SsiKind.Fsize;
                param = target.Substring(5);
            } else if (target.IndexOf("include") == 0) {
                ssiKind = SsiKind.Include;
                param = target.Substring(7);
            }
            //空白清掃
            param = param.Trim();

            //SSIを実行する
            target = SsiJob(ssiKind, param, encoding);

            //再結合
            str = before + target + after;

            return SsiConvert(ref str, encoding);
        }

        //***************************************************
        //SSIを実行する
        //***************************************************
        string SsiJob(SsiKind ssiKind, string param, Encoding encoding) {
            var tmp = param.Split(new[] { '=' }, 2);
            if (tmp.Length != 2) {
                _logger.Set(LogKind.Secure, null, 20, string.Format("param {0}", param));//"パラメータの解釈に失敗しました"
                return "";
            }

            var tag = tmp[0];
            var val = tmp[1].Trim('"');
            var ret = false;
            var str = "";

            switch (ssiKind) {
                case SsiKind.Exec:
                    ret = SsiExec(tag, val, ref str, encoding, _sockTcp);
                    break;
                case SsiKind.Echo:
                    ret = SsiEcho(tag, val, ref str);
                    break;
                case SsiKind.Config:
                    ret = SsiConfig(tag, val);
                    break;
                case SsiKind.Fsize:
                    ret = SsiFsize(tag, val, ref str);
                    break;
                case SsiKind.Flastmod:
                    ret = SsiFlastmod(tag, val, ref str);
                    break;
                case SsiKind.Include:
                    ret = SsiInclude(tag, val, ref str, encoding);
                    break;
            }

            if (!ret) {
                _logger.Set(LogKind.Secure, null, 21, string.Format("{0}=\"{1}\"", tag, val));
                return "";
            }
            _logger.Set(LogKind.Detail, null, 17, string.Format("{0} {1} -> {2}", ssiKind, param, str));//"exec SSI

//            //Ver5.9.1 CGI出力だけ、ヘッダ処理する
//            if (ssiKind != SsiKind.Include){
//                //Ver5.4.8
//                //SSI用のCGI出力からヘッダ情報を削除する
//                var lines = str.Split('\n').ToList();
//                var index = lines.IndexOf("\r");
//                if (index != -1) {
//                    var sb = new StringBuilder();
//                    for (int i = index + 1; i < lines.Count(); i++) {
//                        sb.Append(lines[i] + "\n");
//                    }
//                    str = sb.ToString();
//                }
//            }
            return str;
        }

        //プログラム実行
        bool SsiExec(string tag, string val, ref string str, Encoding encoding, SockTcp tcpObj) {
            Target newTarget;
            var param = "";
            if (tag.ToLower() == "cmd") {
                param = val;
                newTarget = CreateTarget("comspec", null);
                if (newTarget == null) {
                    return false;
                }
            } else if (tag.ToLower() == "cgi") {
                var cmd = val;
                var tmp = val.Split(new[] { ' ' }, 2);
                if (tmp.Length == 2) {
                    cmd = tmp[0];
                    param = tmp[1];
                }
                newTarget = CreateTarget("file", cmd);
                if (newTarget == null) {
                    return false;
                }
                if (newTarget.TargetKind != TargetKind.Cgi) {
                    _logger.Set(LogKind.Error, tcpObj, 27, string.Format("<!--#exec cgi=\"{0}\"-->", val));
                    return false;
                }
            } else { // cmd="" cgi="" 以外の場合はエラー
                _logger.Set(LogKind.Error, tcpObj, 28, "");
                return false;
            }

            var cgi = new Cgi();
            //TODO 変数削除リファクタリング対象
            //IPAddress remoteAddress = tcpObj.RemoteEndPoint.Address;
            //string remoteHost = tcpObj.RemoteHost;
            //環境変数作成
            //Env env = new Env(kernel, request, recvHeader, remoteAddress, remoteHost, newTarget.FullPath);
            var env = new Env(_kernel,_conf, _request, _recvHeader, tcpObj, newTarget.FullPath);
            WebStream output;//標準出力
            const string err = "";
            var cgiTimeout = (int)_conf.Get("cgiTimeout");
            if (!cgi.Exec(newTarget, param, env, null, out output,cgiTimeout)) {
                str = err;
            } else{
                var b = new byte[output.Length];
                output.Read(b, 0, b.Length);   
                str = encoding.GetString(b);
            }
            return true;
        }

        //ファイルの更新日時
        bool SsiFlastmod(string tag, string val, ref string str) {
            var newTarget = CreateTarget(tag, val);
            if (newTarget == null)
                return false;
            if (newTarget.FileInfo == null)
                return false;
            str = newTarget.FileInfo.LastWriteTime.ToString(_timeFmt);
            return true;
        }

        //ファイルのインクルード
        bool SsiInclude(string tag, string val, ref string str, Encoding encoding) {
            var newTarget = CreateTarget(tag, val);
            if (newTarget == null)
                return false;

            //ループに陥るため、自分自身はインクルードできない
            if (_target.FullPath == newTarget.FullPath) {
                _logger.Set(LogKind.Error, null, 15, string.Format("{0}", newTarget.FullPath));
                return false;
            }

            if (newTarget.TargetKind == TargetKind.Cgi) {
                var cgi = new Cgi();
                //TODO 変数削除 リファクタリング対象
                //IPAddress remoteAddress = tcpObj.RemoteEndPoint.Address;
                //string remoteHostName = tcpObj.RemoteHost;
                //環境変数作成
                //Ver5.6.2
                //Env env = new Env(kernel, request, recvHeader, remoteAddress, remoteHostName, newTarget.FullPath);
                var env = new Env(_kernel, _conf,_request, _recvHeader,_sockTcp,newTarget.FullPath);
                const string param = "";
                WebStream output;
                var cgiTimeout = (int)_conf.Get("cgiTimeout");
                cgi.Exec(newTarget, param, env,null, out output, cgiTimeout);
                str = Encoding.ASCII.GetString(output.GetBytes());

                //Ver5.9.1 CGI出力は、ヘッダをカットする
                var lines = str.Split('\n').ToList();
                var index = lines.IndexOf("\r");
                if (index != -1) {
                    var sb = new StringBuilder();
                    for (int i = index + 1; i < lines.Count(); i++) {
                        sb.Append(lines[i] + "\n");
                    }
                    str = sb.ToString();
                }

            } else if (newTarget.TargetKind == TargetKind.File || newTarget.TargetKind == TargetKind.Ssi) {
                if (File.Exists(newTarget.FullPath)) {
                    using (var sr = new StreamReader(newTarget.FullPath, encoding)) {
                        str = sr.ReadToEnd();
                        sr.Close();
                    }
                } else {
                    return false;
                }
            } else {
                return false;
            }
            return true;
        }

        bool SsiConfig(string tag, string val) {
            if (tag == "timefmt") {
                SetTimeFmt(val);//日付書式文字列(timeFmt)の変更
            } else if (tag == "sizefmt") {
                _sizeFmt = val == "abbrev" ? val : "bytes";
            } else {
                return false;
            }
            return true;
        }

        //ファイルのサイズ
        bool SsiFsize(string tag, string val, ref string str) {
            Target newTarget = CreateTarget(tag, val);
            if (newTarget == null)
                return false;
            long size = 0;
            if (newTarget.FileInfo != null) {
                size = newTarget.FileInfo.Length;
            }
            if (_sizeFmt == "bytes") { //バイト単位固定
                str = string.Format("{0}", size);
            } else {
                if (size < 1000) { //1000以下の場合は、バイト単位
                    str = string.Format("{0}", size);
                } else if (size > 1024 * 1024) { //Mbyte単位
                    str = string.Format("{0}M", size / (1024 * 1024));
                } else { //Kbyte単位
                    str = string.Format("{0}K", size / 1024);
                }
            }
            return true;
        }

        bool SsiEcho(string tag, string val, ref string str) {
            if (tag != "var") {
                return false;
            }
            switch (val) {
                case "LAST_MODIFIED":
                    str = _target.FileInfo.LastWriteTime.ToString(_timeFmt);
                    break;
                case "DATE_GMT":
                    str = DateTime.UtcNow.ToString(_timeFmt);
                    break;
                case "DATE_LOCAL":
                    str = DateTime.Now.ToString(_timeFmt);
                    break;
                case "DOCUMENT_NAME":
                    str = Path.GetFileName(_target.FullPath);
                    break;
                //Ver5.6.0
                //case "DOCUMENT_URL":
                case "DOCUMENT_URI":
                    str = _target.FullPath;
                    break;
                //Ver5.6.0 未実装
                case "QUERY_STRING_UNESCAPED":
                    str = "???";
                    break;
                default:
                    return false;
            }
            return true;
        }

        Target CreateTarget(string tag, string val) {
            var newTarget = new Target(_conf, _logger);
            if (tag == "file") {
                //現在のドキュメンのフルパスからからファイル名を生成する
                string fullPath = Path.GetDirectoryName(_target.FullPath) + "\\" + val;
                newTarget.InitFromFile(fullPath);
            } else if (tag == "virtual") {
                newTarget.InitFromUri(val);
            } else if (tag == "comspec") {
                string fullPath = Path.GetDirectoryName(_target.FullPath) + "\\";
                newTarget.InitFromCmd(fullPath);
            } else {
                return null;
            }
            return newTarget;
        }

        //***************************************************
        // <!--config timefmt="--" で送られた日付書式を変換して
        // 内部処理変数 timeFmtにセットする
        //***************************************************
        void SetTimeFmt(string str) {
            var sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++) {
                char c = str[i];
                if (c == '%') {
                    i++;
                    c = str[i];
                    switch (c) {
                        case 'c':
                            //sb.Append("MM/dd/yy hh/mm/ss");//10/30/97 11:22:33(月/日/年 時:分:秒)
                            sb.Append("MM/dd/yy hh:mm:ss");//10/30/97 11:22:33(月/日/年 時:分:秒)
                            break;
                        case 'x':
                            sb.Append("MM/dd/yy");//10/30/97 (月/日/年) 
                            break;
                        case 'X':
                            //sb.Append("hh/mm/ss");//11:22:33 (時:分:秒)
                            sb.Append("hh:mm:ss");//11:22:33 (時:分:秒)
                            break;
                        case 'y':
                            sb.Append("yy");//97 年(2桁) 
                            break;
                        case 'Y':
                            sb.Append("yyyy");//1997 年(4桁) 
                            break;
                        case 'b':
                            sb.Append("MMM");//Oct 月(3文字) 
                            break;
                        case 'B':
                            sb.Append("MMMM");//October 月(フルスペル) 
                            break;
                        case 'm':
                            sb.Append("MM");//08 月(2桁) 
                            break;
                        case 'a':
                            sb.Append("ddd");//Sat 曜日(3文字) 
                            break;
                        case 'A':
                            sb.Append("dddd");//Saturday 曜日(フルスペル) 
                            break;
                        case 'd':
                            sb.Append("dd");//30 日(2桁) 
                            break;
                        case 'j':
                            sb.Append("???");//223 1月1日からの日数【未対応】
                            break;
                        case 'w':
                            sb.Append("???");// 6 日曜日からの日数 【未対応】
                            break;
                        case 'p':
                            sb.Append("tt");//PM AMもしくはPM 
                            break;
                        case 'H':
                            sb.Append("HH");//23 時(24時間制) 
                            break;
                        case 'I':
                            sb.Append("hh");//11 時(12時間制) 
                            break;
                        case 'M':
                            sb.Append("mm");//44 分 
                            break;
                        case 'S':
                            sb.Append("ss");//56 秒 
                            break;
                        case 'Z':
                            sb.Append("%K");//JST タイムゾーン 
                            break;
                    }
                } else {
                    sb.Append(c);
                }
                _timeFmt = sb.ToString();
            }
        }
    }
}
