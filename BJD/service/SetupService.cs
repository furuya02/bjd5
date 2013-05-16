using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.ServiceProcess;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.IO;
using Bjd.util;

namespace Bjd.service
{
    //*********************************************************
    //サービスへの登録・削除及び設定を変更するクラス
    //*********************************************************
    internal class SetupService {
        ServiceController _sc;//サービスコントローラ
        readonly string _exePath;
        //readonly Logger _logger;
        private Kernel _kernel;
        public SetupService(Kernel kernel){
            _kernel = kernel;
            //_logger = kernel.CreateLogger("SetupService", true, null);
            var myAssembly = Assembly.GetEntryAssembly();
            _exePath = myAssembly.Location;

            Init();//最新の状態に更新する
        }
        
        //最新の状態に更新する
        void Init() {
            _sc = new ServiceController("BlackJumboDog");
            try {
                var status = _sc.Status;

            } catch {
                _sc = null; //サービスがインストールされていない
            }
        }

        //【インストール（登録）されているかどうかを取得する】
        public bool IsRegist {
            get{
                return _sc != null;
            }
        }
        //【状態を取得する】
        public ServiceControllerStatus Status {
            get{
                return _sc != null ? _sc.Status : ServiceControllerStatus.Stopped;
            }
        }
        //【スタートアップの種類を取得する】
        public string StartupType {
            get {
                if (_sc != null) {
                    var path = "Win32_Service.Name='" + _sc.ServiceName + "'";
                    var p = new ManagementPath(path);
                    var managementObj = new ManagementObject(p);

                    return managementObj["StartMode"].ToString();
                }
                return "";
            }
        }
        //【公開ファンクション】
        public void Job(ServiceCmd serviceCmd) {


            //現在のユーザーのWindowsIdentityオブジェクトを取得
            var wi = WindowsIdentity.GetCurrent();
            if (wi != null){
                //WindowsPrincipalオブジェクトを作成する
                var wp = new WindowsPrincipal(wi);
                //Administratorsグループに属しているか調べる
                if (!wp.IsInRole(WindowsBuiltInRole.Administrator)){
                    Msg.Show(MsgKind.Error, _kernel.IsJp() ? "BJD.exeは「管理者として実行」されていません" : "Execute BJD.exe as a Administrator");
                    return;
                }
            }

            switch (serviceCmd) {
                case ServiceCmd.Install:
                    InstallUtil(true);
                    break;
                case ServiceCmd.Uninstall:
                    if (_sc.Status != ServiceControllerStatus.Stopped) {
                        _sc.Stop();
                        _sc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 10));
                    }
                    InstallUtil(false);
                    break;
                case ServiceCmd.Start:
                    _sc.Start();
                    _sc.WaitForStatus(ServiceControllerStatus.Running,new TimeSpan(0,0,10));
                    break;
                case ServiceCmd.Stop:
                    _sc.Stop();
                    _sc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 10));
                    break;
                case ServiceCmd.Automatic:
                    SetStartupType("Automatic");
                    break;
                case ServiceCmd.Manual:
                    SetStartupType("Manual");
                    break;
                case ServiceCmd.Disable:
                    SetStartupType("Disabled");
                    break;
            }
            Init();
        }

        void SetStartupType(string value) {
            var path = "Win32_Service.Name='" + _sc.ServiceName + "'";
            var p = new ManagementPath(path);
            var managementObj = new ManagementObject(p);
            var parameters = new object[1];
            parameters[0] = value;//Automatic Manual  Disabled
            managementObj.InvokeMethod("ChangeStartMode", parameters);
        }

        

        //自分自身をサービスへインストール（アンインストール）する
        //sw==true インストール
        //sw==false アンインストール
        void InstallUtil(bool sw) {
            //installutil.exeのフルパスを取得
            string installutilPath = Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "installutil.exe");
            if (!File.Exists(installutilPath)) {
                Msg.Show(MsgKind.Error,"installutil.exeが見つかりませんでした。");
                goto end;
            }
            //installutil.exeを起動
            var stdout = new List<string>();//エラーの場合に確認するため標準出力を取得する
            Process p;
            try {
                var dir = Path.GetDirectoryName(_exePath);//作業ディレクトリ
                const string utilName = "InstallUtil.exe";
                var utilPath = dir + "\\"+ utilName;
                File.Copy(installutilPath,utilPath,true);

                //コマンド名 パラメータ
                var info = new ProcessStartInfo{FileName = utilName, Arguments = "BJD.exe"};
                if (!sw)
                    info.Arguments = "/u BJD.exe";
                info.CreateNoWindow = true;//子プロセスのウィンドウを表示しない。
                info.UseShellExecute = false;// StandardInput を使用する場合は、UseShellExecute が false になっている必要がある
                info.RedirectStandardInput = true;// 標準入力を使用する
                info.RedirectStandardOutput = true;// 標準出力を使用する
                info.RedirectStandardError = true;//標準エラー出力を使用する
                if (dir != null)
                    info.WorkingDirectory = dir;//作業ディレクトリ

                p = Process.Start(info);


                //標準出力からのデータ取得
                //標準出力バッファが満タンなると子プロセスがロックすのでWaitより前に読み込む
                string [] lines = p.StandardOutput.ReadToEnd().Split(new[]{"\r\n"},StringSplitOptions.RemoveEmptyEntries);
                stdout.AddRange(lines);

                //string stderr = p.StandardError.ReadToEnd();
                //File.Delete(utilPath);

                p.WaitForExit();

                try {
                    File.Delete(utilPath);
                } catch {

                }


            } catch {
                Msg.Show(MsgKind.Error,"installutil.exeの起動に失敗しました。");
                goto end;
            }

            if (p.ExitCode != 0) {
                Msg.Show(MsgKind.Error,"installutil.exeがエラーコード(" + p.ExitCode.ToString() + ")を返しました。\nBJD.exeを「管理者として実行」してください。" + "");
                //foreach (string s in stdout) {
                    //_logger.Set(LogKind.Error,null,9000039,s);
                //}
                goto end;
            }
            Environment.ExitCode = 0;
            Init();
        end:
            ;
        }


    }
}
