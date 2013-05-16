using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Bjd.remote;

namespace Bjd {
    public class Dlg : ToolDlg {
        readonly ListBox _listBox;

        public Dlg(Kernel kernel, string nameTag, Object obj, string caption)
            : base(kernel, nameTag, obj, caption) {
            //リストビューの作成
            _listBox = new ListBox{Dock = DockStyle.Fill};
            //メインコントロールの追加
            AddControl(_listBox);

            var menuFile = AddMenu(null, null, kernel.IsJp() ? "ファイル(&F)" : "&File", Keys.None);
            AddMenu(menuFile, FuncClose, kernel.IsJp() ? "閉じる(&C)" : "&Close", Keys.None);

            var menuEdit = AddMenu(null, null, kernel.IsJp() ? "編集(&E)" : "&Edit", Keys.None);
            Add2(menuEdit, FuncRefresh, kernel.IsJp() ? "最新状態にする(&R)" : "&Refresh", Keys.F5);
            Add2(menuEdit, FuncCopy, kernel.IsJp() ? "コピー(&C)" : "&Copy", (Keys.Control | Keys.C));

            //最新状態に更新
            FuncRefresh();
        }

        //ダイアログが閉じる
        override public void Closed() {
            
        }

        //メインコントロールのクリア
        override public void Clear() {
            _listBox.Items.Clear();
        }

        //メインコントロールへのデータ追加
        override public void AddItem(string str) {
            _listBox.Items.Add(str);
        }

        //コマンドに対する応答
        override public void Recv(string cmdStr, string buffer) {
            
        }

        //コピー
        private void FuncCopy() {
            var sb = new StringBuilder();
            foreach (object t in _listBox.Items){
                sb.Append(t.ToString());
            }
            Clipboard.SetText(sb.ToString());
        }

        //最新の状態にする
        private void FuncRefresh() {
            //ToolStatusはServerから呼び出されていないため、基底クラスのRefresh()は使用できない
            //このため、オーバーライドしたthis.Refresh()を使用する
            Cmd("Refresh-status");
        }

        new protected void Cmd(string cmdStr) {
            if (cmdStr.IndexOf("Refresh") == 0) {
                //メインコントロールのクリア
                Clear();

                //データ取得のため表示待機
                //ステータスバーへのテキスト表示
                SetStatusText("");
                MainControl.BackColor = SystemColors.ButtonFace;
                MainControl.Update();
                Text = "情報取得中です。しばらくお待ちください。";
            }

            if (Kernel.RunMode == RunMode.Remote) {
                //（ToolDlg用）データ要求(C->S)
                RemoteData.Send(sockTcp, RemoteDataKind.CmdTool, string.Format("{0}-{1}", NameTag, cmdStr));
            } else {
                //if (manager != null) {
                string buffer = Kernel.Cmd(cmdStr);//リモート操作（データ取得）
                CmdRecv(cmdStr, buffer);
                //} else {
                //    RemoteObj remoteObj = new RemoteObj(REMOTE_OBJ_KIND.CMD_TOOL,string.Format("{0}-{1}", nameTag, cmdStr));
                //    remoteObj.Send(sockTcp);
                //}
            }
        }
    }
}
