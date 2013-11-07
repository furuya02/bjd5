using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Bjd.log;

namespace Bjd.net {
    public class Ssl : IDisposable {
        public bool Status { get; private set; }

        //接続先サーバ名（クライアント用）
        readonly string _targetServer;
        //証明書（サーバ用）
        readonly X509Certificate2 _x509Certificate2;

        public void Dispose() {
        }

        //クライアント用コンストラクタ
        public Ssl(string targetServer) {
            _targetServer = targetServer;

            Status = true;//初期化が成功しているかどうかのステータス

        }
        //サーバ用コンストラクタ
        public Ssl(Logger logger, string fileName, string password) {
        //    this.logger = logger;

            Status = false;//初期化が成功しているかどうかのステータス
            if (!File.Exists(fileName)) {
                logger.Set(LogKind.Error, null, 9000026, fileName);
                return;
            }

            try {
                _x509Certificate2 = new X509Certificate2(fileName, password);
            } catch (Exception ex) {
                logger.Set(LogKind.Error, null, 9000023, ex.Message);
                return;
            }
            Status = true;//初期化が成功しているかどうかのステータス

        }

        public OneSsl CreateClientStream(Socket socket) {
            //Ver5.9.8 例外発生に対応
            try{
                return new OneSsl(socket, _targetServer);
            } catch (Exception){
                return null;
            }
        }
        public OneSsl CreateServerStream(Socket socket) {
            //Ver5.9.8 例外発生に対応
            try{
                return new OneSsl(socket, _x509Certificate2);
            } catch (Exception) {
                return null;
            }
        }
    }
}

