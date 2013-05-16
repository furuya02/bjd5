using System;
using System.Text;
using System.Threading;
using Bjd;
using Bjd.log;
using Bjd.net;
using Bjd.sock;
using Bjd.util;

namespace ProxyFtpServer {
    class FtpTunnel : Tunnel {
        readonly Kernel _kernel;
        //DataThread _dataThread;

        private DataTunnel _dataTunnel = null;
        int _dataPort;

        public FtpTunnel(Kernel kernel, Logger logger, int idleTime, int dataPort, int timeout)
            : base(logger, idleTime, timeout) {
            _kernel = kernel;
            _dataPort = dataPort;
        }

        public int Dispose() {
            if (_dataTunnel != null) {
                _dataTunnel.Dispose();
            }
            return _dataPort;
        }

        //受信時の処理
        protected override byte[] Assumption(byte[] buf, ILife iLife){
            var resultBuf = new byte[0];
            
            //一度に複数行分のデータが来る場合が有るので、行単位に分割して処理する
            var lines = Inet.GetLines(buf);
            foreach (var l in lines){
                resultBuf = Bytes.Create(resultBuf,AssumptionLine(l, iLife));
            }
            return resultBuf;
        }

        //protected override byte[] AssumptionLine(byte[] buf,ILife iLife) {
        byte[] AssumptionLine(byte[] buf,ILife iLife) {
            //stringに変換してから処理する
            var str = Encoding.ASCII.GetString(buf);

            Logger.Set(LogKind.Detail, null, 7, str);

            var index = str.IndexOf(" ");
            if (index < 0)
                goto end;

            var cmdStr = str.Substring(0, index);
            var paramStr = str.Substring(index + 1);

            if (cmdStr == "227"){
                //PASVに対するレスポンス

                //**********************************************************************
                //　「PASV 192.168.22.102,23,15」コマンドからサーバ側の情報を取得する
                //**********************************************************************
                var tmp2 = paramStr.Split('(', ')');
                if (tmp2.Length != 3){
                    Logger.Set(LogKind.Error, null, 5, str);
                    goto end;
                }
                string[] tmp = tmp2[1].Split(',');
                if (tmp.Length != 6){
                    Logger.Set(LogKind.Error, null, 6, str);
                    goto end;
                }
                var connectIp = new Ip(string.Format("{0}.{1}.{2}.{3}", tmp[0], tmp[1], tmp[2], tmp[3]));
                var connectPort = Convert.ToInt32(tmp[4])*256 + Convert.ToInt32(tmp[5]);

                //**********************************************************************
                // クライアント側用のListenソケットを生成する
                //**********************************************************************
                var listenIp = Sock[CS.Client].LocalIp;
                // 利用可能なデータポートの選定
                int listenPort = 0;
                while (iLife.IsLife()){
                    _dataPort++;
                    if (SockServer.IsAvailable(_kernel, listenIp, _dataPort)){
                        listenPort = _dataPort;
                        break;
                    }
                }

                //**********************************************************************
                // クライアント側に送る227レスポンスを生成する
                //**********************************************************************
                str = string.Format("227 Entering Passive Mode ({0},{1},{2},{3},{4},{5})\r\n", listenIp.IpV4[0], listenIp.IpV4[1], listenIp.IpV4[2], listenIp.IpV4[3], listenPort/256, listenPort%256);
                buf = Encoding.ASCII.GetBytes(str);

                //データスレッドの準備
                if (_dataTunnel != null){
                    _dataTunnel.Dispose();
                }
                _dataTunnel = new DataTunnel(_kernel, Logger, listenIp, listenPort, connectIp, connectPort,this);
                _dataTunnel.Start();
                Thread.Sleep(3); //Listenが完了してから 227を送信する

            } else if (cmdStr == "226") { //Transfer complete.
                //_dataTunnel.WaitComplate();
                //Thread.Sleep(10);
                //_dataTunnel.Stop();
            } else if (cmdStr.ToUpper() == "PORT") {

                //**********************************************************************
                //「PORT 192.168.22.102,23,15」コマンドからクライアント側の情報を取得する
                //**********************************************************************
                var tmp = paramStr.Split(',');
                if (tmp.Length != 6) {
                    Logger.Set(LogKind.Error, null, 4, str);
                    goto end;
                }
                var connectIp = new Ip(string.Format("{0}.{1}.{2}.{3}", tmp[0], tmp[1], tmp[2], tmp[3]));
                var connectPort = Convert.ToInt32(tmp[4]) * 256 + Convert.ToInt32(tmp[5]);

                //**********************************************************************
                // サーバ側用のListenソケットを生成する
                //**********************************************************************
                var listenIp = Sock[CS.Server].LocalIp;
                // 利用可能なデータポートの選定
                int listenPort = 0;
                while (iLife.IsLife()){
                    _dataPort++;
                    if (SockServer.IsAvailable(_kernel,listenIp,_dataPort)){
                        listenPort = _dataPort;
                        break;
                    }
                }
                //**********************************************************************
                // サーバ側に送るPORTコマンドを生成する
                //**********************************************************************
                //置き換えたPORTコマンドをセットする
                str = string.Format("PORT {0},{1},{2},{3},{4},{5}\r\n", listenIp.IpV4[0], listenIp.IpV4[1], listenIp.IpV4[2], listenIp.IpV4[3], listenPort / 256, listenPort % 256);
                buf = Encoding.ASCII.GetBytes(str);

                //データスレッドの準備
                if (_dataTunnel != null) {
                    _dataTunnel.Dispose();
                }
                _dataTunnel = new DataTunnel(_kernel, Logger, listenIp, listenPort, connectIp, connectPort,this);
                _dataTunnel.Start();
                Thread.Sleep(3); //Listenが完了してから PORTを送信する
            }
        end:
            return buf;
        }
    }
}