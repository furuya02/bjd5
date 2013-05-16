using System;
using System.Text;
using System.Threading;
using System.IO;
using Bjd;
using System.Globalization;
using Bjd.log;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using Bjd.sock;
using Bjd.util;


namespace TftpServer {
    partial class Server :OneServer {

        //UserList userList;
        readonly string _workDir;
        //コンストラクタ
        public Server(Kernel kernel, Conf conf,OneBind oneBind)
            : base(kernel,conf,oneBind) {
            _workDir = (string)Conf.Get("workDir");
        }
        enum Opcode {
            Unknown=0,
            Rrq=1,
            Wrq=2,
            Data=3,
            Ack=4,
            Error=5,
            Oack=6
        }
        enum TftpMode {
            Netascii = 0,
            Octet = 1
        }


        override protected bool OnStartServer() { return true; }
        override protected void OnStopServer() { }
        //接続単位の処理
        override protected void OnSubThread(SockObj sockObj) {
            var sockUdp = (SockUdp)sockObj;

            //作業フォルダの確認
            if (_workDir == "") {
                Logger.Set(LogKind.Error,null,5,"");
                goto end;
            }
            if (!Directory.Exists(_workDir)) {
                Logger.Set(LogKind.Error,null,6,string.Format("workDir = {0}",_workDir));
                goto end;
            }

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            
            var offset = 0;
            var opCode = Opcode.Unknown;
            var fileName = "";
            var tftpMode = TftpMode.Netascii;

            if (!GetOpCode(sockUdp,ref opCode,ref offset))//オペコードの取得
                goto end;

            if (opCode != Opcode.Wrq && opCode != Opcode.Rrq) {
                //クライアントからのリクエストでWRQ及びRRQ以外はエラーとして受け付けない
                goto end;
            }

            if (!GetFileName(sockUdp,ref fileName,ref offset))//ファイル名の取得
                goto end;
            if (!GetMode(sockUdp,ref tftpMode,ref offset))//モードの取得
                goto end;
            var path = string.Format("{0}\\{1}",_workDir,fileName);

            //リクエスト元に対するソケットを新規に作成する
            var ip = sockUdp.RemoteIp;
            var port = sockUdp.RemoteAddress.Port;
            var childObj = new SockUdp(Kernel,ip,port,null,new byte[0]);

            if (opCode == Opcode.Wrq) {//アップロード処理
               
                if (!UpLoad(childObj,path)) {
                    //エラー
                }

            } else if (opCode == Opcode.Rrq) {//ダウンロード処理

                if (!DownLoad(childObj, path)){
                    goto end;
                }
            }
        end:
            if (sockUdp != null)
                sockUdp.Close();
        }

        //オペコードの取得
        bool GetOpCode(SockUdp sockUdp,ref Opcode opCode,ref int offset) {
            //オペコードの取得
            byte n = 0;
            try {
                n = sockUdp.RecvBuf[1];
                opCode = (Opcode)n;
            } catch {
                opCode = Opcode.Unknown;
            }
            if (opCode < Opcode.Rrq || Opcode.Oack < opCode) {
                Logger.Set(LogKind.Error,sockUdp,1,string.Format("OPCODE=0x{0:x}",n));
                return false;
            }
            offset += 2;
            return true;
        }
        //ファイル名の取得
        bool GetFileName(SockUdp sockUdp,ref string fileName,ref int offset) {
            fileName = GetString(sockUdp.RecvBuf,offset);
            if (fileName == null) {
                Logger.Set(LogKind.Error,sockUdp,2,"fileName=null");
                return false;
            }
            offset += fileName.Length + 1;
            return true;
        }
        //モードの取得
        bool GetMode(SockUdp sockUdp,ref TftpMode tftpMode,ref int offset) {
            string modeStr = GetString(sockUdp.RecvBuf,offset);
            if (modeStr == null) {
                Logger.Set(LogKind.Error,sockUdp,3,"mode=null");
                return false;
            }
            if (modeStr.ToLower() == "netascii") {
                tftpMode = TftpMode.Netascii;
            } else if (modeStr.ToLower() == "octet") {
                tftpMode = TftpMode.Octet;
            } else {
                Logger.Set(LogKind.Error,sockUdp,4,string.Format("mode={0}",modeStr));
                return false;
            }
            offset += modeStr.Length + 1;
            return true;
        }
        //アスキー文字列の取得
        string GetString(byte [] buffer,int offset) {
            for (int i = 0; i < buffer.Length - offset; i++) {
                if (buffer[offset + i] == '\0') {
                    return  Encoding.ASCII.GetString(buffer,offset,i);
                }
            }
            return null;
        }


        //ダウンロード（ファイル送信）
        bool DownLoad(SockUdp childObj,string path) {

            var ret = false;
            var no = (ushort)1;
            var total = 0;
            FileStream fs = null;
            BinaryReader br = null;

            if (!(bool)Conf.Get("read")) {//「読込み」が許可されていない
                Logger.Set(LogKind.Secure,childObj,10,path);
                //エラーコード(2) アクセス違反
                childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Error),Util.htons(2),"Receive of a message prohibition"));
                goto end;
            }

            if (!File.Exists(path)) {//ファイルが見つからない
                Logger.Set(LogKind.Secure,childObj,13,path);
                //エラーコード(1) ファイルが見つからない
                childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Error),Util.htons(1),"A file is not found"));
                goto end;
            }

            try {
                fs = new FileStream(path,FileMode.OpenOrCreate,FileAccess.Read);
            } catch (Exception e) {
                //エラーコード(2) アクセス違反
                childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Error),Util.htons(2),e.Message));
                goto end;
            }

            br = new BinaryReader(fs);

            while (true) {
                var data = br.ReadBytes(512);

                //DATA 送信
                childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Data),Util.htons(no),data));
                total += data.Length;

                if (data.Length < 512) {
                    if (data.Length == 0) {
                        //最後の 0bytes データを送る
                        childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Data),Util.htons(no)));
                    }
                    Logger.Set(LogKind.Normal,childObj,9,string.Format("{0} {1}byte",path,total));
                    ret = true;
                    goto end;
                }

                Thread.Sleep(10);
                
                // ACK待ち
                //if (!childObj.Recv(Timeout)) {
                var buf = childObj.Recv(Timeout);
                if (buf.Length==0) {
                    Logger.Set(LogKind.Error,childObj,7,string.Format("{0}sec",Timeout));
                    break;
                }
                if ((Opcode)(buf[1]) != Opcode.Ack) 
                    break;
                //ACK番号が整合しているかどうかの確認
                var ackNo = Util.htons(BitConverter.ToUInt16(buf,2));
                if (no != ackNo) {
                    Logger.Set(LogKind.Error,childObj,14,string.Format("no={0} ack={1}",no,ackNo));
                    //エラーとして処理する
                    childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Error),Util.htons(2),"unmatch ACK"));
                    goto end;
                }
                no++;//次のデータ
            }
        end:
            if (br != null)
                br.Close();
            if (fs != null)
                fs.Close();
            return ret;
        }
        //アップロード（ファイル受信）
        bool UpLoad(SockUdp childObj,string path) {

            var ret = false;
            
            ushort no = 0;
            var totalSize = 0;
            FileStream fs = null;
            BinaryWriter bw = null;

            if (!(bool)Conf.Get("write")) {//「書込み」が許可されていない
                Logger.Set(LogKind.Secure,childObj,11,path);
                //エラーコード(2) アクセス違反
                childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Error),Util.htons(2),"Transmission of a message prohibition"));
                goto end;
            }
            if (!(bool)Conf.Get("override")) {//「上書き」が許可されていない
                if (File.Exists(path)) {
                    Logger.Set(LogKind.Secure,childObj,12,path);
                    //エラーコード(6) アクセス違反
                    childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Error),Util.htons(6),"There is already a file"));
                    goto end;
                }
            }
            
            
            //ACK(0)送信
            childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Ack),Util.htons(no)));
            
            try {
                fs = new FileStream(path,FileMode.OpenOrCreate,FileAccess.Write);
            } catch (Exception e) {
                //エラーコード(2) アクセス違反
                childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Error),Util.htons(2),e.Message));
                goto end;
            }

            bw = new BinaryWriter(fs);

            while (true) {
                //受信
//                if (!childObj.Recv(Timeout)) {
                var buf = childObj.Recv(Timeout);
                if (buf.Length==0) {
                    Logger.Set(LogKind.Error,childObj,7,string.Format("{0}sec",Timeout));
                    break;
                }
                if ((Opcode)(buf[1]) != Opcode.Data) {
                    break;
                }
                
                //次のデータかどうかの確認
                if (Util.htons(BitConverter.ToUInt16(buf,2)) != no + 1)
                    continue;
                no++;

                int size = buf.Length - 4;
                bw.Write(buf,4,size); //Write
                totalSize += size;

                //ACK送信
                childObj.Send(Bytes.Create(Util.htons((ushort)Opcode.Ack),Util.htons(no)));

                if (size != 512) {
                    Logger.Set(LogKind.Normal,childObj,8,string.Format("{0} {1}byte",path,totalSize));
                    ret = true;
                    goto end;
                }
                Thread.Sleep(1);
            }
        end:
            if(bw!=null)
                bw.Close();
            if(fs!=null)
                fs.Close();
            return ret;
        }

        //RemoteServerでのみ使用される
        public override void Append(OneLog oneLog) {

        }

    }
}
