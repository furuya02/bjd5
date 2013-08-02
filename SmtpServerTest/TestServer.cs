using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Bjd;
using Bjd.mail;
using Bjd.net;
using Bjd.option;
using Bjd.server;
using BjdTest.test;

namespace SmtpServerTest {
    enum TestServerType{
        Pop,
        Smtp
    }

    class TestServer{
        
        private readonly TmpOption _op; //設定ファイルの上書きと退避
        private readonly OneServer _v6Sv; //サーバ
        private readonly OneServer _v4Sv; //サーバ

        public TestServer(TestServerType type,String iniSubDir,String iniFileName) {

            var confName = type == TestServerType.Pop ? "Pop3" : "Smtp";

            //設定ファイルの退避と上書き
            _op = new TmpOption(iniSubDir,iniFileName);
            var kernel = new Kernel();
            var option = kernel.ListOption.Get(confName);
            var conf = new Conf(option);


            //サーバ起動
            if (type == TestServerType.Pop){
                _v4Sv = new Pop3Server.Server(kernel, conf, new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
                _v6Sv = new Pop3Server.Server(kernel, conf, new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
            } else {
                _v4Sv = new SmtpServer.Server(kernel, conf, new OneBind(new Ip(IpKind.V4Localhost), ProtocolKind.Tcp));
                _v6Sv = new SmtpServer.Server(kernel, conf, new OneBind(new Ip(IpKind.V6Localhost), ProtocolKind.Tcp));
            }
            _v4Sv.Start();
            _v6Sv.Start();

            Thread.Sleep(100); //少し余裕がないと多重でテストした場合に、サーバが起動しきらないうちにクライアントからの接続が始まってしまう。

        }

        public String ToString(InetKind inetKind) {
            if (inetKind == InetKind.V4) {
                return _v4Sv.ToString();
            }
            return _v6Sv.ToString();
        }

        public void SetMail(String user, String fileName) {
            //メールボックスへのデータセット
            var srcDir = String.Format("{0}\\SmtpServerTest\\", TestUtil.ProjectDirectory());
            var dstDir = String.Format("{0}\\mailbox\\{1}\\", srcDir,user);
            File.Copy(srcDir + "DF_" + fileName, dstDir + "DF_" + fileName, true);
            File.Copy(srcDir + "MF_" + fileName, dstDir + "MF_" + fileName, true);
        }

        //DFファイルの一覧を取得する
        public string[] GetDf(string user) {
            var dir = String.Format("{0}\\SmtpServerTest\\mailbox\\{1}", TestUtil.ProjectDirectory(),user);
            //var dir = string.Format("c:\\tmp2\\bjd5\\SmtpServerTest\\mailbox\\{0}", user);
            var files = Directory.GetFiles(dir, "DF*");
            return files;
        }

        //メールの一覧を取得する
        public List<Mail> GetMf(string user) {
            var dir = String.Format("{0}\\SmtpServerTest\\mailbox\\{1}", TestUtil.ProjectDirectory(), user);
            var ar = new List<Mail>();
            foreach (var fileName in Directory.GetFiles(dir, "MF*")){
                var mail = new Mail();
                mail.Read(fileName);
                ar.Add(mail);
            }
            return ar;
        }

        
        public void Dispose(){
            //サーバ停止
            _v4Sv.Stop();
            _v6Sv.Stop();

            _v4Sv.Dispose();
            _v6Sv.Dispose();

            //設定ファイルのリストア
            _op.Dispose();

            //メールボックスの削除
            var path = String.Format("{0}\\SmtpServerTest\\mailbox", TestUtil.ProjectDirectory());
            //Directory.Delete(@"c:\tmp2\bjd5\SmtpServerTest\mailbox", true);
            Directory.Delete(path, true);
        }
    }
}
