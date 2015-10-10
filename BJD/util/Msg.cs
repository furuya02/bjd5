using System.Windows.Forms;

namespace Bjd.util {
    public class Msg {
        private Msg() { }//�f�t�H���g�R���X�g���N�^�̉B��

        static public DialogResult Show(MsgKind msgKind, string msg) {
            var buttons = MessageBoxButtons.OK;
            var icon = MessageBoxIcon.Error;
            switch (msgKind) {
                case MsgKind.Stop:
                    buttons = MessageBoxButtons.RetryCancel;
                    break;
                case MsgKind.Question:
                    buttons = MessageBoxButtons.OKCancel;
                    icon = MessageBoxIcon.Question;
                    break;
                case MsgKind.Infomation:
                    icon = MessageBoxIcon.Information;
                    break;
                case MsgKind.Warning:
                    icon = MessageBoxIcon.Warning;
                    break;

            }
            return MessageBox.Show(msg, Application.ProductName, buttons, icon);
        }
    }
}