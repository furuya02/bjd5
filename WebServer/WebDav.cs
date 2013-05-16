using System;
using System.IO;
using System.Text;
using System.Xml;
using Bjd.log;
using Bjd.util;

namespace WebServer {
    //**********************************************************************************
    //WebDAV class 1 (ロック機能なし)に準拠
    //
    //RFC 2291「WWWにおける分散オーサリングおよびバージョン管理プロトコルの要件」提唱
    //RFC 2518「分散オーサリングのためのHTTP拡張 --WEBDAV」具体的定義
    //RFC 3253 「WebDAVのバージョニング拡張メソッドやヘッダやリソースタイプのセット」参考
    //**********************************************************************************
    class WebDav {
        //RFC2518(9.2 Depth Header)
        //Depth:0 指定したリソースだけに効果を及ぼす
        //Depth:1 リソースおよびその直下のリソースにメソッドの効果を及ぼす
        //Depth:infinity 配下のリソース全てに効果を及ぼす
        enum Depth {
            Depth0 = 0,
            Depth1 = 1,
            DepthInfinity = 2,
            Null = 3
        }

        Depth _depth = Depth.Null;
        readonly Logger _logger;
        readonly WebDavDb _webDavDb;
        readonly Document _document;
        readonly string _fullPath = "";
        readonly string _hrefHost = "";
        readonly string _hrefUri = "";
        readonly WebDavKind _webDavKind;
        readonly TargetKind _targetKind;
        readonly ContentType _contentType;
        readonly bool _useEtag;

        public WebDav(Logger logger, WebDavDb webDavDb, Target target, Document document, string urlStr, string depthStr, ContentType contentType, bool useEtag) {
            _logger = logger;
            _webDavDb = webDavDb;
            _document = document;
            _webDavKind = target.WebDavKind;
            _targetKind = target.TargetKind;
            _contentType = contentType;
            _useEtag = useEtag;

            if (depthStr != null) {
                if (depthStr == "0") {
                    _depth = Depth.Depth0;
                } else if (depthStr == "1") {
                    _depth = Depth.Depth1;
                } else if (depthStr == "infinity") {
                    _depth = Depth.DepthInfinity;
                }
            }

            _fullPath = target.FullPath;
            _hrefHost = urlStr + target.Uri;
            //hrefをhttp://hostname　と uri部分に分解する
            var index = _hrefHost.IndexOf("://");
            if (index != -1) {
                _hrefUri = _hrefHost.Substring(index + 3);
                var pos = _hrefUri.IndexOf('/');
                if (pos != -1) {
                    _hrefUri = _hrefUri.Substring(pos);
                    _hrefHost = _hrefHost.Substring(0, index + pos + 3);
                }
            }
            if (_hrefUri != "") {
                if (_targetKind == TargetKind.Dir && _hrefUri[_hrefUri.Length - 1] != '/')
                    _hrefUri = _hrefUri + "/";
            }

            //RFC 2518(5.2) コレクションに対するリクエストで最後に/(スラッシュ)なし
            //で参照されるとき自動的にこれを付加して処理することができる
            //この際、Content-Locationで見なしたURLをクライアントに返すべき
            //document.AddHeader("Content-Location",hrefHost+Util.SwapStr("%2f","/",HttpUtility.UrlEncode(hrefUri)));
            //document.AddHeader("Content-Location",HttpUtility.UrlPathEncode(hrefHost + hrefUri));

            var href = Uri.EscapeDataString(_hrefUri);

            //Ver5.4.6
            href = Util.SwapStr("%2F", "/", href);

            href = Util.SwapStr("%2f", "/", href);
            href = Util.SwapStr("+", "%20", href);
            document.AddHeader("Content-Location", _hrefHost + href);
        }

        public static bool IsTarget(HttpMethod httpMethod) {
            switch (httpMethod) {
                case HttpMethod.Options:
                case HttpMethod.Delete:
                case HttpMethod.Put:
                case HttpMethod.Propfind:
                case HttpMethod.Proppatch:
                case HttpMethod.Mkcol:
                case HttpMethod.Move:
                case HttpMethod.Copy:
                    return true;
            }
            return false;
        }

        //階層下リソースのプロパティ値の取得
        void FindAll(PropFindResponce propFindResponce, Depth depth, string hrefHost, string hrefUri, string path, bool useEtag) {
            if (hrefUri.Length > 1 && hrefUri[hrefUri.Length - 1] != '/') {
                hrefUri = hrefUri + "/";
            }
            var di = new DirectoryInfo(path);
            var isCollection = true;
            foreach (DirectoryInfo info in di.GetDirectories("*.*")) {
                propFindResponce.Add(isCollection,
                    info.Name,
                    hrefHost,
                    hrefUri + info.Name + "/",
                    "", //contentType
                    "", //etag
                    0, //Directoryのサイズは0で初期化する
                    info.CreationTime,
                    info.LastWriteTime);
                if (depth == Depth.DepthInfinity) {
                    //さらに階層下を再帰処理
                    string newPath = path + info.Name;
                    if (path[path.Length - 1] != '\\')
                        newPath = path + "\\" + info.Name;
                    FindAll(propFindResponce, depth, hrefHost, hrefUri + info.Name + "/", newPath, useEtag);
                }
            }
            isCollection = false;
            foreach (FileInfo info in di.GetFiles("*.*")) {
                propFindResponce.Add(isCollection,
                    info.Name,
                    hrefHost,
                    hrefUri + info.Name,
                    _contentType.Get(info.Name),
                    useEtag ? WebServerUtil.Etag(info) : "", //Etag
                    info.Length,
                    info.CreationTime,
                    info.LastWriteTime);
            }
        }

        //PROPFIND
        public int PropFind() {
            if (_webDavKind == WebDavKind.Non)
                return 500;

            if (_targetKind == TargetKind.Non)
                return 404;

            if (_depth == Depth.Null)
                _depth = Depth.DepthInfinity;//指定されない場合は、「infinity」とする RFC2518(8.1 PROPFIND)

            const int responseCode = 207;

            //PROPFINDの情報を蓄積してレスポンスを生成するクラス
            var propFindResponce = new PropFindResponce(_webDavDb);

            //if(target.Kind == TARGET_KIND.DIR) {//１コレクションのプロパテイ値の取得
            if (_targetKind == TargetKind.Dir) { //１コレクションのプロパテイ値の取得
                const bool isCollection = true; //コレクション
                var di = new DirectoryInfo(_fullPath);
                propFindResponce.Add(isCollection,
                    di.Name,
                    _hrefHost,
                    _hrefUri,
                    "", //contentType
                    "", //etag
                    0, //Directoryのサイズは0で初期化する
                    di.CreationTime,
                    di.LastWriteTime);
                if (_depth != Depth.Depth0) {
                    //直下のリソースのプロパテイ値の取得
                    FindAll(propFindResponce, _depth, _hrefHost, _hrefUri, _fullPath, _useEtag);
                }
            } else { //１リソースのプロパテイ値の取得
                const bool isCollection = false; //非コレクション
                var info = new FileInfo(_fullPath);
                propFindResponce.Add(isCollection,
                    info.Name,
                    _hrefHost,
                    _hrefUri,
                    _contentType.Get(_fullPath),
                    _useEtag ? WebServerUtil.Etag(info) : "",
                    info.Length,
                    info.CreationTime,
                    info.LastWriteTime);
            }
            //レスポンス作成
            _document.CreateFromXml(propFindResponce.ToString());
            return responseCode;
        }

        //PROPPATCH
        public int PropPatch(byte[] input) {
            if (_targetKind == TargetKind.Non)
                return 404;

            int responseCode = 500;
            if (_webDavKind == WebDavKind.Write) {
                responseCode = 207;

                var propPatchResponce = new PropPatchResponce(_hrefUri);

                //設定値の読み込み
                var doc = new XmlDocument();
                doc.LoadXml(Encoding.ASCII.GetString(input));
                //set
                foreach (XmlNode nodeSet in doc.GetElementsByTagName("D:set")) {
                    foreach (XmlNode nodeProp in nodeSet.ChildNodes) {
                        if (nodeProp.LocalName.ToLower() == "prop") {
                            foreach (XmlNode nodeTarget in nodeProp.ChildNodes) {
                                var nameSpace = nodeTarget.NamespaceURI;
                                var name = nodeTarget.LocalName;
                                var value = nodeTarget.InnerText;
                                var responceCode = 200;
                                //if(targetKind == TARGET_KIND.FILE && nameSpace != "DAV:"){
                                if ((_targetKind == TargetKind.File || _targetKind == TargetKind.Dir) && nameSpace != "DAV:") {
                                    _webDavDb.Set(_hrefUri, nameSpace, name, value);//DB更新処理
                                } else {
                                    responceCode = 409;
                                }
                                propPatchResponce.Add(nameSpace, name, responceCode);
                            }
                        }
                    }
                }
                //propertyupdate
                foreach (XmlNode nodeSet in doc.GetElementsByTagName("D:propertyupdate")) {
                    foreach (XmlNode nodeProp in nodeSet.ChildNodes) {
                        if (nodeProp.LocalName.ToLower() == "prop") {
                            foreach (XmlNode nodeTarget in nodeProp.ChildNodes) {
                                var nameSpace = nodeTarget.NamespaceURI;
                                var name = nodeTarget.LocalName;
                                var value = nodeTarget.InnerText;
                                responseCode = 200;
                                //if(targetKind == TARGET_KIND.FILE && nameSpace != "DAV:") {
                                if ((_targetKind == TargetKind.File || _targetKind == TargetKind.Dir) && nameSpace != "DAV:") {
                                    _webDavDb.Set(_hrefUri, nameSpace, name, value);//DB更新処理
                                } else {
                                    responseCode = 409;
                                }
                                propPatchResponce.Add(nameSpace, name, responseCode);
                            }
                        }
                    }
                }
                //remove
                foreach (XmlNode nodeSet in doc.GetElementsByTagName("D:remove")) {
                    foreach (XmlNode nodeProp in nodeSet.ChildNodes) {
                        if (nodeProp.LocalName.ToLower() == "prop") {
                            foreach (XmlNode nodeTarget in nodeProp.ChildNodes) {
                                var nameSpace = nodeTarget.NamespaceURI;
                                var name = nodeTarget.LocalName;
                                responseCode = 200;
                                //if(targetKind == TARGET_KIND.FILE && nameSpace != "DAV:") {
                                if ((_targetKind == TargetKind.File || _targetKind == TargetKind.Dir) && nameSpace != "DAV:") {
                                    _webDavDb.Remove(_hrefUri, nameSpace, name);//DB更新処理
                                } else {
                                    responseCode = 409;
                                }
                                propPatchResponce.Add(nameSpace, name, responseCode);
                            }
                        }
                    }
                }

                //レスポンス作成
                _document.CreateFromXml(propPatchResponce.ToString());
            }
            return responseCode;
        }

        //OPTION 処理
        public int Option() {
            const int responseCode = 200;
            var sb = new StringBuilder();
            sb.Append("GET, POST, HEAD, OPTIONS");//通常ディレクトリ
            if (_webDavKind != WebDavKind.Non) { //WebDAV対象ディレクトリ
                sb.Append(", PROPFIND");
                if (_webDavKind == WebDavKind.Write) { //WebDAV書き込み可の場合
                    sb.Append(", PROPATCH, PUT, DELETE, COPY, MOVE, MKCOL");
                }
                //RFC 2518(5.2) OptiopnでDavヘッダを返さなければならない
                _document.AddHeader("Dav", "1");

                //WindowsXPで80以外のポートでアクセスしたときに使用される
                //下記のWebDAVクライアントでは、このヘッダが無いと動作しない
                //User-Agent: Microsoft Data Access Internet Publishing Provider Protocol Discovery
                _document.AddHeader("MS-Author-Via", "DAV");
            }
            _document.AddHeader("Allow", sb.ToString());
            return responseCode;
        }

        //DELETE
        public int Delete() {
            if (_targetKind == TargetKind.Dir || _targetKind == TargetKind.Move) {
                _depth = Depth.DepthInfinity;//コレクションに対するDELETE では「infinity」が使用されているように動作しなければならない RFC2518(8.6.2)
            }
            if (_depth == Depth.Null)
                _depth = Depth.DepthInfinity;//指定されない場合は、「infinity」とする RFC2518(9.2)

            int responseCode = 405;
            if (_webDavKind == WebDavKind.Write) {
                _webDavDb.Remove(_hrefUri);//データベース削除

                if (Directory.Exists(_fullPath)) {
                    try {
                        //ディレクトリの削除
                        RemoveDirectory(_hrefUri, _fullPath, true);
                        responseCode = 204;//No Content
                    } catch (Exception ex) {
                        _logger.Set(LogKind.Error, null, 29, ex.Message);
                        responseCode = 500;//ERROR
                    }
                } else if (File.Exists(_fullPath)) {
                    try {
                        responseCode = RemoveFile(_hrefUri, _fullPath) ? 204 : 500;
                    } catch (Exception ex) {
                        _logger.Set(LogKind.Error, null, 30, ex.Message);
                        responseCode = 500;//ERROR
                    }
                } else {
                    responseCode = 404;//Not Found
                }
            }
            return responseCode;
        }

        //PUT
        public int Put(byte[] input) {
            var responseCode = 405;
            if (_webDavKind == WebDavKind.Write) {
                _webDavDb.Remove(_hrefUri);//データベース削除

                //if(File.Exists(fullPath)) {
                //    responseCode = 204;//No Content
                //} else {
                responseCode = 201;//Created
                //}
                try {
                    //ファイルの作成
                    using (var writer = new FileStream(_fullPath, FileMode.Create, FileAccess.ReadWrite)) {
                        if (input != null)
                            writer.Write(input, 0, input.Length);
                        writer.Close();
                    }
                } catch (Exception ex) {
                    _logger.Set(LogKind.Error, null, 31, ex.Message);
                    responseCode = 500;//ERROR
                }
            }
            return responseCode;
        }

        //MOVE.COPY
        public int MoveCopy(Target destTarget, bool overwrite, HttpMethod httpMethod) {
            int responseCode = 405;
            if (_targetKind == TargetKind.Dir) {
                _depth = Depth.DepthInfinity;//コレクションに対するMOVE では「infinity」が使用されているように動作しなければならない RFC2518(8.9.2)
            }
            if (_depth == Depth.Null)
                _depth = Depth.DepthInfinity;//指定されない場合は、「infinity」とする RFC2518(9.2)

            if (Directory.Exists(_fullPath)) {
                try {
                    responseCode = 201;
                    if (overwrite) {
                        if (Directory.Exists(destTarget.FullPath)) {
                            responseCode = 204;
                            try {
                                //ディレクトリの削除
                                RemoveDirectory(destTarget.Uri, destTarget.FullPath, false);
                            } catch (Exception ex) {
                                _logger.Set(LogKind.Error, null, 32, ex.Message);
                            }
                        }
                    }
                    if (Directory.Exists(destTarget.FullPath)) { //対象が存在する場合は、エラーとなる
                        responseCode = 403;
                    } else {
                        //ディレクトリのコピー
                        if (CopyDirectory(_hrefUri, _fullPath, destTarget.Uri, destTarget.FullPath)) {
                            if (httpMethod == HttpMethod.Move) {
                                //元ディレクトリの削除
                                RemoveDirectory(_hrefUri, _fullPath, true);
                            }
                        } else {
                            responseCode = 403;
                        }
                    }
                } catch (Exception ex) {
                    _logger.Set(LogKind.Error, null, 34, ex.Message);
                    responseCode = 500;
                }
            } else if (File.Exists(_fullPath)) {
                try {
                    responseCode = 201;
                    if (overwrite) {
                        if (File.Exists(destTarget.FullPath)) {
                            responseCode = 204;
                            try {
                                RemoveFile(destTarget.Uri, destTarget.FullPath);//ファイルの削除
                            } catch (Exception ex) {
                                _logger.Set(LogKind.Error, null, 33, ex.Message);
                            }
                        }
                    }
                    if (File.Exists(destTarget.FullPath)) { //対象が存在する場合は、エラーとなる
                        responseCode = 403;
                    } else {
                        //ファイルのコピー
                        if (CopyFile(_hrefUri, _fullPath, destTarget.Uri, destTarget.FullPath)) {
                            if (httpMethod == HttpMethod.Move) {
                                //元ファイルの削除
                                RemoveFile(_hrefUri, _fullPath);
                            }
                        } else {
                            responseCode = 403;
                        }
                    }
                } catch (Exception ex) {
                    _logger.Set(LogKind.Error, null, 35, ex.Message);
                    responseCode = 500;
                }
            }
            return responseCode;
        }

        //ディレクトリの削除
        bool RemoveDirectory(string uri, string path, bool recursive) {
            if (!recursive) { //階層下を削除しない場合
                try {
                    Directory.Delete(path);
                    _webDavDb.Remove(uri);
                } catch {
                    return false;
                }
            } else { //階層下も削除する場合
                foreach (var dir in Directory.GetDirectories(path)) {
                    var name = dir.Substring(path.Length);
                    var nextUri = uri + name + "/";
                    var nextPath = path + name + "\\";
                    if (!RemoveDirectory(nextUri, nextPath, recursive))
                        return false;
                }
                foreach (var file in Directory.GetFiles(path)) {
                    if (!RemoveFile(uri + file.Substring(path.Length), file))
                        return false;
                }
                Directory.Delete(path);
                _webDavDb.Remove(uri);
            }
            return true;
        }

        //ディレクトリのコピー
        bool CopyDirectory(string srcUri, string srcPath, string dstUri, string dstPath) {
            Directory.CreateDirectory(dstPath);
            File.SetAttributes(dstPath, File.GetAttributes(srcPath));

            foreach (var dir in Directory.GetDirectories(srcPath)) {
                var name = dir.Substring(srcPath.Length);
                var nextSrcUri = srcUri + name + "/";
                var nextSrcPath = srcPath + name + "\\";
                var nextDstUri = dstUri + name + "/";
                var nextDstPath = dstPath + name + "\\";
                if (!CopyDirectory(nextSrcUri, nextSrcPath, nextDstUri, nextDstPath))
                    return false;
            }
            foreach (var file in Directory.GetFiles(srcPath)) {
                var name = file.Substring(srcPath.Length);
                var nextSrcUri = srcUri + name;
                var nextSrcPath = srcPath + name;
                var nextDstUri = dstUri + name;
                var nextDstPath = dstPath + name;
                if (!CopyFile(nextSrcUri, nextSrcPath, nextDstUri, nextDstPath))
                    return false;
            }
            return true;
        }

        //ファイルのコピー
        bool CopyFile(string srcUri, string srcPath, string dstUri, string dstPath) {
            try {
                File.Copy(srcPath, dstPath);
                //プロパティのコピー
                var list = _webDavDb.Get(srcUri);
                foreach (var o in list) {
                    _webDavDb.Set(dstUri, o.NameSpace, o.Name, o.Value);
                }
            } catch {
                return false;
            }
            return true;
        }

        //ファイルの削除
        bool RemoveFile(string uri, string path) {
            try {
                File.Delete(path);//ファイルの削除
                _webDavDb.Remove(uri);//プロパティの削除
            } catch {
                return false;
            }
            return true;
        }


        //MKCOL
        public int MkCol() {
            int responseCode = 405;
            if (_webDavKind == WebDavKind.Write) {
                //親ディレクトリの存在確認
                if (_fullPath.Length > 0) {
                    var dir = Path.GetDirectoryName(_fullPath.Substring(0, _fullPath.Length - 1));
                    if (dir!=null && !Directory.Exists(dir)) {
                        responseCode = 409;
                    } else if (!Directory.Exists(_fullPath) && !File.Exists(_fullPath)) {
                        try {
                            Directory.CreateDirectory(_fullPath);
                            responseCode = 201;
                        } catch (Exception ex) {
                            _logger.Set(LogKind.Error, null, 36, ex.Message);
                            responseCode = 500;
                        }
                    }
                }
            }
            return responseCode;
        }
    }
}
