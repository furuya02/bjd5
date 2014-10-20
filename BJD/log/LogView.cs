using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Bjd.util;

namespace Bjd.log {

    public class LogView : IDisposable{

        private readonly ListView _listView;
        private readonly List<OneLog> _ar = new List<OneLog>();
        private readonly Timer _timer;
        private readonly Kernel _kernel;

        public LogView(Kernel kernel,ListView listView){
            if (listView == null){
                return;
            }
            _kernel = kernel;
            _listView = listView;
            //タイマー（表示）イベント処理
            _timer = new Timer{Enabled = true, Interval = 100};
            _timer.Tick += TimerTick;
        }

        //タイマー(表示)イベント
        private void TimerTick(object sender, EventArgs e){
            if (_listView == null){
                return;
            }
            if (_ar.Count == 0)
                return;
            _timer.Enabled = false;
            lock (this){
                _listView.BeginUpdate();

                //Ver5.8.5 Java fix
                //「表示する最大行数」を超えた場合は削除する
                //Ver5.8.6 _kernel.ListOption!=null追加
                if (_kernel != null && _listView != null && _kernel.ListOption!=null){
                    var op = _kernel.ListOption.Get("Log");
                    if (op != null){
                        var linesMax = (int) op.GetValue("linesMax");
                        var linesDelete = (int) op.GetValue("linesDelete");
                        //「表示する最大行数」よりも「削除する行数」の方が大きい数値を設定されている場合、最大行数に修正する
                        if (linesMax < linesDelete)
                            linesDelete = linesMax;
                        if (_listView.Items.Count > linesMax) {
                            while (_listView.Items.Count > (linesMax - linesDelete)){
                                _listView.Items.RemoveAt(0);
                            }
                            GC.Collect();
                        }
                    }

                }

                //一回のイベントで処理する最大数は100行まで
                var list = new List<OneLog>();
                for (var i = 0; i < 300 && 0 < _ar.Count; i++){
                    list.Add(_ar[0]);
                    _ar.RemoveAt(0);
                }
                Disp(list);

                //Ver5.8.8
                //Java fix
//                //リモートクライアントへのログ送信
//                if (_kernel.RemoteConnect != null){
//                    //クライアントから接続されている場合
//                    foreach (var oneLog in list){
//                        
//                        var sv = _kernel.ListServer.Get("Remote");
//                        if (sv != null)
//                            sv.Append(oneLog);
//                    }
//                }


                _listView.EndUpdate();

            }
            //１行の高さを計算してスクロールする
            _listView.EnsureVisible(_listView.Items[_listView.Items.Count - 1].Index);
            _timer.Enabled = true;
        }

        public void Dispose(){
            if (_listView != null)
                _listView.Dispose();
        }

        public void SetFont(Font font){
            //Java fix
            if (font != null && _listView!=null){
                if (_listView.InvokeRequired){
                    _listView.BeginInvoke(new MethodInvoker(() => SetFont(font)));
                } else{
                    _listView.Font = font;
                }
            }
        }

        //ログビューへの表示(リモートからも使用される)
        public void Append(OneLog oneLog){
            if (_listView == null)
                return;
            lock (this){
                _ar.Add(oneLog);
            }
        }

        //選択されたログをクリップボードにコピーする
        public void SetClipboard(){
            if (_listView == null)
                return;

            var sb = new StringBuilder();
            var colMax = _listView.Columns.Count;
            for (int c = 0; c < colMax; c++){
                sb.Append(_listView.Columns[c].Text);
                sb.Append("\t");
            }
            sb.Append("\r\n");
            for (int i = 0; i < _listView.SelectedItems.Count; i++){
                for (int c = 0; c < colMax; c++){
                    sb.Append(_listView.SelectedItems[i].SubItems[c].Text);
                    sb.Append("\t");

                }
                sb.Append("\r\n");
            }
            //Ver6.1.0
            try{
                Clipboard.SetText(sb.ToString());
            } catch (Exception ex){
                Msg.Show(MsgKind.Error, ex.Message);
            }
        }

        //表示ログをクリア
        public void Clear(){
            if (_listView == null)
                return;
            _listView.Items.Clear();
        }

        //このメソッドはタイマースレッドからのみ使用される
        private void Disp(List<OneLog> list){
            try{
                foreach (OneLog oneLog in list){
                    if (_listView == null)
                        break;
                    //リストビューへの出力                    
                    ListViewItem item = _listView.Items.Add(oneLog.Dt());
                    item.SubItems.Add(oneLog.Kind());
                    item.SubItems.Add(oneLog.ThreadId());
                    item.SubItems.Add(oneLog.NameTag());
                    item.SubItems.Add(oneLog.RemoteHostname());
                    item.SubItems.Add(oneLog.MessageNo());
                    item.SubItems.Add(oneLog.Message());
                    item.SubItems.Add(oneLog.DetailInfomation());
                }
            }
            catch (Exception ex){
                var sb = new StringBuilder();
                sb.Append(ex.Message + "\r\n");
                foreach (var oneLog in list){
                    sb.Append(String.Format("{0} {1}\r\n", oneLog.MessageNo(),oneLog.Message()));
               }
                Msg.Show(MsgKind.Error, sb.ToString());
            }
        }
    }

}

    /*
    public class LogView:IDisposable {
        readonly Kernel _kernel;
        readonly ListView _listView;
        readonly Timer _timer;
        readonly List<OneLog> _ar = new List<OneLog>();


        public LogView(Kernel kernel,ListView listView) {
            if (listView == null)
                return;
            
            _kernel = kernel;
            _listView = listView;

            //タイマー（表示）イベント処理
            _timer = new Timer{Enabled = true, Interval = 100};

            _timer.Tick += TimerTick;
        }
        public void Dispose() {
            if (_listView != null)
                _listView.Dispose();

        }
        public void InitFont(){
            if (_kernel != null) {
                var font = (Font)_kernel.ListOption.Get("Log").GetValue("font");
                if (font != null)
                    _listView.Font = font;
            }
        }
        
        //ログビューへの表示(リモートからも使用される)
        public void Append(OneLog oneLog) {
            if (_listView == null)
                return;
            lock (this) {
                _ar.Add(oneLog);
            }
        }


        //必要であれば有効化する
//        public void Refresh() {
//            if (listView != null && listView.InvokeRequired) {// 別スレッドから呼び出された場合
//                listView.BeginInvoke(new MethodInvoker(() => Refresh()));
//            } else {
//                if (listView != null)
//                    timer_Tick(null, null);
//            }
//        }

        //選択されたログをクリップボードにコピーする
        public void SetClipboard() {
            if (_listView == null)
                return;

            var sb = new StringBuilder();
            var colMax = _listView.Columns.Count;
            for (int c = 0; c < colMax; c++) {
                sb.Append(_listView.Columns[c].Text);
                sb.Append("\t");
            }
            sb.Append("\r\n");
            for (int i = 0; i < _listView.SelectedItems.Count; i++) {
                for (int c = 0; c < colMax; c++) {
                    sb.Append(_listView.SelectedItems[i].SubItems[c].Text);
                    sb.Append("\t");

                }
                sb.Append("\r\n");
            }
            Clipboard.SetText(sb.ToString());
        }
        //表示ログをクリア
        public void Clear() {
            if (_listView == null)
                return;
            _listView.Items.Clear();
        }

        //タイマー(表示)イベント
        void TimerTick(object sender, EventArgs e) {
            if (_ar.Count == 0)
                return;
            _timer.Enabled = false;
            lock (this) {
                _listView.BeginUpdate();

                //あとでリアルのオプションを取得する方法を確立してから実装する

                //「表示する最大行数」を超えた場合は、2000行まで削除する
//                if (listView.Items.Count > kernel.Log2.LinesMax) {
//                    //「表示する最大行数」よりも「削除する行数」の方が大きい数値を設定されている場合、最大行数に修正する
//                    int linesDelete = kernel.Log2.LinesDelete;
//                    if (kernel.Log2.LinesMax < linesDelete)
//                        linesDelete = kernel.Log2.LinesMax;
//                    while (listView.Items.Count > (kernel.Log2.LinesMax - linesDelete)) {
//                        listView.Items.RemoveAt(0);
//                    }
//                }
                //一回のイベントで処理する最大数は100行まで
                var list = new List<OneLog>();
                for (int i = 0; i < 300 && 0 < _ar.Count; i++) {
                    list.Add(_ar[0]);
                    _ar.RemoveAt(0);
                }
                Disp(list);

                _listView.EndUpdate();

            }
            //１行の高さを計算してスクロールする
            _listView.EnsureVisible(_listView.Items[_listView.Items.Count - 1].Index);
            _timer.Enabled = true;
        }

        //このメソッドはタイマースレッドからのみ使用される
        void Disp(List<OneLog> list) {
            try {
                foreach (OneLog oneLog in list) {
                    if (_listView == null)
                        break;
                    //リストビューへの出力                    
                    ListViewItem item = _listView.Items.Add(oneLog.Dt());
                    item.SubItems.Add(oneLog.Kind());
                    item.SubItems.Add(oneLog.ThreadId());
                    item.SubItems.Add(oneLog.NameTag());
                    item.SubItems.Add(oneLog.RemoteHostname());
                    item.SubItems.Add(oneLog.MessageNo());
                    item.SubItems.Add(oneLog.Message());
                    item.SubItems.Add(oneLog.DetailInfomation());
                }
            } catch (Exception ex) {
                var sb = new StringBuilder();
                sb.Append(ex.Message + "\r\n");
                foreach (var oneLog in list) {
                    string.Format("{0} {1} {2}\r\n", oneLog.MessageNo, oneLog.Message);
                }
                Msg.Show(MsgKind.Error, sb.ToString());
            }
        }

    }

}
*/