namespace Bjd.trace {
    partial class TraceDlg {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TraceDlg));
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.PopupMenuCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.PopupMenuClear = new System.Windows.Forms.ToolStripMenuItem();
            this.PopupMenuSave = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.PopupMenuClose = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.MainMenuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenuSave = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenuClose = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenuEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenuCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenuClear = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewTrace = new System.Windows.Forms.ListView();
            this.contextMenuStrip.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip
            // 
            this.statusStrip.Location = new System.Drawing.Point(0, 451);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(721, 22);
            this.statusStrip.TabIndex = 0;
            this.statusStrip.Text = "statusStrip1";
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PopupMenuCopy,
            this.PopupMenuClear,
            this.PopupMenuSave,
            this.toolStripMenuItem1,
            this.PopupMenuClose});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(174, 120);
            // 
            // PopupMenuCopy
            // 
            this.PopupMenuCopy.Name = "PopupMenuCopy";
            this.PopupMenuCopy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.PopupMenuCopy.Size = new System.Drawing.Size(173, 22);
            this.PopupMenuCopy.Text = "コピー(&C)";
            this.PopupMenuCopy.Click += new System.EventHandler(this.MainMenuCopyClick);
            // 
            // PopupMenuClear
            // 
            this.PopupMenuClear.Name = "PopupMenuClear";
            this.PopupMenuClear.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.PopupMenuClear.Size = new System.Drawing.Size(173, 22);
            this.PopupMenuClear.Text = "クリア(&L)";
            this.PopupMenuClear.Click += new System.EventHandler(this.MainMenuClearClick);
            // 
            // PopupMenuSave
            // 
            this.PopupMenuSave.Name = "PopupMenuSave";
            this.PopupMenuSave.Size = new System.Drawing.Size(173, 22);
            this.PopupMenuSave.Text = "名前を付けて保存(&S)";
            this.PopupMenuSave.Click += new System.EventHandler(this.MainMenuSaveClick);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(170, 6);
            // 
            // PopupMenuClose
            // 
            this.PopupMenuClose.Name = "PopupMenuClose";
            this.PopupMenuClose.Size = new System.Drawing.Size(173, 22);
            this.PopupMenuClose.Text = "閉じる(&X)";
            this.PopupMenuClose.Click += new System.EventHandler(this.MainMenuCloseClick);
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MainMenuFile,
            this.MainMenuEdit});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(721, 26);
            this.menuStrip.TabIndex = 2;
            this.menuStrip.Text = "menuStrip1";
            // 
            // MainMenuFile
            // 
            this.MainMenuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MainMenuSave,
            this.MainMenuClose});
            this.MainMenuFile.Name = "MainMenuFile";
            this.MainMenuFile.Size = new System.Drawing.Size(85, 22);
            this.MainMenuFile.Text = "ファイル(&F)";
            // 
            // MainMenuSave
            // 
            this.MainMenuSave.Name = "MainMenuSave";
            this.MainMenuSave.Size = new System.Drawing.Size(190, 22);
            this.MainMenuSave.Text = "名前を付けて保存(&S)";
            this.MainMenuSave.Click += new System.EventHandler(this.MainMenuSaveClick);
            // 
            // MainMenuClose
            // 
            this.MainMenuClose.Name = "MainMenuClose";
            this.MainMenuClose.Size = new System.Drawing.Size(190, 22);
            this.MainMenuClose.Text = "閉じる(&X)";
            this.MainMenuClose.Click += new System.EventHandler(this.MainMenuCloseClick);
            // 
            // MainMenuEdit
            // 
            this.MainMenuEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MainMenuCopy,
            this.MainMenuClear});
            this.MainMenuEdit.Name = "MainMenuEdit";
            this.MainMenuEdit.Size = new System.Drawing.Size(61, 22);
            this.MainMenuEdit.Text = "編集(&E)";
            // 
            // MainMenuCopy
            // 
            this.MainMenuCopy.Name = "MainMenuCopy";
            this.MainMenuCopy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.MainMenuCopy.Size = new System.Drawing.Size(177, 22);
            this.MainMenuCopy.Text = "コピー(&C)";
            this.MainMenuCopy.Click += new System.EventHandler(this.MainMenuCopyClick);
            // 
            // MainMenuClear
            // 
            this.MainMenuClear.Name = "MainMenuClear";
            this.MainMenuClear.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.MainMenuClear.Size = new System.Drawing.Size(177, 22);
            this.MainMenuClear.Text = "クリア(&L)";
            this.MainMenuClear.Click += new System.EventHandler(this.MainMenuClearClick);
            // 
            // listViewTrace
            // 
            this.listViewTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewTrace.FullRowSelect = true;
            this.listViewTrace.HideSelection = false;
            this.listViewTrace.Location = new System.Drawing.Point(0, 26);
            this.listViewTrace.Name = "listViewTrace";
            this.listViewTrace.OwnerDraw = true;
            this.listViewTrace.Size = new System.Drawing.Size(721, 425);
            this.listViewTrace.TabIndex = 3;
            this.listViewTrace.UseCompatibleStateImageBehavior = false;
            this.listViewTrace.View = System.Windows.Forms.View.Details;
            this.listViewTrace.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.ListViewDrawColumnHeader);
            this.listViewTrace.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.ListViewDrawSubItem);
            // 
            // TraceDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(721, 473);
            this.Controls.Add(this.listViewTrace);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TraceDlg";
            this.Text = "トレース表示";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TraceDlgFormClosing);
            this.contextMenuStrip.ResumeLayout(false);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem MainMenuFile;
        private System.Windows.Forms.ToolStripMenuItem MainMenuClose;
        private System.Windows.Forms.ToolStripMenuItem MainMenuEdit;
        private System.Windows.Forms.ToolStripMenuItem MainMenuCopy;
        private System.Windows.Forms.ToolStripMenuItem MainMenuClear;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem PopupMenuCopy;
        private System.Windows.Forms.ToolStripMenuItem PopupMenuClear;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem PopupMenuClose;
        private System.Windows.Forms.ListView listViewTrace;
        private System.Windows.Forms.ToolStripMenuItem PopupMenuSave;
        private System.Windows.Forms.ToolStripMenuItem MainMenuSave;
    }
}