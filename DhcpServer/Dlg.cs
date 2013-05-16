using System;
using System.Windows.Forms;
using Bjd;

namespace DhcpServer {
    public class Dlg : ToolDlg {
        readonly ListView _listView;

        public Dlg(Kernel kernel, string nameTag, Object obj, string caption)
            : base(kernel, nameTag, obj, caption) {
            //リストビューの作成
            _listView = new ListView{
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                HideSelection = false,
                View = System.Windows.Forms.View.Details
            };
            _listView.Columns.Add("Status");
            _listView.Columns.Add("Ip");
            _listView.Columns.Add("MacAppointment");
            _listView.Columns.Add("Mac");
            _listView.Columns.Add("Date");
            _listView.Columns[0].Width = 120;
            _listView.Columns[1].Width = 120;
            _listView.Columns[2].Width = 100;
            _listView.Columns[3].Width = 250;
            _listView.Columns[4].Width = 350;
            //メインコントロールの追加
            AddControl(_listView);

            ToolStripMenuItem menuFile = AddMenu(null, null, kernel.IsJp() ? "ファイル(&F)" : "&File", Keys.None);
            Add2(menuFile, FuncRefresh, kernel.IsJp() ? "最新の状態に更新する(&R)" : "&Refresh", Keys.F5);
            AddMenu(menuFile, null, "-", Keys.None);
            AddMenu(menuFile, FuncClose, kernel.IsJp() ? "閉じる(&C)" : "&Close", Keys.None);

            FuncRefresh();//最新の状態に更新する
        }

        //最新の状態に更新する
        private void FuncRefresh() {
            Cmd("Refresh-Lease");
        }

        //ダイアログが閉じる
        override public void Closed() {
            
        }

        //メインコントロールのクリア
        override public void Clear() {
            _listView.Items.Clear();
        }

        //メインコントロールへのデータ追加
        override public void AddItem(string line) {
            var oneLease = new OneLease(line);

            ListViewItem item = _listView.Items.Add(oneLease.DbStatus.ToString());
            item.SubItems.Add(oneLease.Ip.ToString());
            item.SubItems.Add(oneLease.MacAppointment.ToString());
            item.SubItems.Add(oneLease.Mac.ToString());
            item.SubItems.Add(oneLease.Dt.ToString());
        }

        //コマンドに対する応答
        override public void Recv(string cmdStr, string buffer) {
            
        }
    }
}