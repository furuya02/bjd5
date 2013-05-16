using System.IO;
using System.Windows.Forms;
using Bjd.ctrl;

namespace Bjd.browse {
    class BrowseData {
        readonly ToolStripTextBox _textBox;
        readonly Button _buttonOk;
        readonly CtrlType _ctrlType;
         

        //public string Str = "";
        string _str = "";

        public BrowseData(ToolStripTextBox textBox, Button buttonOk, CtrlType ctrlType) {
            _textBox = textBox;
            _buttonOk = buttonOk;
            _ctrlType = ctrlType;
            buttonOk.Enabled = false;
        }
        public void Set(string path) {
            _str = path;
            if (_ctrlType == CtrlType.Folder) {
                _textBox.Text = Dir;
                _buttonOk.Enabled = (_str != "");
            } else {
                _textBox.Text = _str;

                string fileName = Path.GetFileName(_str);
                _buttonOk.Enabled = fileName != "";
            }
        }
        public string Dir {
            get {
                string dir = Path.GetDirectoryName(_str);
                if (dir == null)
                    return Path.GetPathRoot(_str);
                if (1 <= dir.Length && dir[dir.Length - 1] != '\\')
                    dir = dir + "\\";
                return dir;

            }
        }
        public string File {
            get {
                return _str;
            }
        }
    }
}
