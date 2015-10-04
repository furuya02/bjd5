using System;
using System.Windows.Forms;
using System.ServiceProcess;

namespace Bjd.service {
    internal partial class SetupServiceDlg : Form {
        readonly SetupService _setupService;
        readonly Kernel _kernel;
        public SetupServiceDlg(Kernel kernel) {
            InitializeComponent();

            _setupService = new SetupService(kernel);

            _kernel = kernel;

            Text = (kernel.IsJp()) ?"サービス設定ダイアログ":"Setting Service";

            groupBoxInstall.Text = (kernel.IsJp()) ? "サービスへのインストール" : "Registration";
            buttonInstall.Text = (kernel.IsJp()) ? "登録" : "Install";
            buttonUninstall.Text = (kernel.IsJp()) ? "削除" : "Uninstall";

            groupBoxStatus.Text = (kernel.IsJp()) ? "状態" : "Service status";
            buttonStart.Text = (kernel.IsJp()) ? "開始" : "Start";
            buttonStop.Text = (kernel.IsJp()) ? "停止" : "Stop";
            buttonRestart.Text = (kernel.IsJp()) ? "再起動" : "Restart";

            groupBoxStartupType.Text = (kernel.IsJp()) ? "スタートアップの種類" : "Startup type";
            buttonAutomatic.Text = (kernel.IsJp()) ? "自動" : "Auto";
            buttonManual.Text = (kernel.IsJp()) ? "手動" : "Manual";
            buttonDisable.Text = (kernel.IsJp()) ? "無効" : "Disable";
            
            
            DispInit();
        }

        public override sealed string Text{
            get { return base.Text; }
            set { base.Text = value; }
        }

        void DispInit() {

            if (_setupService.IsRegist) {//サービスが登録済みかどうか

                //「インストール」
                textBoxInstall.Text = (_kernel.IsJp()) ? "登録" : "Registered";
                buttonInstall.Enabled = false;
                buttonUninstall.Enabled = true;

                //「状態」
                groupBoxStatus.Visible = true;//グループ表示
                buttonStart.Enabled = true;
                buttonStop.Enabled = false;
                buttonRestart.Enabled = false;
                switch (_setupService.Status) {
                    case ServiceControllerStatus.ContinuePending:
                        textBoxStatus.Text = (_kernel.IsJp()) ? "保留中" : "ContinuePending";
                        break;
                    case ServiceControllerStatus.Paused:
                        textBoxStatus.Text = (_kernel.IsJp()) ? "一時中断" : "Paused";
                        break;
                    case ServiceControllerStatus.PausePending:
                        textBoxStatus.Text = (_kernel.IsJp()) ? "一時中断保留中" : "PausePending";
                        break;
                    case ServiceControllerStatus.Running:
                        textBoxStatus.Text = (_kernel.IsJp()) ? "実行中" : "Running";
                        buttonStart.Enabled = false;
                        buttonStop.Enabled = true;
                        buttonRestart.Enabled = true;
                        break;
                    case ServiceControllerStatus.StartPending:
                        textBoxStatus.Text = (_kernel.IsJp()) ? "開始中" : "StartPending";
                        break;
                    case ServiceControllerStatus.Stopped:
                        textBoxStatus.Text = (_kernel.IsJp()) ? "停止" : "Stopped";
                        break;
                    case ServiceControllerStatus.StopPending:
                        textBoxStatus.Text = (_kernel.IsJp()) ? "停止中" : "StopPending";
                        break;
                }
                //スタートアップ
                groupBoxStartupType.Visible = true;//グループ表示
                switch (_setupService.StartupType) {
                    case "Auto":
                        textBoxStartupType.Text = (_kernel.IsJp()) ? "自動" : "Auto";
                        buttonAutomatic.Enabled = false;
                        buttonManual.Enabled = true;
                        buttonDisable.Enabled = true;
                        break;
                    case "Manual":
                        textBoxStartupType.Text = (_kernel.IsJp()) ? "手動" : "Manual";
                        buttonAutomatic.Enabled = true;
                        buttonManual.Enabled = false;
                        buttonDisable.Enabled = true;
                        break;
                    case "Disabled":
                        textBoxStartupType.Text = (_kernel.IsJp()) ? "無効" : "Disabled";
                        buttonAutomatic.Enabled = true;
                        buttonManual.Enabled = true;
                        buttonDisable.Enabled = false;
                        break;
                }

                _kernel.RunMode = RunMode.NormalRegist;

            } else {
               
                textBoxInstall.Text = (_kernel.IsJp()) ? "未登録" : "Not Regist";
                buttonInstall.Enabled = true;
                buttonUninstall.Enabled = false;

                //「状態」「スタートアップ」グループ非表示
                groupBoxStatus.Visible = false;
                groupBoxStartupType.Visible = false;

                _kernel.RunMode = RunMode.Normal;
            }
            //「起動/停止」メニューの初期化
            //kernel.Menu2.InitStartStop(kernel.IsRunnig);
            _kernel.View.SetColor();//ウインド色の初期化
        }
        //登録
        private void ButtonInstallClick(object sender, EventArgs e) {
            Job(ServiceCmd.Install);

        }
        //削除
        private void ButtonUninstallClick(object sender, EventArgs e) {
　          Job(ServiceCmd.Uninstall);
        }
        //開始
        private void ButtonStartClick(object sender, EventArgs e) {
            Job(ServiceCmd.Start);

        }
        //停止
        private void ButtonStopClick(object sender, EventArgs e) {
            Job(ServiceCmd.Stop);
        }
        //再起動
        private void ButtonRestartClick(object sender, EventArgs e) {
            Job(ServiceCmd.Stop);
            Job(ServiceCmd.Start);
        }
        //自動
        private void ButtonAutomaticClick(object sender, EventArgs e) {
            Job(ServiceCmd.Automatic);
        }
        //手動
        private void ButtonManualClick(object sender, EventArgs e) {
            Job(ServiceCmd.Manual);
        }
        //無効
        private void ButtonDisableClick(object sender, EventArgs e) {
            Job(ServiceCmd.Disable);
        }

        void Job(ServiceCmd serviceCmd) {

            Enabled = false;
            Cursor.Current = Cursors.WaitCursor;

            _setupService.Job(serviceCmd);
            DispInit();

            Enabled = true;
            Cursor.Current = Cursors.Default;
        }

        private void ButtonOkClick(object sender, EventArgs e) {
            Close();
        }


    }
}