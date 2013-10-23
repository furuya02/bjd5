using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Bjd.net {
    public class OneSsl {
        readonly SslStream _stream;

        //クライアント接続
        public OneSsl(Socket socket, string targetServer) {
            _stream = new SslStream(new NetworkStream(socket));
            _stream.AuthenticateAsClient(targetServer);
        }

        //サーバ接続
        public OneSsl(Socket socket, X509Certificate2 x509Certificate2) {
            _stream = new SslStream(new NetworkStream(socket));
            try{
                _stream.AuthenticateAsServer(x509Certificate2);
            } catch (Exception){

            }
            _stream.ReadTimeout = 5000;
            _stream.WriteTimeout = 5000;
        }

        ~OneSsl() {
            try {
                _stream.Close();
            } catch {
            }
        }

        public int Write(byte[] buf, int len) {
            _stream.Write(buf, 0, len);
            return buf.Length;
        }

        public void BeginRead(byte[] buf, int offset, int count, AsyncCallback ac, object o) {
            _stream.BeginRead(buf, offset, count, ac, o);
        }

        public int EndRead(IAsyncResult ar) {
            return _stream.EndRead(ar);
        }

        public void Close() {
            _stream.Close();
        }
    }
}