using System;
using System.Net;
using System.IO;

namespace ProxyHttpServerTest {
    class TsWeb : IDisposable {
        readonly string _documentRoot;
        readonly HttpListener _listener;


        public TsWeb(int port, string documentRoot) {
            _documentRoot = documentRoot;


            string prefix = string.Format("http://*:{0}/", port); // 受け付けるURL
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix); // プレフィックスの登録
            
            _listener.Start();
            _listener.BeginGetContext(OnRequested, _listener);

        }
        
        public void Dispose() {
            _listener.Abort();
            //listener.Stop();
            _listener.Close();
            
        }
        //  要求を受信した時に実行するメソッド。
        public void OnRequested(IAsyncResult result) {
            var listener = (HttpListener)result.AsyncState;
            if (!listener.IsListening) {
                return;
            }

            var ctx = listener.EndGetContext(result);
            var req = ctx.Request;
            var res = ctx.Response;
            
            var path = _documentRoot + req.RawUrl.Replace("/", "\\");

            // ファイルが存在すればレスポンス・ストリームに書き出す
            if (File.Exists(path)) {
                byte[] content = File.ReadAllBytes(path);
                res.OutputStream.Write(content, 0, content.Length);
            } else {
                res.StatusCode = 404;
            }
            res.Close();
        }

    }
}