using System;
using System.Windows.Forms;
using Bjd;

namespace ProxyHttpServer {
    public class Dlg : ToolDlg {
        readonly ListView _listView;

        CacheKind _kind = CacheKind.Memory;

        public Dlg(Kernel kernel, string nameTag, Object obj, string caption)
            : base(kernel, nameTag, obj, caption) {
            //リストビューの作成
            _listView = new ListView{
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                HideSelection = false,
                View = System.Windows.Forms.View.Details
            };
            _listView.Columns.Add("Url");
            _listView.Columns.Add("Size");
            _listView.Columns.Add("LastModified");
            _listView.Columns.Add("Expires");
            _listView.Columns.Add("Create");
            _listView.Columns.Add("LastAccess");
            _listView.Columns[0].Width = 400;
            _listView.Columns[1].Width = 100;
            _listView.Columns[2].Width = 120;
            _listView.Columns[3].Width = 120;
            _listView.Columns[4].Width = 120;
            _listView.Columns[5].Width = 120;

            //メインコントロールの追加
            AddControl(_listView);

            ToolStripMenuItem menuFile = AddMenu(null, null, kernel.IsJp() ? "ファイル(&F)" : "&File", Keys.None);
            Add2(menuFile, FuncDisk, kernel.IsJp() ? "ディスク一覧(&D)" : "&Disk", Keys.F1);
            Add2(menuFile, FuncMemory, kernel.IsJp() ? "メモリ一覧(&M)" : "&Memory", Keys.F2);
            AddMenu(menuFile, null, "-", Keys.None);
            AddMenu(menuFile, FuncClose, kernel.IsJp() ? "閉じる(&C)" : "&Close", Keys.None);

            AddPopup(null, "-");

            ToolStripMenuItem menuEdit = AddMenu(null, null, kernel.IsJp() ? "編集(&E)" : "&Edit", Keys.None);
            Add2(menuEdit, FuncSelectAll, kernel.IsJp() ? "すべて選択(&A)" : "Select &All", (Keys.Control | Keys.A));
            Add2(menuEdit, FuncDelete, kernel.IsJp() ? "削除(&D)" : "&Delete", Keys.Delete);

            //メモリ一覧
            FuncMemory();
        }

        //メモリ一覧
        void FuncMemory() {
            _kind = CacheKind.Memory;
            Cmd("Refresh-MemoryCache");
        }

        //ディスク一覧
        void FuncDisk() {
            _kind = CacheKind.Disk;
            Cmd("Refresh-DiskCache");
        }

        //すべて選択
        private void FuncSelectAll() {
            _listView.BeginUpdate();
            foreach (ListViewItem item in _listView.Items) {
                item.Selected = true;
            }
            _listView.EndUpdate();
        }

        //削除
        private void FuncDelete() {
            _listView.BeginUpdate();
            foreach (ListViewItem item in _listView.SelectedItems) {
                try {
                    var str = item.Text;

                    int index = str.IndexOf("://");
                    //(前半) "http"
                    //(後半) "hostname_80/path/filename.ext"
                    if (index < 0)
                        continue;
                    str = str.Substring(index + 3);

                    //(前半) "hostname_80"
                    index = str.IndexOf("_");
                    if (index < 0)
                        continue;
                    string hostName = str.Substring(0, index);
                    //(後半) "80/path/filename.ext"
                    str = str.Substring(index + 1);

                    //(前半) "80"
                    index = str.IndexOf("/");
                    if (index < 0)
                        continue;
                    string portStr = str.Substring(0, index);
                    //(後半) "/path/filename.ext"
                    string uri = str.Substring(index);

                    int port = Convert.ToInt32(portStr);
                    const string cmd = "Cmd-Remove";

                    //コマンド実行
                    string cmdStr = string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                        cmd,
                        _kind,
                        hostName,
                        port,
                        uri);
                    Cmd(cmdStr);//リモート操作対応

                    _listView.Items.Remove(item);
                } catch (Exception){
                }
            }
            _listView.EndUpdate();
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
            var cacheInfo = new CacheInfo(line);

            ListViewItem item = _listView.Items.Add(cacheInfo.Url);
            item.SubItems.Add(string.Format("{0:#,0}", cacheInfo.Size));
            item.SubItems.Add(cacheInfo.LastModified.Ticks == 0 ? "" : cacheInfo.LastModified.ToString());
            item.SubItems.Add(cacheInfo.Expires.Ticks == 0 ? "" : cacheInfo.Expires.ToString());
            item.SubItems.Add(cacheInfo.CreateDt.Ticks == 0 ? "" : cacheInfo.CreateDt.ToString());
            item.SubItems.Add(cacheInfo.LastAccess.Ticks == 0 ? "" : cacheInfo.LastAccess.ToString());
        }

        //コマンドに対する応答
        override public void Recv(string cmdStr, string buffer) {
            
        }
    }
}
