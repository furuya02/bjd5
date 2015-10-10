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
    //�T�[�r�X�ւ̓o�^�E�폜�y�ѐݒ��ύX����N���X
    //*********************************************************
    internal class SetupService {
        ServiceController _sc;//�T�[�r�X�R���g���[��
        readonly string _exePath;
        //readonly Logger _logger;
        private Kernel _kernel;
        public SetupService(Kernel kernel){
            _kernel = kernel;
            //_logger = kernel.CreateLogger("SetupService", true, null);
            var myAssembly = Assembly.GetEntryAssembly();
            _exePath = myAssembly.Location;

            Init();//�ŐV�̏�ԂɍX�V����
        }
        
        //�ŐV�̏�ԂɍX�V����
        void Init() {
            _sc = new ServiceController("BlackJumboDog");
            try {
                var status = _sc.Status;

            } catch {
                _sc = null; //�T�[�r�X���C���X�g�[������Ă��Ȃ�
            }
        }

        //�y�C���X�g�[���i�o�^�j����Ă��邩�ǂ�����擾����z
        public bool IsRegist {
            get{
                return _sc != null;
            }
        }
        //�y��Ԃ�擾����z
        public ServiceControllerStatus Status {
            get{
                return _sc != null ? _sc.Status : ServiceControllerStatus.Stopped;
            }
        }
        //�y�X�^�[�g�A�b�v�̎�ނ�擾����z
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
        //�y���J�t�@���N�V�����z
        public void Job(ServiceCmd serviceCmd) {


            //���݂̃��[�U�[��WindowsIdentity�I�u�W�F�N�g��擾
            var wi = WindowsIdentity.GetCurrent();
            if (wi != null){
                //WindowsPrincipal�I�u�W�F�N�g��쐬����
                var wp = new WindowsPrincipal(wi);
                //Administrators�O���[�v�ɑ����Ă��邩���ׂ�
                if (!wp.IsInRole(WindowsBuiltInRole.Administrator)){
                    Msg.Show(MsgKind.Error, _kernel.IsJp() ? "BJD.exe�́u�Ǘ��҂Ƃ��Ď��s�v����Ă��܂���" : "Execute BJD.exe as a Administrator");
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

        

        //�������g��T�[�r�X�փC���X�g�[���i�A���C���X�g�[���j����
        //sw==true �C���X�g�[��
        //sw==false �A���C���X�g�[��
        void InstallUtil(bool sw) {
            //installutil.exe�̃t���p�X��擾
            string installutilPath = Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "installutil.exe");
            if (!File.Exists(installutilPath)) {
                Msg.Show(MsgKind.Error,"installutil.exe��������܂���ł����B");
                goto end;
            }
            //installutil.exe��N��
            var stdout = new List<string>();//�G���[�̏ꍇ�Ɋm�F���邽�ߕW���o�͂�擾����
            Process p;
            try {
                var dir = Path.GetDirectoryName(_exePath);//��ƃf�B���N�g��
                const string utilName = "InstallUtil.exe";
                var utilPath = dir + "\\"+ utilName;
                File.Copy(installutilPath,utilPath,true);

                //�R�}���h�� �p�����[�^
                var info = new ProcessStartInfo{FileName = utilName, Arguments = "BJD.exe"};
                if (!sw)
                    info.Arguments = "/u BJD.exe";
                info.CreateNoWindow = true;//�q�v���Z�X�̃E�B���h�E��\�����Ȃ��B
                info.UseShellExecute = false;// StandardInput ��g�p����ꍇ�́AUseShellExecute �� false �ɂȂ��Ă���K�v������
                info.RedirectStandardInput = true;// �W�����͂�g�p����
                info.RedirectStandardOutput = true;// �W���o�͂�g�p����
                info.RedirectStandardError = true;//�W���G���[�o�͂�g�p����
                if (dir != null)
                    info.WorkingDirectory = dir;//��ƃf�B���N�g��

                p = Process.Start(info);


                //�W���o�͂���̃f�[�^�擾
                //�W���o�̓o�b�t�@�����^���Ȃ�Ǝq�v���Z�X�����b�N���̂�Wait���O�ɓǂݍ���
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
                Msg.Show(MsgKind.Error,"installutil.exe�̋N���Ɏ��s���܂����B");
                goto end;
            }

            if (p.ExitCode != 0) {
                Msg.Show(MsgKind.Error,"installutil.exe���G���[�R�[�h(" + p.ExitCode.ToString() + ")��Ԃ��܂����B\nBJD.exe��u�Ǘ��҂Ƃ��Ď��s�v���Ă��������B" + "");
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
