using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Bjd;
using Bjd.util;

namespace SmtpServer {
    public class Dlg : ToolDlg {
        readonly TreeView _treeView;
        readonly ImageList _imageList = new ImageList();

        readonly List<string> _tmpFileList = new List<string>();

        public Dlg(Kernel kernel, string nameTag, Object obj, string caption)
            : base(kernel, nameTag, obj, caption) {
            //イメージリストの初期化
            _imageList.Images.Add(Resource.user);
            _imageList.Images.Add(Resource.mail);
            _imageList.Images.Add(Resource.queue);

            //ツリービューの作成
            _treeView = new TreeView();
            _treeView.Dock = DockStyle.Fill;
            _treeView.FullRowSelect = true;
            _treeView.HideSelection = false;
            _treeView.ImageList = _imageList;

            //メインコントロールの追加
            AddControl(_treeView);

            _treeView.DoubleClick += DlgDoubleClick;

            ToolStripMenuItem menuFile = AddMenu(null, null, kernel.IsJp() ? "ファイル(&F)" : "&File", Keys.None);
            Add2(menuFile, FuncRefresh, kernel.IsJp() ? "最新の状態に更新する(&R)" : "&Refuresh", Keys.F5);
            AddMenu(menuFile, null, "-", Keys.None);
            AddMenu(menuFile, FuncClose, kernel.IsJp() ? "閉じる(&C)" : "&Close", Keys.None);

            ToolStripMenuItem menuTool = AddMenu(null, null, kernel.IsJp() ? "ツール(&T)" : "&Tool", Keys.None);
            Add2(menuTool, FuncView, kernel.IsJp() ? "表示(&V)" : "&View", Keys.F1);
            Add2(menuTool, FuncDelete, kernel.IsJp() ? "削除(&D)" : "&Delete", Keys.Delete);

            //最新の状態に更新する
            FuncRefresh();
        }

        //ダブルクリック
        void DlgDoubleClick(object sender, EventArgs e) {
            if (IsSelected())
                FuncView();
        }

        //最新の状態に更新する
        private void FuncRefresh() {
            Cmd("Refresh-MailBox");
        }

        //表示
        private void FuncView() {
            if (IsSelected()) {
                TreeNode node = _treeView.SelectedNode;
                var uid = (string)node.Tag;
                string user = node.Parent.Text;
                string cmdStr = string.Format("Cmd-View-{0}-{1}", user, uid);
                Cmd(cmdStr);
            }
        }

        //削除
        private void FuncDelete() {
            if (IsSelected()) {
                TreeNode node = _treeView.SelectedNode;
                var uid = (string)node.Tag;
                string user = node.Parent.Text;
                string cmdStr = string.Format("Cmd-Delete-{0}-{1}", user, uid);
                Cmd(cmdStr);
            }
        }

        //メールが選択されているかどうか?
        bool IsSelected() {
            TreeNode node = _treeView.SelectedNode;
            if (node.Parent != null) {
                if ((string)node.Parent.Tag == "user" || (string)node.Parent.Tag == "QUEUE")
                    return true;
            }
            Msg.Show(MsgKind.Error, Kernel.IsJp() ? "メールが選択されていません" : "An email is not chosen");
            return false;
        }

        //ダイアログが閉じる
        override public void Closed() {
            while (_tmpFileList.Count > 0) {
                File.Delete(_tmpFileList[0]);
                _tmpFileList.RemoveAt(0);
            }
        }

        //メインコントロールのクリア
        override public void Clear() {
            _treeView.Nodes.Clear();
        }

        //メインコントロールへのデータ追加
        override public void AddItem(string line) {
            string[] tmp = line.Split('\t');
            if (tmp.Length == 6) {
                string user = tmp[0];
                string uid = tmp[1];
                string from = tmp[2];
                string to = tmp[3];
                string size = tmp[4];
                string date = tmp[5];
                int imageIndex = 1;
                TreeNode node = AddUser(user);
                if ("QUEUE" == (string)node.Tag)
                    imageIndex = 2;
                TreeNode node2 = node.Nodes.Add(string.Format("From:<{0}> To:<{1}> Size:{2} Date:{3} [{4}]", from, to, size, date, uid));
                node2.ImageIndex = imageIndex;
                node2.SelectedImageIndex = imageIndex;
                node2.Tag = uid;
            }
            _treeView.ExpandAll();
        }

        //コマンドに対する応答
        override public void Recv(string cmdStr, string buffer) {
            if (cmdStr.IndexOf("Cmd-View") == 0) {
                //string[] tmp = cmdStr.Split('-');
                //string user = tmp[2];
                //string uid = tmp[3];
                string tmpFileName = Path.GetTempFileName() + ".eml";
                _tmpFileList.Add(tmpFileName);

                byte[] buf = Inet.ToBytes(buffer);
                using (var bw = new BinaryWriter(new FileStream(tmpFileName, FileMode.Create, FileAccess.Write))) {
                    bw.Write(buf);
                    bw.Flush();
                    bw.Close();
                }
                System.Diagnostics.Process.Start(tmpFileName);
            } else if (cmdStr.IndexOf("Cmd-Delete") == 0) {
                //string[] tmp = cmdStr.Split('-');
                //string user = tmp[2];
                //string uid = tmp[3];
                if (buffer == "running") {
                    Msg.Show(MsgKind.Error, Kernel.IsJp() ? "SMTPサーバの起動中は、メールの削除はできません" : "In start of a SMTP server, there is not elimination of an email");
                } else if (buffer == "success") {
                    FuncRefresh();//最新の状態に更新する
                }
            }
        }

        TreeNode AddUser(string name) {
            if (_treeView.Nodes.Count == 0) {
                TreeNode node = _treeView.Nodes.Add("QUEUE");
                node.Tag = "QUEUE";
                node.ImageIndex = 2;
                node.SelectedImageIndex = 2;
                node = _treeView.Nodes.Add("MAILBOX");
                node.Tag = "MAILBOX";
                node.ImageIndex = 1;
                node.SelectedImageIndex = 1;
            }

            if (name == "$queue") {
                return _treeView.Nodes[0];
            }
            TreeNode treeNode = null;
            foreach (TreeNode n in _treeView.Nodes[1].Nodes) {
                if (n.Text != name)
                    continue;
                treeNode = n;
                break;
            }
            if (treeNode == null) {
                treeNode = _treeView.Nodes[1].Nodes.Add(name);
                treeNode.Tag = "user";
            }
            return treeNode;
        }
    }
}