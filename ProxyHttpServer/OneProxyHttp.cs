using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyHttpServer {
    class OneProxyHttp : IDisposable {
        readonly Response _response = new Response();
        readonly Dictionary<CS, HttpSideState> _sideState = new Dictionary<CS, HttpSideState>(2);

        long _lastRecvServer = DateTime.Now.Ticks;

        enum Charset {
            Unknown = 0,
            Ascii = 1,
            Sjis = 2,
            Euc = 3,
            Utf8 = 4,
            Utf7 = 5,
            Jis = 6//iso-2022-jp
        }
        bool _isText;//コンテンツ制限の対象かどうかのフラグ
        Charset _charset = Charset.Unknown;
        byte[] _textData = new byte[0];

        public HttpSideState SideState(CS cs) {
            return _sideState[cs];
        }

        //リクエストの接続先ホスト名
        //proxyオブジェクが、現在接続中のホストと、このリクエストの接続先が同一かどうかを確認するためのプロパティ
        //proxyオブジェクトは、既存のプロキシ処理をすべて完了していないと接続先を変更できないため、このオブジェクトのサーバへの接続は待機させられることになる
        public string HostName {
            get {
                return _oneObj.Request.HostName;
            }
        }

        //キャッシュ対象かどうかのフラグ
        bool _isCacheTarget;

        //受信形式
        enum OneHttpKind {
            Unknown,//不明
            ContentLength,//Content-Length形式
            Chunk //chunk形式
        }
        OneHttpKind _oneHttpKind = OneHttpKind.Unknown;

        // OneHttpKind == ONE_HTTP_KIND.CONTENT_LENGTHの場合は、下記の変数でサーバからの受信完了を確認する
        //Ver5.6.1
        //int contentLength = -1;
        long _contentLength = -1;
        // OneHttpKind == ONE_HTTP_KIND.CHUNKの場合は、下記の変数でサーバからの受信を行う
        int _chunkLen = -1;

        readonly Proxy _proxy;
        public ProxyHttp ProxyHttp { get; private set; }
        readonly OneObj _oneObj;

        public void Dispose() {
            _oneObj.Dispose();
        }

        public long WaitTime {
            get {
                return (DateTime.Now.Ticks - _lastRecvServer) / 1000 / 1000 / 10;
            }
        }

        public List<string> DebugLog(int i) {
            var list = new List<string>();
            list.Add(string.Format("[ONE_HTTP][{0}] {1}", i, _oneObj.Request.RequestStr));

            if (_sideState[CS.Client] == HttpSideState.ClientSideSendBody) {
                list.Add("[ONE_HTTP] Finish");
            } else {
                if (_oneHttpKind == OneHttpKind.Chunk) {
                    list.Add(string.Format("[ONE_HTTP][{0}] code={1} KIND={2} sv={3} cl={4} chunkLen={5} pos sv={6} cl={7}", i, _response.Code, _oneHttpKind, SideState(CS.Server), SideState(CS.Client), _chunkLen, _oneObj.Pos[CS.Server], _oneObj.Pos[CS.Client]));
                } else {
                    list.Add(string.Format("[ONE_HTTP][{0}] code={1} KIND={2} sv={3} cl={4} contentLength={5} pos sv={6} cl={7}", i, _response.Code, _oneHttpKind, SideState(CS.Server), SideState(CS.Client), _contentLength, _oneObj.Pos[CS.Server], _oneObj.Pos[CS.Client]));
                }
                list.Add(string.Format("[ONE_HTTP][{0}] buf sv={1} cl={2} ■WaitTime={3}sec", i, _oneObj.Body[CS.Server].Length, _oneObj.Body[CS.Client].Length, WaitTime));
            }
            return list;
        }

        public OneProxyHttp(Proxy proxy, ProxyHttp proxyHttp, OneObj oneObj) {
            _proxy = proxy;
            ProxyHttp = proxyHttp;
            _oneObj = oneObj;

            _sideState[CS.Client] = HttpSideState.ClientSideRecvRequest;
            _sideState[CS.Server] = HttpSideState.Non;
        }

        //キャッシュ確認
        public void CacheWrite(Cache cache) {
            if (!_isCacheTarget)//キャッシュ対象外
                return;
            if (_response.Code != 200)//レスポンスコードが200のものだけが対象
                return;

            //Ver5.6.1
            if (!_oneObj.Body[CS.Server].CanUse)
                return;

            var oneCache = new OneCache(_oneObj.Request.HostName, _oneObj.Request.Port, _oneObj.Request.Uri);
            oneCache.Add(_oneObj.Header[CS.Server], _oneObj.Body[CS.Server].Get());
            cache.Add(oneCache);
        }

        //キャッシュ確認
        public void CacheConform(Cache cache) {
            //キャッシュ対象のリクエストかどうかの確認
            if (!_oneObj.Request.Cgi) {
                if (cache.IsTarget(_oneObj.Request.HostName, _oneObj.Request.Uri, _oneObj.Request.Ext)) {
                    _isCacheTarget = true;

                    // Pragma: no-cache が指定されている場合は、蓄積されたキャッシュを否定する
                    var pragmaStr = _oneObj.Header[CS.Client].GetVal("Pragma");
                    
                    if (pragmaStr != null && pragmaStr.ToLower().IndexOf("no-cache") >= 0) {
                        _proxy.Logger.Set(LogKind.Detail, null, 16, _oneObj.Request.Uri);
                        cache.Remove(_oneObj.Request.HostName, _oneObj.Request.Port, _oneObj.Request.Uri);//存在する場合は、無効化する
                    } else {
                        string modifiedStr = _oneObj.Header[CS.Client].GetVal("If-Modified-Since");
                        DateTime modified = Util.Str2Time(modifiedStr);
                        OneCache oneCache = cache.Get(_oneObj.Request, modified);
                        if (oneCache != null) { //キャッシュが見つかった場合
                            _proxy.Logger.Set(LogKind.Detail, null, 14, _oneObj.Request.Uri);

                            _sideState[CS.Server] = HttpSideState.ServerSideRecvBody;//一気に受信完了
                            _sideState[CS.Client] = HttpSideState.ClientSideRecvRequest;//リクエスト受信完了まで進める

                            _response.Recv("HTTP/1.1 200 OK");
                            _oneObj.Header[CS.Server] = new Header(oneCache.Header);
                            _oneObj.Body[CS.Server].Set(new byte[oneCache.Body.Length]);
                            
                            //Buffer.BlockCopy(oneCache.Body, 0, oneObj.Body[CS.Server], 0, oneCache.Body.Length);
                            _oneObj.Body[CS.Server].Set(oneCache.Body);
                            
                            _proxy.NoConnect(_oneObj.Request.HostName, _oneObj.Request.Port);

                            //擬似的にContentLength形式で処理する
                            _oneHttpKind = OneHttpKind.ContentLength;
                            _contentLength = _oneObj.Body[CS.Server].Length;

                            //キャッシュによる返答（このオブジェクトはキャッシュしない）
                            _isCacheTarget = false;
                        }
                    }
                }
            }
        }

        public bool SendServer(ILife iLife) {
            //処置なし
            if (_sideState[CS.Server] != HttpSideState.Non && _sideState[CS.Server] != HttpSideState.ServerSideSendHeader)
                return true;

            //サーバ側との接続処理
            if (!_proxy.Connect(iLife, _oneObj.Request.HostName, _oneObj.Request.Port, _oneObj.Request.RequestStr, _oneObj.Request.Protocol)) {
                _proxy.Logger.Set(LogKind.Debug, null, 999, "□Break http.Connect()==false");
                return false;
            }

            //ヘッダ送信
            var sendBuf = new byte[0];
            if (_sideState[CS.Server] == HttpSideState.Non) {
                if (_proxy.UpperProxy.Use) {
                    if (_proxy.UpperProxy.UseAuth) {
                        var s = string.Format("{0}:{1}", _proxy.UpperProxy.AuthUser, _proxy.UpperProxy.AuthPass);
                        s = string.Format("Basic {0}\r\n", Base64.Encode(s));
                        _oneObj.Header[CS.Client].Append("Proxy-Authorization", Encoding.ASCII.GetBytes(s));
                    }
                }

                
                if (_oneObj.Request.Protocol == ProxyProtocol.Ssl) {
                    if (!_proxy.UpperProxy.Use) {
                        //取得したリクエストをバッファに格納する
                        //sendBuf = new byte[0];
                        //sendBuf[CS.CLIENT] = Bytes.Create("HTTP/1.0 200 Connection established\r\n\r\n");//CONNECTが成功したことをクライアントに返す
                    } else {
                        //上位プロキシを使用する場合(リクエストラインはそのまま使用される)
                        sendBuf = Bytes.Create(_oneObj.Request.SendLine(_proxy.UpperProxy.Use), _oneObj.Header[CS.Client].GetBytes());
                    }
                } else if (_oneObj.Request.Protocol == ProxyProtocol.Http) { //HTTPの場合
                    //Ver5.4.4
                    //"Proxy-Connection"ヘッダは,"Connection"ヘッダに変換する
                    var s = _oneObj.Header[CS.Client].GetVal("Proxy-Connection");
                    if (s != null) { //ヘッダが存在する場合
                        _oneObj.Header[CS.Client].Replace("Proxy-Connection", "Connection", s);
                    }

                    //header.Remove("Proxy-Connection");//＜＝■これ入れていいのか？

                    //取得したリクエストをバッファに格納する
                    //上位プロキシを使用する場合(リクエストラインはそのまま使用される)
                    sendBuf = Bytes.Create(_oneObj.Request.SendLine(_proxy.UpperProxy.Use), _oneObj.Header[CS.Client].GetBytes());

                }
                
                if (!Send(_proxy.Sock(CS.Server), sendBuf))//送信
                    return false;
                _sideState[CS.Server] = HttpSideState.ServerSideSendHeader;
            }

            if (_sideState[CS.Server] == HttpSideState.ServerSideSendHeader) {
                //バッファに残っているデータの送信
                if (!SendBuf(CS.Client))
                    return false;
            }

            //サーバへの送信完了を確認する（ステータス変更）
            _sideState[CS.Server] = HttpSideState.ServerSideSendBody;

            return true;
        }

        public bool RecvServer(ILife iLife) {
            //処置なし
            if (_sideState[CS.Server] == HttpSideState.ServerSideRecvBody)
                return true;

            //int timeout=3;
            //レスポンス・ヘッダの受信
            if (_sideState[CS.Server] == HttpSideState.ServerSideSendBody) {
                //Ver5.0.5
                //int c = proxy.OptionTimeout; //本当は、OptionTimeout*10　だけど、最初のレスポンスがあまりに遅いとプログラムがロックするので10分の１に設定する
                var c = _proxy.OptionTimeout * 10;
                while (iLife.IsLife() && _proxy.Sock(CS.Server).SockState == SockState.Connect && _proxy.Sock(CS.Client).SockState == SockState.Connect && _proxy.Sock(CS.Server).Length() == 0) {
                    Thread.Sleep(100);
                    c--;
                    if (c < 0)
                        return false;//レスポンスが遅い場合、あまり待ちすぎると処理が止まってしまうので、エラーとする
                }
                //レスポンスの取得
                //int len = proxy.Sock(CS.SERVER).Length();
                if (!_response.Recv(_proxy.Logger, _proxy.Sock(CS.Server), _proxy.OptionTimeout, iLife)) {
                    _proxy.Logger.Set(LogKind.Error, _proxy.Sock(CS.Server), 6, "");
                    return false;
                }
                //ヘッダの受信
                if (!_oneObj.Header[CS.Server].Recv(_proxy.Sock(CS.Server), _proxy.OptionTimeout, iLife)) {
                    _proxy.Logger.Set(LogKind.Error, _proxy.Sock(CS.Server), 7, "");
                    return false;
                }

                //データ転送形式の判別
                if (_oneObj.Request.HttpMethod == HttpMethod.Head) {
                    _oneHttpKind = OneHttpKind.ContentLength;
                    _contentLength = 0;
                }
                if (_oneHttpKind == OneHttpKind.Unknown) {
                    string strTransferEncoding = _oneObj.Header[CS.Server].GetVal("Transfer-Encoding");
                    if (strTransferEncoding != null) {
                        if (strTransferEncoding == "chunked")
                            _oneHttpKind = OneHttpKind.Chunk;
                    }
                }
                if (_oneHttpKind == OneHttpKind.Unknown) {
                    string strContentLength = _oneObj.Header[CS.Server].GetVal("Content-Length");
                    if (strContentLength != null) {
                        //Ver5.3.3
                        //contentLength = Convert.ToInt32(strContentLength);
                        //oneHttpKind = ONE_HTTP_KIND.CONTENT_LENGTH;
                        //Ver5.6.1
                        //int i;
                        //if (Int32.TryParse(strContentLength, out i)) {
                        long i;
                        if (Int64.TryParse(strContentLength, out i)) {
                            _contentLength = i;
                            _oneHttpKind = OneHttpKind.ContentLength;
                        }
                    } else {
                        if (_response.Code != 200) {
                            _oneHttpKind = OneHttpKind.ContentLength;
                            _contentLength = 0;
                        }
                    }
                }

                //コンテンツ制限の対象かどうかのフラグを設定する
                if (ProxyHttp.LimitString != null) { //コンテンツ制限に文字列が設定されている場合
                    string contentType = _oneObj.Header[CS.Server].GetVal("Content-Type");
                    if (contentType != null) {
                        if (contentType.ToLower().IndexOf("text/h") == 0) {
                            _isText = true;
                        }
                        if (contentType.ToLower().IndexOf("text/t") == 0) {
                            _isText = true;
                        }
                    }
                    //Content-Encoding:gzipが指定された場合は、テキスト扱いしない
                    if (_isText) {
                        string contentEncoding = _oneObj.Header[CS.Server].GetVal("Content-Encoding");
                        if (contentEncoding != null) {
                            if (contentEncoding.ToUpper().IndexOf("gzip") != -1) {
                                _isText = false;
                            }
                        }
                    }
                }

                _sideState[CS.Server] = HttpSideState.ServerSideRecvHeader;//ヘッダ受信完了

                CheckCharset(_oneObj.Header[CS.Server].GetBytes());//キャラクタセットのチェック

                _lastRecvServer = DateTime.Now.Ticks;
            }

            //データ本体の受信
            if (_oneHttpKind == OneHttpKind.Chunk) { //チャンク形式の場合
                //チャンク形式の受信
                if (!RecvServerChunk(iLife))
                    return false;
            } else { //Content-Length形式の受信
                if (!RecvServerContentLength(iLife))
                    return false;
            }

            //受信完了の確認
            if (_oneHttpKind == OneHttpKind.ContentLength) {
                if (_contentLength <= _oneObj.Body[CS.Server].Length){
                    //_sideState[CS.Server] = HttpSideState.ServerSideRecvBody;//受信完了
                    SetServerSideBody();//Ver5.7.2
                } else {
                    //データが未到着の場合は、しばらく他のスレッドを優先する
                    //while(life && proxy.Sock(CS.SERVER).Length() == 0)
                    //    Thread.Sleep(100);
                    
                    //Ver5.6.1 2012.05.05 速度向上
                    //for (int i = 0; i < 100 && life; i++)
                    //    Thread.Sleep(10);
                    Thread.Sleep(1);
                }
            }
            if (_proxy.Sock(CS.Server).SockState == Bjd.sock.SockState.Error && _proxy.Sock(CS.Server).Length() == 0) {
                //サーバ側が切断されており、取得できるデータが残っていないときは、常に受信完了とする
                _sideState[CS.Server] = HttpSideState.ServerSideRecvBody;//受信完了
            }
            return true;
        }

        //チャンク形式の受信(RecvServer()から使用される)
        bool RecvServerChunk(ILife iLife) {
            var len = _proxy.Sock(CS.Server).Length();
            if (len <= 0)
                return true;
            while (iLife.IsLife()) {
                byte[] b;
                if (_chunkLen == -1) { //チャンクサイズの取得
                    //サイズ取得
                    b = _proxy.Sock(CS.Server).LineRecv(_proxy.OptionTimeout,iLife);
                    if (b == null || b.Length < 2) {
                        _proxy.Logger.Set(LogKind.Debug, null, 999, string.Format("chunk ERROR b==null or b.Lenght<2"));
                        return false;
                    }
                    _oneObj.Body[CS.Server].Add(b);
                    _lastRecvServer = DateTime.Now.Ticks;

                    //サイズ変換
                    string str = Encoding.ASCII.GetString(b, 0, b.Length - 2);
                    if (str == "") {
                        _chunkLen = -1;//次回はサイズ取得
                        continue;
                    }

                    try {
                        _chunkLen = Convert.ToInt32(str.Trim(), 16);
                    } catch {
                        _proxy.Logger.Set(LogKind.Debug, null, 999, string.Format("【例外】Convert.ToInt32(str,16) str=\"{0}\"", str));
                        break;
                    }
                    if (_chunkLen == 0) { //受信完了
                        //残りのデータ(空行)ある場合
                        int l = _proxy.Sock(CS.Server).Length();
                        if (0 < l) {
                            b = _proxy.Sock(CS.Server).Recv(l, _proxy.OptionTimeout,iLife);
                            if (b != null)
                                _oneObj.Body[CS.Server].Add(b);
                        }
                        SetServerSideBody(); //Ver5.7.2
                        //_sideState[CS.Server] = HttpSideState.ServerSideRecvBody;
                        break;
                    }
                }
                len = _proxy.Sock(CS.Server).Length();
                if (_chunkLen > len) { //データ受信が可能かどうかの判断
                    Thread.Sleep(300);//待機
                    break;//受信は、次回に回す
                }
                //データ受信（サイズ分）
                b = _proxy.Sock(CS.Server).Recv(_chunkLen, _proxy.OptionTimeout,iLife);
                if (b == null || b.Length != _chunkLen)
                    return false;

                _oneObj.Body[CS.Server].Add(b);

                if (_isText) {
                    _textData = Bytes.Create(_textData, b);
                    //コンテンツ制限の確認
                    if (IsHitLimitString(_textData))
                        return false;
                }

                _lastRecvServer = DateTime.Now.Ticks;
                _chunkLen = -1;//次回はサイズ取得
            }
            return true;
        }

        //Content-Length形式の受信(RecvServer()から使用される)
        bool RecvServerContentLength(ILife iLife) {
            var len = _proxy.Sock(CS.Server).Length();
            if (len <= 0)
                return true;

            var b = _proxy.Sock(CS.Server).Recv(len, _proxy.OptionTimeout,iLife);
            if (b == null)
                return false;

            _oneObj.Body[CS.Server].Add(b);

            if (_isText) {
                //コンテンツ制限の確認
                if (_oneObj.Body[CS.Server].CanUse) {//Ver5.6.1
                    if (IsHitLimitString(_oneObj.Body[CS.Server].Get()))
                        return false;
                }
            }

            _lastRecvServer = DateTime.Now.Ticks;
            return true;
        }

        public bool SendClient(ILife iLife) {
            if (_sideState[CS.Server] != HttpSideState.ServerSideRecvHeader && _sideState[CS.Server] != HttpSideState.ServerSideRecvBody)
                return true;

            if (_sideState[CS.Client] == HttpSideState.ClientSideRecvRequest) {
                //サーバから受信したレスポンス及びヘッダをバッファに展開する
                var sendBuf = Bytes.Create(_response.ToString(), "\r\n", _oneObj.Header[CS.Server].GetBytes());
                if (!Send(_proxy.Sock(CS.Client), sendBuf))//送信
                    return false;
                _sideState[CS.Client] = HttpSideState.ClientSideSendHeader;//ヘッダ送信完了
            }
            if (_sideState[CS.Client] == HttpSideState.ClientSideSendHeader) {
                //バッファに残っているデータの送信
                if (!SendBuf(CS.Server))
                    return false;
            }

            //クライアントへの送信完了を確認する（ステータス変更）
            if (_oneHttpKind == OneHttpKind.ContentLength) { //Content-Lengthの場合
                //posがContentLengthまで達したら完了
                if (_oneObj.Pos[CS.Server] >= _contentLength)
                    _sideState[CS.Client] = HttpSideState.ClientSideSendBody;
            } else if (_oneHttpKind == OneHttpKind.Chunk) { //chunkの場合
                //サーバ側の受信が完了し、バッファをすべて送信したら完了
                if (_sideState[CS.Server] == HttpSideState.ServerSideRecvBody && _oneObj.Body[CS.Server].Length <= _oneObj.Pos[CS.Server])
                    _sideState[CS.Client] = HttpSideState.ClientSideSendBody;
            }
            //上記の条件で送信完了していない場合でも
            if (_sideState[CS.Client] != HttpSideState.ClientSideSendBody) {
                //サーバ側が切断され、受信未処理のデータが存在せず、バッファをすべて送信していたら完了
                if (_proxy.Sock(CS.Server).SockState == SockState.Error && _proxy.Sock(CS.Server).Length() == 0 && _oneObj.Body[CS.Server].Length <= _oneObj.Pos[CS.Server]) {
                    _sideState[CS.Client] = HttpSideState.ClientSideSendBody;
                }
            }

            return true;
        }

        //バッファに残っているデータの送信
        //パラメータ cs CS.SERVER を設定した場合、buf[CS.SERVER]を処理対象とし、クライアント側に送信する
        bool SendBuf(CS cs) {
            var sock = _proxy.Sock(CS.Client);
            if (cs == CS.Client)
                sock = _proxy.Sock(CS.Server);

            var len = _oneObj.Body[cs].Length - _oneObj.Pos[cs];
            if (len > 0) {
                //Ver5.6.1
                byte[] sendBuf = _oneObj.Body[cs].SendBuf((int)_oneObj.Pos[cs]);
                if (!Send(sock, sendBuf))//送信
                    return false;
                _oneObj.Pos[cs] += len;
                //byte[] sendBuf = new byte[len];
                //Buffer.BlockCopy(oneObj.Body[cs], oneObj.Pos[cs], sendBuf, 0, len);
                //if (!Send(sock, sendBuf))//送信
                //    return false;
                //if (oneObj.Pos[cs] == 0) {
                //    oneObj.Body[cs] = new byte[0];
                //    if (len > 65535) {
                //        System.GC.Collect();
                //    }
                //} else {
                //    oneObj.Pos[cs] += len;
                //}

            }
            return true;
        }

        //送信
        bool Send(SockTcp sock, byte[] sendBuf) {
            var c = sock.SendUseEncode(sendBuf);
            if (c != sendBuf.Length){
                return false;
            }
            return true;
        }

        //コンテンツ制限の確認
        bool IsHitLimitString(byte[] b) {
            if (_isText) {
                CheckCharset(b);//キャラクタセットのチェック

                var str = "";

                switch (_charset) {
                    case Charset.Ascii:
                        str = Encoding.ASCII.GetString(b);
                        break;
                    case Charset.Sjis:
                        str = Encoding.GetEncoding("shift-jis").GetString(b);
                        break;
                    case Charset.Euc:
                        str = Encoding.GetEncoding("euc-jp").GetString(b);
                        break;
                    case Charset.Jis:
                        str = Encoding.GetEncoding(50222).GetString(b);
                        break;
                    case Charset.Utf8:
                        str = Encoding.UTF8.GetString(b);
                        break;
                    case Charset.Utf7:
                        str = Encoding.UTF7.GetString(b);
                        break;
                    case Charset.Unknown:
                        str = Encoding.ASCII.GetString(b);
                        break;
                }
                //コンテンツ制限
                string hitStr = ProxyHttp.LimitString.IsHit(str);
                if (hitStr != null) {
                    //制限にヒットした場合
                    _proxy.Logger.Set(LogKind.Normal, _proxy.Sock(CS.Server), 21, hitStr);
                    return true;
                }
            }
            return false;
        }

        //キャラクタセットのチェック
        void CheckCharset(byte[] b) {
            if (_charset == Charset.Unknown) {
                var s = Encoding.ASCII.GetString(b);
                int index = s.ToLower().IndexOf("charset");
                if (0 <= index) {
                    s = s.Substring(index + 8);
                    if (s.ToLower().IndexOf("x-sjis") >= 0) {
                        _charset = Charset.Sjis;
                    } else if (s.ToLower().IndexOf("shift_jis") >= 0) {
                        _charset = Charset.Sjis;
                    } else if (s.ToLower().IndexOf("x-euc-jp") >= 0) {
                        _charset = Charset.Euc;
                    } else if (s.ToLower().IndexOf("euc-jp") >= 0) {
                        _charset = Charset.Euc;
                    } else if (s.ToLower().IndexOf("utf-8") >= 0) {
                        _charset = Charset.Utf8;
                    } else if (s.ToLower().IndexOf("utf-7") >= 0) {
                        _charset = Charset.Utf7;
                    } else if (s.ToLower().IndexOf("iso-2022-jp") >= 0) {
                        _charset = Charset.Jis;
                    }
                }
            }
        }
        void SetServerSideBody() {
            //_proxy.Logger.Set(LogKind.Debug, null, 999, String.Format("◆Code={0} {1}", _response.Code, _sideState[CS.Client]));
            if (_response.Code == 100) {
                _sideState[CS.Server] = HttpSideState.ServerSideSendBody;//送信完了状態まで巻き戻す
            }else{
                _sideState[CS.Server] = HttpSideState.ServerSideRecvBody;//受信完了
                
            }
        }
    }
}
