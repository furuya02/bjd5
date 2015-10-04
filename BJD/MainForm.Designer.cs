namespace Bjd {
    partial class MainForm {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.listViewMainLog = new System.Windows.Forms.ListView();
            this.date = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.kind = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.threadId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.server = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.address = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.messageNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.message = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.detailInfomation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.PopupMenuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.PopupMenuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(831, 24);
            this.menuStrip.TabIndex = 3;
            this.menuStrip.Text = "menuStrip1";
            // 
            // statusStrip
            // 
            this.statusStrip.Location = new System.Drawing.Point(0, 368);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(831, 22);
            this.statusStrip.TabIndex = 4;
            this.statusStrip.Text = "statusStrip1";
            // 
            // fontDialog1
            // 
            this.fontDialog1.Color = System.Drawing.SystemColors.ControlText;
            // 
            // listViewMainLog
            // 
            this.listViewMainLog.BackColor = System.Drawing.SystemColors.Window;
            this.listViewMainLog.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.date,
            this.kind,
            this.threadId,
            this.server,
            this.address,
            this.messageNo,
            this.message,
            this.detailInfomation});
            this.listViewMainLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewMainLog.FullRowSelect = true;
            this.listViewMainLog.Location = new System.Drawing.Point(0, 24);
            this.listViewMainLog.Name = "listViewMainLog";
            this.listViewMainLog.Size = new System.Drawing.Size(831, 344);
            this.listViewMainLog.TabIndex = 5;
            this.listViewMainLog.UseCompatibleStateImageBehavior = false;
            this.listViewMainLog.View = System.Windows.Forms.View.Details;
            // 
            // date
            // 
            this.date.Text = "日時";
            this.date.Width = 120;
            // 
            // kind
            // 
            this.kind.Text = "種類";
            // 
            // threadId
            // 
            this.threadId.Text = "スレッドID";
            // 
            // server
            // 
            this.server.Text = "機能(サーバ)";
            this.server.Width = 80;
            // 
            // address
            // 
            this.address.Text = "アドレス";
            this.address.Width = 80;
            // 
            // messageNo
            // 
            this.messageNo.Text = "メッセージID";
            this.messageNo.Width = 70;
            // 
            // message
            // 
            this.message.Text = "説明";
            this.message.Width = 200;
            // 
            // detailInfomation
            // 
            this.detailInfomation.Text = "詳細情報";
            this.detailInfomation.Width = 300;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PopupMenuOpen,
            this.PopupMenuExit});
            this.contextMenuStrip.Name = "popupMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(117, 48);
            // 
            // PopupMenuOpen
            // 
            this.PopupMenuOpen.Name = "PopupMenuOpen";
            this.PopupMenuOpen.Size = new System.Drawing.Size(116, 22);
            this.PopupMenuOpen.Text = "開く(&O)";
            this.PopupMenuOpen.Click += new System.EventHandler(this.PopupMenuClick);
            // 
            // PopupMenuExit
            // 
            this.PopupMenuExit.Name = "PopupMenuExit";
            this.PopupMenuExit.Size = new System.Drawing.Size(116, 22);
            this.PopupMenuExit.Text = "終了(&X)";
            this.PopupMenuExit.Click += new System.EventHandler(this.PopupMenuClick);
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "BlackJumboDog";
            this.notifyIcon.DoubleClick += new System.EventHandler(this.PopupMenuClick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(831, 390);
            this.Controls.Add(this.listViewMainLog);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "BlackJumboDog";
            this.Activated += new System.EventHandler(this.MainFormActivated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing);
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.ListView listViewMainLog;
        private System.Windows.Forms.ColumnHeader date;
        private System.Windows.Forms.ColumnHeader server;
        private System.Windows.Forms.ColumnHeader messageNo;
        private System.Windows.Forms.ColumnHeader message;
        private System.Windows.Forms.ColumnHeader threadId;
        private System.Windows.Forms.ColumnHeader detailInfomation;
        private System.Windows.Forms.ColumnHeader kind;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem PopupMenuOpen;
        private System.Windows.Forms.ToolStripMenuItem PopupMenuExit;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ColumnHeader address;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     