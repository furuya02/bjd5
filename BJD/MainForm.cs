using System;
using System.Windows.Forms;
using Bjd.util;


namespace Bjd {


    public partial class MainForm : Form {
        readonly Kernel _kernel;
        public MainForm() {
            InitializeComponent();
            //kernel = new Kernel(this, listViewMainLog, menuStrip, contextMenuStrip,notifyIcon);
            _kernel = new Kernel(this, listViewMainLog, menuStrip, notifyIcon);

        }


        private void MainFormActivated(object sender, EventArgs e) {
            _kernel.View.Activated();
        }
        //�t�H�[����������Ƃ�
        private void MainFormFormClosing(object sender, FormClosingEventArgs e) {

            //�v���O�����̏I���m�F
            if (_kernel.RunMode == RunMode.Normal || _kernel.RunMode == RunMode.NormalRegist) {
                if ((bool)_kernel.ListOption.Get("Basic").GetValue("useExitDlg")) {
                    if (DialogResult.OK != Msg.Show(MsgKind.Question, _kernel.IsJp() ? "�v���O������I�����Ă�낵���ł���" : "May I finish a program?")) {
                        e.Cancel = true;//�I�������Œ��~���ꂽ�ꍇ�́A�v���O������I�����Ȃ�
                        return;
                    }
                }
            }
            _kernel.Dispose();
        }
        protected override void WndProc(ref Message m) {
            //�ŏ������b�Z�[�W��t�b�N����
            //�@WM_SYSCOMMAND(0x112) 
            if (m.Msg == 0x112){
                //SC_MINIMIZE(0xF020) �ŏ���
                if(m.WParam == (IntPtr)0xF020){
                    _kernel.View.SetVisible(false);
                    return;
                //Ver5.0.0-a5
                //SC_CLOSE(0xF060)�N���[�Y�E�C���h�E
                }
                if(m.WParam == (IntPtr)0xF060){
                    _kernel.View.SetVisible(false);
                    return;                
                }
            }
            base.WndProc(ref m);
        } 

        private void PopupMenuClick(object sender, EventArgs e) {
            if (sender is NotifyIcon) {//�^�X�N�g���C�A�C�R���̃_�u���N���b�N
                if (CheckPassword()) {//�Ǘ��҃p�X���[�h�̊m�F
                    _kernel.View.SetVisible(true);
                    LogEnsure();//Ver5.0.0-b23 �ŏI�s��\������
                }
            } else {
                var menu = (ToolStripMenuItem)sender;
                if (menu.Name.IndexOf("PopupMenuOpen") == 0) {//�u�J���v
                    if (CheckPassword()) {//�Ǘ��҃p�X���[�h�̊m�F
                        _kernel.View.SetVisible(true);
                        LogEnsure();//Ver5.0.0-b23 �ŏI�s��\������
                    }
                } else if (menu.Name.IndexOf("PopupMenuExit") == 0) {//�u�I���v
                    if (CheckPassword()) //�Ǘ��҃p�X���[�h�̊m�F
                        Close();
                }
            }
        }

        //Ver5.0.0-b23 �ŏI�s��\������
        private void LogEnsure() {
            //Ver5.0.1
            if(listViewMainLog.Items.Count>0)
                listViewMainLog.EnsureVisible(listViewMainLog.Items[listViewMainLog.Items.Count - 1].Index);
        }

        //�Ǘ��҃p�X���[�h�̊m�F
        //Ver5.4.2
        //bool PasswordCheck() {
        bool CheckPassword() {
            var op = _kernel.ListOption.Get("Basic");
            if (!(bool)op.GetValue("useAdminPassword"))
                return true;
            var password = (string)op.GetValue("password");
            if (password == "") 
                return true;
            var dlg = new PasswordDlg(_kernel);
            while (true) {
                if (DialogResult.OK != dlg.ShowDialog())
                    return false;
                if (dlg.PasswordStr == password)
                    return true;
                Msg.Show(MsgKind.Error,(_kernel.IsJp()) ? "�p�X���[�h���Ⴂ�܂�" : "password incorrect");
            }
        }
    }

}
