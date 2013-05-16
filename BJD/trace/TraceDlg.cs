using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using Bjd.log;
using Bjd.net;

namespace Bjd.trace
{
    public partial class TraceDlg : Form
    {
        readonly Kernel _kernel;
        readonly Timer _timer;
        readonly List<OneTrace> _ar = new List<OneTrace>();
        Logger _logger;

        readonly List<string> _colJp = new List<string>();
        readonly List<string> _colEn = new List<string>();

        public TraceDlg(Kernel kernel) {
            InitializeComponent();

            _kernel = kernel;

            _colJp.Add("送受");
            _colJp.Add("スレッドID");
            _colJp.Add("アドレス");
            _colJp.Add("データ");
            _colEn.Add("Direction");
            _colEn.Add("ThreadID");
            _colEn.Add("Address");
            _colEn.Add("Data");

            //オーナー描画
            foreach (var t in _colJp){
                listViewTrace.Columns.Add(t);
            }
            listViewTrace.Columns[2].Width = 100;
            listViewTrace.Columns[3].Width = 500;


            _timer = new Timer{Enabled = true, Interval = 100};
            _timer.Tick += TimerTick;

            kernel.WindowSize.Read(this);//ウインドサイズの復元
            kernel.WindowSize.Read(listViewTrace);//カラム幅の復元

        }
        new public void Dispose() {

            _kernel.WindowSize.Save(this);//ウインドサイズの保存
            _kernel.WindowSize.Save(listViewTrace);//カラム幅の保存

            base.Dispose();
        }

        //オブジェクトの破棄を避けて、非表示にする
        private void TraceDlgFormClosing(object sender, FormClosingEventArgs e) {
            listViewTrace.Items.Clear();
            e.Cancel = true;
            Visible = false;

        }
        //閉じる
        private void MainMenuCloseClick(object sender, EventArgs e) {
            listViewTrace.Items.Clear();
            Visible = false;


            if (_kernel.RunMode == RunMode.Remote) {
                //トレース表示がクローズしたことをサーバーに送信する
                _kernel.RemoteClient.VisibleTrace2(false);
            }
        }
        //開く
        public void Open() {
            Text = (_kernel.IsJp()) ? "トレース表示" : "Trace Dialog";
            MainMenuFile.Text = (_kernel.IsJp()) ? "ファイル(&F)" : "&File";
            MainMenuClose.Text = (_kernel.IsJp()) ? "閉じる(&C)" : "&Close";
            MainMenuEdit.Text = (_kernel.IsJp()) ? "編集(&E)" : "&Edit";
            MainMenuCopy.Text = (_kernel.IsJp()) ? "コピー(&C)" : "&Copy";
            MainMenuClear.Text = (_kernel.IsJp()) ? "クリア(&L)" : "C&lear";
            MainMenuSave.Text = (_kernel.IsJp()) ? "名前を付けて保存(&S)" : "S&ave";

            PopupMenuCopy.Text = (_kernel.IsJp()) ? "コピー(&C)" : "&Copy";
            PopupMenuClear.Text = (_kernel.IsJp()) ? "クリア(&L)" : "C&lear";
            PopupMenuClose.Text = (_kernel.IsJp()) ? "閉じる(&C)" : "&Close";
            PopupMenuClose.Text = (_kernel.IsJp()) ? "名前を付けて保存(&S)" : "S&ave";

            for (int i = 0; i < _colJp.Count; i++) {
                listViewTrace.Columns[i].Text = (_kernel.IsJp()) ? _colJp[i] : _colEn[i];
            }

            Show();
            Focus();

            if (_kernel.RunMode == RunMode.Remote) {
                //トレース表示がオープンしたことをサーバーに送信する
                _kernel.RemoteClient.VisibleTrace2(true);
            }

        }

        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();

        //トレースの追加（リモートサーバスレッドから使用される）
        public void AddTrace(string buffer) {
            string[] tmp = buffer.Split(new[] { '\b' }, 4);
            if (tmp.Length < 4)
                return;
            try {
                var traceKind = (TraceKind)Enum.Parse(typeof(TraceKind), tmp[0]);
                var threadId = Convert.ToInt32(tmp[1]);
                var ip = new Ip(tmp[2]);
                var str = tmp[3];
                //トレースの追加（内部共通処理）
                lock (this) {
                    _ar.Add(new OneTrace(traceKind, str, threadId, ip));
                }
            } catch (Exception){ }
        }

        //トレースの追加(SockObj内から使用される)
        public void AddTrace(TraceKind traceKind, string str, Ip ip) {
            if (!Visible)
                return;
            var threadId = GetCurrentThreadId();

            //トレースの追加（内部共通処理）
            lock (this) {
                _ar.Add(new OneTrace(traceKind, str, threadId, ip));
            }
        }
        //タイマー(表示)イベント
        void TimerTick(object sender, EventArgs e) {
            if (_ar.Count == 0)
                return;
            _timer.Enabled = false;
            lock (this) {
                if (!Visible) {//トレースダイアログが閉じている場内、蓄積されたデータは破棄される
                    _ar.Clear();
                }

                listViewTrace.BeginUpdate();
                //Ver5.1.2
                if (_ar.Count > 2000) {
                    while (_ar.Count > 2000) {
                        _ar.RemoveAt(0);
                    }
                    listViewTrace.Items.Clear();
                } else {
                    if (listViewTrace.Items.Count > 3000) {
                        while (listViewTrace.Items.Count > 2000) {
                            listViewTrace.Items.RemoveAt(0);
                        }
                    }
                }
                //一回のイベントで処理する最大数は100行まで
                var list = new List<OneTrace>();
                for (var i = 0; i < 100 && 0 < _ar.Count; i++) {
                    list.Add(_ar[0]);
                    _ar.RemoveAt(0);
                }
                Disp2(list);

                listViewTrace.EndUpdate();
            }
            //１行の高さを計算してスクロールする
            try {
                listViewTrace.EnsureVisible(listViewTrace.Items[listViewTrace.Items.Count - 1].Index);
            } catch {
            }
            _timer.Enabled = true;
        }

        //トレースの追加（内部共通処理）
        //タイマースレッドからのみ使用される
        void Disp2(List<OneTrace> list) {
            if (list.Count <= 0)
                return;
            try {
                if (listViewTrace.InvokeRequired) {
                    listViewTrace.Invoke(new MethodInvoker(() => Disp2(list)));
                } else {
                    foreach (OneTrace oneTrace in list) {

                        //リストビューへの出力                    
                        ListViewItem item = listViewTrace.Items.Add(oneTrace.TraceKind.ToString());
                        item.SubItems.Add(oneTrace.ThreadId.ToString());
                        item.SubItems.Add(oneTrace.Ip.ToString());
                        item.SubItems.Add(oneTrace.Str);
                    }
                }
            } catch (Exception ex) {
                if (_logger == null)
                    _logger = _kernel.CreateLogger("TraceDlg", false, null);
                //Ver5.0.0-b3 トレース表示で発生した例外をログ出力で処理するように修正
                _logger.Set(LogKind.Error, null, 9000041, ex.Message);
                _logger.Exception(ex, null, 9000038);
            }
        }

        //クリア
        private void MainMenuClearClick(object sender, EventArgs e) {
            listViewTrace.Items.Clear();
            //listBox.Items.Clear();
        }
        //コピー
        private void MainMenuCopyClick(object sender, EventArgs e) {
            //StringBuilder sb = new StringBuilder();
            //for (int i = 0;i < listView.Items.Count;i++) {
            //    for(int c=0;c<ColJp.Count;c++){
            //        sb.Append(listView.Items[i].SubItems[c].Text);
            //        sb.Append("\t");
            //    }
            //    sb.Append("\r\n");
            //}

            //if(sb.Length>0)
            //    Clipboard.SetText(sb.ToString());
            string str = GetText();
            if (str.Length > 0)
                Clipboard.SetText(str);
        }

        private void ListViewDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) {
            e.DrawDefault = true;
        }

        private void ListViewDrawSubItem(object sender, DrawListViewSubItemEventArgs e) {
            if (e.ItemIndex < 0)
                return;
            try {
                //文字を描画する色の選択
                var b = new SolidBrush(Color.DodgerBlue);
                if (e.Item.SubItems[0].Text == "Recv")
                    b = new SolidBrush(Color.Red);

                if (e.Item.Selected) {// 選択行
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
                    if (e.Item.SubItems[0].Text != "Recv")
                        b = new SolidBrush(Color.White);
                } else {
                    e.DrawBackground();
                }

                //Ver5.1.4
                //e.Graphics.DrawString(e.Item.SubItems[e.ColumnIndex].Text, listView.Font, b, e.Bounds);//文字列の描画
                e.Graphics.SetClip(e.Bounds);
                e.Graphics.DrawString(e.Item.SubItems[e.ColumnIndex].Text, listViewTrace.Font, b, e.Bounds.X + 2, e.Bounds.Y + 2);//文字列の描画
                b.Dispose();

            } catch {
            }
        }
        //名前を付けて保存
        private void MainMenuSaveClick(object sender, EventArgs e) {
            var sfd = new SaveFileDialog();
            sfd.FileName = "trace.txt";
            sfd.Filter = "TraceFile(*.txt)|*.txt|All(*.*)|*.*";
            sfd.RestoreDirectory = true;

            if (sfd.ShowDialog() == DialogResult.OK) {
                using (var sw = new StreamWriter(sfd.FileName, false, Encoding.GetEncoding("Shift_JIS"))) {
                    sw.Write(GetText());
                    sw.Flush();
                    sw.Close();
                }
            }
        }
        //内容をテキスト形式で取得する
        string GetText() {
            var sb = new StringBuilder();
            for (int i = 0; i < listViewTrace.Items.Count; i++) {
                for (int c = 0; c < _colJp.Count; c++) {
                    sb.Append(listViewTrace.Items[i].SubItems[c].Text);
                    sb.Append("\t");
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }
    }
}