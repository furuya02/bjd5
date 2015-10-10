using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Bjd.net;
using Bjd.remote;
using Bjd.server;
using Bjd.sock;

namespace Bjd {
    public abstract partial class ToolDlg : Form {
        protected Kernel Kernel;
        readonly string _caption;
        protected string NameTag;
        protected Control MainControl;//メインコントロール

        new abstract public void Closed();//ダイアログが閉じる際に呼び出される
        abstract public void Clear();//メインコントロールのクリア
        abstract public void AddItem(string line);//メインコントロールへのデータ追加
        abstract public void Recv(string cmdStr, string buffer);//コマンドへの応答

        //通常の場合、Serverが初期化され
        //リモートクライアントの場合、TcpObjが初期化される
        protected OneServer Server;
        protected SockTcp sockTcp;

        public delegate void MenuFunc();
        ContextMenuStrip _popupMenu;

        protected ToolDlg(Kernel kernel,string nameTag,Object obj,string caption) {
            InitializeComponent();

            Kernel = kernel;
            NameTag = nameTag;
            _caption = caption;
        
            if (kernel.RunMode == RunMode.Remote) {
                sockTcp = (SockTcp)obj;
            } else {
                Server = (OneServer)obj;
            }

            Text = caption;

            //ウインドウサイズの復元
            kernel.WindowSize.Read(this);

            //MainMenuFile.Text = (kernel.IsJp()) ? "ファイル(&F)" : "&File";
            //MainMenuClose.Text = (kernel.IsJp()) ? "閉じる(&C)" : "&Close";
        }

        public override sealed string Text{
            get { return base.Text; }
            set { base.Text = value; }
        }

        //メインメニューの追加
        protected ToolStripMenuItem AddMenu(ToolStripMenuItem parent, MenuFunc menuFunc, string title,Keys keys) {
            ToolStripItem item = null;
            if (parent == null) {
                item = menuStrip.Items.Add(title);
            } else {
                if(title=="-")
                    parent.DropDownItems.Add(new ToolStripSeparator());
                else
                    item = parent.DropDownItems.Add(title);
            }

            if (item != null) {
                item.Click += MenuClick;
                item.Tag = menuFunc;
                if (keys != Keys.None)
                    ((ToolStripMenuItem)item).ShortcutKeys = keys;

            }
            return (ToolStripMenuItem)item;
        }
        //ポップアップメニューの追加
        protected void AddPopup(MenuFunc menuFunc, string title) {
            if (_popupMenu != null) {
                var item = _popupMenu.Items.Add(title);
                item.Click += MenuClick;
                item.Tag = menuFunc;
            }
        }
        //メインメニューとポップアップメニューの両方への追加
        protected ToolStripMenuItem Add2(ToolStripMenuItem parent, MenuFunc menuFunc, string title,Keys keys) {
            //ポップアップメニューの追加
            AddPopup(menuFunc, title);
            //メインメニューの追加
            return AddMenu(parent, menuFunc, title, keys);

        }

        private void MenuClick(object sender, EventArgs e) {
            try{
                var item = (ToolStripMenuItem)sender;
                if (item.Tag != null)
                    ((MenuFunc)item.Tag)();
            }catch{
            }
        }
        protected void FuncClose() {
            Close();
        }

        //メインコントロールの追加
        protected void AddControl(Control control){
            MainControl = control;
            
            //テンポラリ
            var list = new List<Control>();

            SuspendLayout();

            for (var i = 0; i < Controls.Count;i++ ) {
                list.Add(Controls[i]);
                if (i == 0) {
                    list.Add(MainControl);
                }
            }
            Controls.Clear();
            foreach (var t in list){
                Controls.Add(t);
            }
            ResumeLayout();

            _popupMenu = new ContextMenuStrip();
            MainControl.ContextMenuStrip = _popupMenu;
        }

        //ダイアログクローズ時のイベント処理
        private void ToolDlgFormClosed(object sender, FormClosedEventArgs e) {
            Closed();
            //ウインドウサイズの保存
            Kernel.WindowSize.Save(this);
            Kernel.View.Activated();
        }
        //ステータスバーへのテキスト表示
        protected void SetStatusText(string text) {
            StatusLabel.Text = text;
        }


        protected void Cmd(string cmdStr) {
            if (MainControl.InvokeRequired) {
                MainControl.Invoke(new MethodInvoker(()=>Cmd(cmdStr)));
            } else { // メインスレッドから呼び出された場合(コントロールへの描画)
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

                    if (Server != null) {
                        var buffer = Server.Cmd(cmdStr);//リモート操作（データ取得）
                        CmdRecv(cmdStr, buffer);
                    } else {
                        CmdRecv(cmdStr, "");
                    }
                }
            }
        }

        public void CmdRecv(string cmdStr,string buffer) {
            if (MainControl.InvokeRequired) {
                MainControl.Invoke(new MethodInvoker(()=>CmdRecv(cmdStr,buffer)));
            } else { // メインスレッドから呼び出された場合(コントロールへの描画)
                if (cmdStr.IndexOf("Refresh-")==0) {
                    string[] lines = buffer.Split(new char[] { '\b' }, StringSplitOptions.RemoveEmptyEntries);

                    //データ取得のため表示待機（解除）
                    MainControl.BackColor = SystemColors.Window;
                    MainControl.Update();
                    Text = _caption;

                    Kernel.Wait.Max = 100;
                    Kernel.Wait.Start("しばらくお待ちください。");


                    var max = lines.Length;
                    Kernel.Wait.Max = max;
                    for (var i = 0; i < max && Kernel.Wait.Life; i++) {

                        Kernel.Wait.Val = i;
                        Thread.Sleep(1);
                        AddItem(lines[i]);
                    }

                    //ステータスバーへのテキスト表示
                    Kernel.Wait.Stop();
                } else if (cmdStr.IndexOf("Cmd-") == 0) {
                    Recv(cmdStr,buffer);//コマンドへの応答(子クラスで実装される)
                }
            }
        }

    }
}